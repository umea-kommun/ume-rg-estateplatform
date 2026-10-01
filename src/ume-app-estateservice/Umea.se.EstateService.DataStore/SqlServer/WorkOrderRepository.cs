using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Umea.se.EstateService.Shared.Data;
using Umea.se.EstateService.Shared.Data.Entities;
using Umea.se.EstateService.Shared.Data.Enums;

namespace Umea.se.EstateService.DataStore.SqlServer;

public class WorkOrderRepository(EstateDbContext dbContext) : IWorkOrderRepository
{
    public async Task AddAsync(WorkOrderEntity workOrder, CancellationToken cancellationToken = default)
    {
        dbContext.WorkOrders.Add(workOrder);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<WorkOrderEntity>> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        string normalizedEmail = NormalizeEmail(email);
        return await dbContext.WorkOrders
            .AsNoTracking()
            .Include(e => e.Files)
            .Where(e => e.CreatedByEmail == normalizedEmail || e.NotifierEmail == normalizedEmail)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<string?> GetLatestNotifierPhoneAsync(string email, CancellationToken cancellationToken = default)
    {
        // Most recent phone with real content that the user entered on their own reports.
        // Trim() (translated to LTRIM(RTRIM(...))) excludes whitespace-only values, not just
        // null/empty. Id breaks CreatedAt ties for a deterministic "most recent".
        string normalizedEmail = NormalizeEmail(email);
        return await dbContext.WorkOrders
            .AsNoTracking()
            .Where(e => e.CreatedByEmail == normalizedEmail && e.NotifierPhone != null && e.NotifierPhone.Trim() != "")
            .OrderByDescending(e => e.CreatedAt)
            .ThenByDescending(e => e.Id)
            .Select(e => e.NotifierPhone)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<WorkOrderEntity?> GetByUidAsync(Guid uid, string email, CancellationToken cancellationToken = default)
    {
        string normalizedEmail = NormalizeEmail(email);
        return await dbContext.WorkOrders
            .Include(e => e.Files)
            .FirstOrDefaultAsync(e => e.Uid == uid && (e.CreatedByEmail == normalizedEmail || e.NotifierEmail == normalizedEmail), cancellationToken);
    }

    // Admin overload: no email scoping. Tracking (no AsNoTracking) so the retry/dismiss paths
    // can mutate and persist the returned entity.
    public async Task<WorkOrderEntity?> GetByUidAsync(Guid uid, CancellationToken cancellationToken = default)
    {
        return await dbContext.WorkOrders
            .Include(e => e.Files)
            .FirstOrDefaultAsync(e => e.Uid == uid, cancellationToken);
    }

    public async Task<int> GetFailedCountAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.WorkOrders
            .AsNoTracking()
            .CountAsync(e => e.SyncStatus == WorkOrderSyncStatus.Failed && e.NextSyncAt == null, cancellationToken);
    }

    public async Task<IReadOnlyList<WorkOrderEntity>> GetFailedWorkOrdersAsync(CancellationToken cancellationToken = default)
    {
        // Permanently failed = exhausted all retries (Failed with no NextSyncAt scheduled).
        return await dbContext.WorkOrders
            .AsNoTracking()
            .Include(e => e.Files)
            .Where(e => e.SyncStatus == WorkOrderSyncStatus.Failed && e.NextSyncAt == null)
            .OrderByDescending(e => e.UpdatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<WorkOrderEntity>> GetDueForProcessingAsync(DateTimeOffset asOf, CancellationToken cancellationToken = default)
    {
        return await dbContext.WorkOrders
            .Include(e => e.Files)
            .Where(e => e.NextSyncAt != null && e.NextSyncAt <= asOf)
            .Where(e => e.SyncStatus == WorkOrderSyncStatus.Pending
                || e.SyncStatus == WorkOrderSyncStatus.Failed
                || e.SyncStatus == WorkOrderSyncStatus.Processing)
            .OrderBy(e => e.NextSyncAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> TryClaimForProcessingAsync(int id, DateTimeOffset processingTimeout, CancellationToken cancellationToken = default)
    {
        int updated = await dbContext.WorkOrders
            .Where(e => e.Id == id)
            .Where(e => e.SyncStatus == WorkOrderSyncStatus.Pending
                || e.SyncStatus == WorkOrderSyncStatus.Failed
                || (e.SyncStatus == WorkOrderSyncStatus.Processing && e.UpdatedAt < processingTimeout))
            .ExecuteUpdateAsync(s => s
                .SetProperty(e => e.SyncStatus, WorkOrderSyncStatus.Processing)
                .SetProperty(e => e.UpdatedAt, DateTimeOffset.UtcNow),
            cancellationToken);
        return updated > 0;
    }

    public async Task UpdateAsync(WorkOrderEntity workOrder, CancellationToken cancellationToken = default)
    {
        dbContext.WorkOrders.Update(workOrder);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<int>> ClaimForStatusRefreshAsync(IReadOnlyList<int> ids, DateTimeOffset staleBefore, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        // One conditional update for the whole set, so only one of two overlapping requests claims it.
        int updated = await dbContext.WorkOrders
            .Where(e => ids.Contains(e.Id) && (e.StatusCheckedAt == null || e.StatusCheckedAt <= staleBefore))
            .ExecuteUpdateAsync(s => s.SetProperty(e => e.StatusCheckedAt, now), cancellationToken);
        if (updated == 0)
        {
            return [];
        }

        // The stamp identifies the rows this call took; a losing caller left them untouched.
        return await dbContext.WorkOrders
            .Where(e => ids.Contains(e.Id) && e.StatusCheckedAt == now)
            .Select(e => e.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task SaveStatusesAsync(IReadOnlyList<WorkOrderEntity> workOrders, CancellationToken cancellationToken = default)
    {
        // Refresh-owned columns only: the entities are untracked
        // (GetByEmailAsync), so Update() would write every column.
        foreach (WorkOrderEntity workOrder in workOrders)
        {
            // The context may already track this row from an earlier tracked read in the same
            // scope, and Attach() on a second instance with the same key throws. FindEntry is an
            // identity-map lookup (no query); writing the values covers the tracked case and
            // is a self-assignment otherwise.
            EntityEntry<WorkOrderEntity> entry = dbContext.WorkOrders.Local.FindEntry(workOrder.Id)
                ?? dbContext.Attach(workOrder);
            entry.Entity.PythagorasWorkOrderName = workOrder.PythagorasWorkOrderName;
            entry.Entity.PythagorasStatusId = workOrder.PythagorasStatusId;
            entry.Entity.PythagorasStatusName = workOrder.PythagorasStatusName;
            entry.Entity.PythagorasStatusCategory = workOrder.PythagorasStatusCategory;
            entry.Entity.StatusChangedAt = workOrder.StatusChangedAt;
            entry.Entity.CompletedAt = workOrder.CompletedAt;
            entry.Entity.PerformedDescription = workOrder.PerformedDescription;
            entry.Entity.PerformedDescriptionAt = workOrder.PerformedDescriptionAt;

            entry.Property(e => e.PythagorasWorkOrderName).IsModified = true;
            entry.Property(e => e.PythagorasStatusId).IsModified = true;
            entry.Property(e => e.PythagorasStatusName).IsModified = true;
            entry.Property(e => e.PythagorasStatusCategory).IsModified = true;
            entry.Property(e => e.StatusChangedAt).IsModified = true;
            entry.Property(e => e.CompletedAt).IsModified = true;
            entry.Property(e => e.PerformedDescription).IsModified = true;
            entry.Property(e => e.PerformedDescriptionAt).IsModified = true;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    // Emails are persisted lower-cased on save (see WorkOrderHandler.SubmitWorkOrderAsync),
    // so every lookup by email must normalise the same way to match.
    private static string NormalizeEmail(string email) => email.ToLowerInvariant();
}
