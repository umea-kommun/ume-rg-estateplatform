using Microsoft.Extensions.Logging;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Api;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Dto;
using Umea.se.EstateService.Shared.Data;
using Umea.se.EstateService.Shared.Data.Entities;
using Umea.se.EstateService.Shared.Data.Enums;
using Umea.se.EstateService.Shared.Exceptions;
using Umea.se.EstateService.Shared.Infrastructure;
using Umea.se.EstateService.Shared.Infrastructure.ConfigurationModels;
using Umea.se.EstateService.Shared.Models;

namespace Umea.se.EstateService.Logic.Handlers.WorkOrder;

public class WorkOrderStatusSyncService(
    IWorkOrderRepository workOrderRepository,
    IPythagorasClient pythagorasClient,
    IDataStore dataStore,
    ApplicationConfig appConfig,
    ILogger<WorkOrderStatusSyncService> logger)
{
    private const int BatchSize = 100;
    private static readonly TimeSpan _upstreamTimeout = TimeSpan.FromSeconds(30);
    // Must exceed the upstream timeout: each batch is claimed right before its single
    // read, so a claim cannot be retaken mid-read.
    private static readonly TimeSpan _minimumCooldown = TimeSpan.FromSeconds(60);
    private readonly WorkOrderConfiguration _config = appConfig.WorkOrderProcessing;

    /// <summary>Refreshes every due order the user owns and returns the whole list.</summary>
    public async Task<WorkOrderRefreshModel> RefreshAsync(string email, CancellationToken cancellationToken = default)
    {
        (IReadOnlyList<WorkOrderEntity> orders, WorkOrderRefreshOutcome outcome) = await RefreshCoreAsync(email, null, cancellationToken);
        return new WorkOrderRefreshModel { WorkOrders = WorkOrderMapper.MapToListItems(orders, dataStore.BuildingsById), Outcome = outcome };
    }

    /// <summary>Refreshes one order the user owns, under the same rules, and returns its detail.</summary>
    public async Task<WorkOrderDetailModel> RefreshOneAsync(string email, Guid uid, CancellationToken cancellationToken = default)
    {
        (IReadOnlyList<WorkOrderEntity> orders, WorkOrderRefreshOutcome outcome) = await RefreshCoreAsync(email, uid, cancellationToken);
        return WorkOrderMapper.MapToDetail(orders.First(e => e.Uid == uid), outcome);
    }

    private async Task<(IReadOnlyList<WorkOrderEntity> Orders, WorkOrderRefreshOutcome Outcome)> RefreshCoreAsync(
        string email, Guid? uid, CancellationToken cancellationToken)
    {
        IReadOnlyList<WorkOrderEntity> orders = await workOrderRepository.GetByEmailAsync(email, cancellationToken);
        if (uid.HasValue && orders.All(e => e.Uid != uid))
        {
            throw new EntityNotFoundException($"Work order {uid} not found.");
        }

        if (!_config.StatusSyncEnabled)
        {
            return (orders, WorkOrderRefreshOutcome.Disabled);
        }

        TimeSpan cooldown = TimeSpan.FromSeconds(Math.Max(_minimumCooldown.TotalSeconds, _config.StatusRefreshCooldownSeconds));
        DateTimeOffset now = DateTimeOffset.UtcNow;
        DateTimeOffset staleBefore = now - cooldown;

        // Completed orders are re-read for a few days to pick up a late performed description,
        // and once more to fill a missing work order number or completion stamp.
        // Filtering on the loaded stamp is safe since it only moves forward, and it keeps a
        // repeat page open free of writes.
        DateTimeOffset completedAfter = now.AddDays(-_config.CompletedRefreshDays);
        List<WorkOrderEntity> eligible = [.. orders.Where(e =>
            (!uid.HasValue || e.Uid == uid)
            && e.SyncStatus == WorkOrderSyncStatus.Submitted && e.PythagorasWorkOrderId.HasValue
            && (e.PythagorasWorkOrderName is null
                || !IsCompleted(e.PythagorasStatusCategory)
                || e.CompletedAt is null
                || e.CompletedAt > completedAfter)
            && (e.StatusCheckedAt is null || e.StatusCheckedAt <= staleBefore))];

        bool anyClaimed = false;
        bool anyFailed = false;
        foreach (WorkOrderEntity[] batch in eligible.Chunk(BatchSize))
        {
            // Stamping StatusCheckedAt is the claim: only one overlapping request reads each order.
            // Failed reads keep the stamp, so an outage is not retried until the cooldown passes.
            // Claiming per batch means an aborted call only burns the batch it was reading.
            IReadOnlyList<int> claimedIds = await workOrderRepository.ClaimForStatusRefreshAsync(
                [.. batch.Select(e => e.Id)], staleBefore, now, cancellationToken);
            if (claimedIds.Count == 0)
            {
                continue;
            }

            anyClaimed = true;
            List<WorkOrderEntity> claimed = [.. batch.Where(e => claimedIds.Contains(e.Id))];
            foreach (WorkOrderEntity order in claimed)
            {
                order.StatusCheckedAt = now;
            }

            IReadOnlyList<WorkOrderInfoDto> infos;
            try
            {
                List<int> ids = [.. claimed.Select(e => e.PythagorasWorkOrderId!.Value).Distinct()];
                using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeout.CancelAfter(_upstreamTimeout);
                infos = await pythagorasClient.GetWorkOrderInfosByIdsAsync(ids, timeout.Token);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to refresh {Count} work order statuses.", claimed.Count);
                anyFailed = true;
                continue;
            }

            List<WorkOrderEntity> read = [];
            foreach (WorkOrderEntity order in claimed)
            {
                WorkOrderInfoDto? info = infos.FirstOrDefault(e => e.Id == order.PythagorasWorkOrderId);
                if (info is not { StatusId: not null } || string.IsNullOrWhiteSpace(info.StatusName))
                {
                    anyFailed = true;
                    continue;
                }

                // Empty marks "read, Pythagoras has no number" so a completed order is not re-read.
                order.PythagorasWorkOrderName = string.IsNullOrWhiteSpace(info.Name)
                    ? order.PythagorasWorkOrderName ?? string.Empty
                    : info.Name;

                // Pythagoras has no per-field timestamps, only the work order's last update, and which
                // edits move that is not documented. A change seen here is dated by it when it is newer
                // than the previous date, and otherwise by this read.
                DateTimeOffset? updated = FromUnixMilliseconds(info.Updated);

                if (info.StatusId != order.PythagorasStatusId)
                {
                    order.StatusChangedAt = IfNewer(updated, order.StatusChangedAt) ?? now;
                }

                order.PythagorasStatusId = info.StatusId;
                order.PythagorasStatusName = info.StatusName;
                order.PythagorasStatusCategory = info.StatusCategory;
                order.CompletedAt = IsCompleted(info.StatusCategory) ? order.CompletedAt ?? now : null;

                if (string.IsNullOrWhiteSpace(info.PerformedDescriptionDescription))
                {
                    order.PerformedDescription = null;
                    order.PerformedDescriptionAt = null;
                }
                else if (info.PerformedDescriptionDescription != order.PerformedDescription)
                {
                    // A newer creation stamp dates the text exactly; an edit that kept it falls back
                    // to the last update, then to this read.
                    DateTimeOffset? created = FromUnixMilliseconds(info.PerformedDescriptionCreated);
                    order.PerformedDescriptionAt = IfNewer(created, order.PerformedDescriptionAt)
                        ?? IfNewer(updated, order.PerformedDescriptionAt)
                        ?? now;
                    order.PerformedDescription = info.PerformedDescriptionDescription;
                }

                read.Add(order);
            }

            await workOrderRepository.SaveStatusesAsync(read, cancellationToken);
        }

        if (!anyClaimed)
        {
            return (orders, WorkOrderRefreshOutcome.NotDue);
        }

        // The loaded entities hold the saved values.
        return (orders, anyFailed ? WorkOrderRefreshOutcome.Failed : WorkOrderRefreshOutcome.Refreshed);
    }

    private static DateTimeOffset? FromUnixMilliseconds(long? milliseconds) =>
        milliseconds is long ms ? DateTimeOffset.FromUnixTimeMilliseconds(ms) : null;

    /// <summary>The candidate when it is later than the previous date (or there is none), else null.</summary>
    private static DateTimeOffset? IfNewer(DateTimeOffset? candidate, DateTimeOffset? previous) =>
        candidate is not null && (previous is null || candidate > previous) ? candidate : null;

    private bool IsCompleted(string? statusCategory) =>
        _config.CompletedStatusCategories.Contains(statusCategory ?? "", StringComparer.OrdinalIgnoreCase);
}
