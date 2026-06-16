using Microsoft.Extensions.Logging;
using Umea.se.EstateService.Logic.HostedServices;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Enums;
using Umea.se.EstateService.Shared.Data;
using Umea.se.EstateService.Shared.Data.Entities;
using Umea.se.EstateService.Shared.Data.Enums;
using Umea.se.EstateService.Shared.Exceptions;
using Umea.se.EstateService.Shared.Infrastructure;
using Umea.se.EstateService.Shared.Models;

namespace Umea.se.EstateService.Logic.Handlers.WorkOrder;

public class WorkOrderHandler(
    IWorkOrderRepository workOrderRepository,
    IDataStore dataStore,
    WorkOrderChannel workOrderChannel,
    IWorkOrderFileStorage fileStorage,
    WorkOrderFileValidator fileValidator,
    WorkOrderCategoryProvider categoryProvider,
    WorkOrderAccessPolicy accessPolicy,
    ILogger<WorkOrderHandler> logger) : IWorkOrderHandler
{
    private static readonly Dictionary<WorkOrderType, PythagorasWorkOrderType> _workOrderTypeMap = new()
    {
        [WorkOrderType.ErrorReport] = PythagorasWorkOrderType.ErrorReport,
        [WorkOrderType.BuildingService] = PythagorasWorkOrderType.BuildingService,
        [WorkOrderType.FacilityService] = PythagorasWorkOrderType.FacilityService,
        [WorkOrderType.TownHallService] = PythagorasWorkOrderType.TownHallService,
        [WorkOrderType.SpaceRequirement] = PythagorasWorkOrderType.SpaceRequirement,
    };

    public IReadOnlyList<WorkOrderCategoryOption> GetCategoriesForType(WorkOrderType type, IReadOnlyCollection<string>? userGroups = null)
    {
        return accessPolicy.IsTypeAllowed(type, userGroups)
            && _workOrderTypeMap.TryGetValue(type, out PythagorasWorkOrderType pythagorasType)
            ? categoryProvider.GetLeafCategoriesForType((int)pythagorasType)
            : [];
    }

    public async Task<WorkOrderSubmissionModel> SubmitWorkOrderAsync(CreateWorkOrderRequest request, string email, IReadOnlyCollection<string>? userGroups = null, CancellationToken cancellationToken = default)
    {
        ValidationErrorBuilder errors = new();

        bool typeResolved = _workOrderTypeMap.TryGetValue(request.WorkOrderType, out PythagorasWorkOrderType workOrderType);
        if (!typeResolved)
        {
            errors.AddError("workOrderType", ValidationErrorCode.InvalidValue);
        }
        else if (!accessPolicy.IsTypeAllowed(request.WorkOrderType, userGroups))
        {
            // Type is restricted to an AAD group the user isn't in. Reported as NotSupported so
            // it can't be distinguished from "building doesn't offer this type" — the type is
            // never advertised to non-members in the first place (building info strips it).
            errors.AddError("workOrderType", ValidationErrorCode.NotSupported);
        }

        bool isErrorReport = request.WorkOrderType == WorkOrderType.ErrorReport;
        WorkOrderLocation? location = null;

        if (isErrorReport)
        {
            if (string.IsNullOrWhiteSpace(request.Location))
            {
                errors.AddError("location", ValidationErrorCode.Required);
            }
            else if (!Enum.TryParse(request.Location, ignoreCase: true, out WorkOrderLocation parsed))
            {
                errors.AddError("location", ValidationErrorCode.InvalidValue);
            }
            else
            {
                location = parsed;
            }
        }

        if (!dataStore.BuildingsById.TryGetValue(request.BuildingId, out BuildingEntity? building))
        {
            errors.AddError("buildingId", ValidationErrorCode.NotFound);
        }
        else if (!building.WorkOrderTypes.Contains(request.WorkOrderType))
        {
            errors.AddError("workOrderType", ValidationErrorCode.NotSupported);
        }

        int? roomId = null;
        string? roomName = null;
        // Room applies to all work order types (fault reports and orders). For orders
        // location is never set, so the Outdoor conflict check below is a no-op for them.
        if (request.RoomId.HasValue)
        {
            if (location == WorkOrderLocation.Outdoor)
            {
                errors.AddError("roomId", ValidationErrorCode.Conflict);
            }
            else if (!dataStore.RoomsById.TryGetValue(request.RoomId.Value, out RoomEntity? room))
            {
                errors.AddError("roomId", ValidationErrorCode.NotFound);
            }
            else if (room.BuildingId != request.BuildingId)
            {
                errors.AddError("roomId", ValidationErrorCode.InvalidValue);
            }
            else
            {
                roomId = room.Id;
                roomName = room.Name;
            }
        }

        // When the user explicitly picks a category (e.g. SpaceRequirement), validate it is a
        // real leaf category for the type. A persisted CategoryId makes the processor skip the
        // classifier, so a bad id would otherwise be sent straight to Pythagoras.
        if (typeResolved && request.CategoryId.HasValue)
        {
            IReadOnlyList<WorkOrderCategoryOption> categories = categoryProvider.GetLeafCategoriesForType((int)workOrderType);
            if (categories.All(c => c.Id != request.CategoryId.Value))
            {
                errors.AddError("categoryId", ValidationErrorCode.InvalidValue);
            }
        }

        errors.ThrowIfErrors();

        WorkOrderEntity workOrder = new()
        {
            Uid = Guid.NewGuid(),
            BuildingId = request.BuildingId,
            BuildingName = building!.Name,
            RoomId = roomId,
            RoomName = roomName,
            Location = location,
            WorkOrderTypeId = (int)workOrderType,
            CategoryId = request.CategoryId,
            Description = request.Description,
            SyncStatus = WorkOrderSyncStatus.Pending,
            NextSyncAt = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            CreatedByEmail = email.ToLowerInvariant(),
            NotifierEmail = (!string.IsNullOrWhiteSpace(request.NotifierEmail)
                ? request.NotifierEmail
                : email).ToLowerInvariant(),
            NotifierName = request.NotifierName,
            NotifierPhone = request.NotifierPhone
        };

        if (request.Files is { Count: > 0 })
        {
            await fileValidator.ValidateAsync(request.Files, cancellationToken);

            foreach (WorkOrderFileUpload file in request.Files)
            {
                string relativePath = Path.Combine(workOrder.Uid.ToString(), file.FileName);

                await fileStorage.SaveAsync(relativePath, file.Stream, cancellationToken);

                workOrder.Files.Add(new WorkOrderFileEntity
                {
                    FileName = file.FileName,
                    ContentType = file.ContentType,
                    FileSize = file.FileSize,
                    StoragePath = relativePath,
                    CreatedAt = DateTimeOffset.UtcNow
                });
            }
        }

        await workOrderRepository.AddAsync(workOrder, cancellationToken);

        workOrderChannel.Notify(workOrder.Uid);

        logger.LogInformation("WorkOrder {WorkOrderUid} created for building {BuildingId} by {Email}", workOrder.Uid, workOrder.BuildingId, email);

        return WorkOrderMapper.MapToSubmission(workOrder);
    }

    public async Task<IReadOnlyList<WorkOrderListItemModel>> GetWorkOrdersAsync(string email, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<WorkOrderEntity> entities = await workOrderRepository.GetByEmailAsync(email, cancellationToken);
        return WorkOrderMapper.MapToListItems(entities);
    }

    public Task<string?> GetLatestNotifierPhoneAsync(string email, CancellationToken cancellationToken = default)
        => workOrderRepository.GetLatestNotifierPhoneAsync(email, cancellationToken);

    public async Task<WorkOrderDetailModel> GetWorkOrderAsync(Guid uid, string email, CancellationToken cancellationToken = default)
    {
        WorkOrderEntity? workOrder = await workOrderRepository.GetByUidAsync(uid, email, cancellationToken);
        return workOrder is null
            ? throw new EntityNotFoundException($"Work order {uid} not found.")
            : WorkOrderMapper.MapToDetail(workOrder);
    }

    public async Task<WorkOrderDetailModel> SyncWorkOrderAsync(Guid uid, string email, CancellationToken cancellationToken = default)
    {
        WorkOrderEntity? workOrder = await workOrderRepository.GetByUidAsync(uid, email, cancellationToken);
        if (workOrder is null)
        {
            throw new EntityNotFoundException($"Work order {uid} not found.");
        }

        if (workOrder is { SyncStatus: WorkOrderSyncStatus.Submitted, PythagorasWorkOrderId: not null })
        {
            workOrder.NextSyncAt = DateTimeOffset.UtcNow;
            workOrder.UpdatedAt = DateTimeOffset.UtcNow;
            await workOrderRepository.UpdateAsync(workOrder, cancellationToken);
            workOrderChannel.Notify(workOrder.Uid);
        }

        return WorkOrderMapper.MapToDetail(workOrder);
    }

    public async Task<WorkOrderDetailModel> RetryWorkOrderAsync(Guid uid, string email, CancellationToken cancellationToken = default)
    {
        WorkOrderEntity? workOrder = await workOrderRepository.GetByUidAsync(uid, email, cancellationToken);
        if (workOrder is null)
        {
            throw new EntityNotFoundException($"Work order {uid} not found.");
        }

        if (workOrder.SyncStatus is not WorkOrderSyncStatus.Failed || workOrder.NextSyncAt is not null)
        {
            throw new StateConflictException("Work order is not in a permanently failed state.");
        }

        RequeueForRetry(workOrder);
        await workOrderRepository.UpdateAsync(workOrder, cancellationToken);

        workOrderChannel.Notify(workOrder.Uid);

        logger.LogInformation("WorkOrder {WorkOrderUid} manually queued for retry by {Email}.", workOrder.Uid, email);

        return WorkOrderMapper.MapToDetail(workOrder);
    }

    public Task<int> GetFailedCountAsync(CancellationToken cancellationToken = default)
        => workOrderRepository.GetFailedCountAsync(cancellationToken);

    public async Task<IReadOnlyList<FailedWorkOrderModel>> GetFailedWorkOrdersAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<WorkOrderEntity> entities = await workOrderRepository.GetFailedWorkOrdersAsync(cancellationToken);
        return [.. entities.Select(WorkOrderMapper.MapToFailed)];
    }

    public async Task<FailedWorkOrderModel> AdminRetryWorkOrderAsync(Guid uid, CancellationToken cancellationToken = default)
    {
        WorkOrderEntity workOrder = await GetPermanentlyFailedAsync(uid, cancellationToken);

        RequeueForRetry(workOrder);
        await workOrderRepository.UpdateAsync(workOrder, cancellationToken);

        workOrderChannel.Notify(workOrder.Uid);

        logger.LogInformation("WorkOrder {WorkOrderUid} manually queued for retry by admin.", workOrder.Uid);

        return WorkOrderMapper.MapToFailed(workOrder);
    }

    public async Task<FailedWorkOrderModel> AdminDismissWorkOrderAsync(Guid uid, CancellationToken cancellationToken = default)
    {
        WorkOrderEntity workOrder = await GetPermanentlyFailedAsync(uid, cancellationToken);

        // Keep ErrorMessage as-is — it documents why the order could never be submitted.
        workOrder.SyncStatus = WorkOrderSyncStatus.Dismissed;
        workOrder.UpdatedAt = DateTimeOffset.UtcNow;
        await workOrderRepository.UpdateAsync(workOrder, cancellationToken);

        logger.LogInformation("WorkOrder {WorkOrderUid} dismissed (manually resolved) by admin.", workOrder.Uid);

        return WorkOrderMapper.MapToFailed(workOrder);
    }

    // Loads a work order by uid (no email scope) and enforces that it is permanently failed —
    // the only state from which an admin may retry or dismiss it.
    private async Task<WorkOrderEntity> GetPermanentlyFailedAsync(Guid uid, CancellationToken cancellationToken)
    {
        WorkOrderEntity? workOrder = await workOrderRepository.GetByUidAsync(uid, cancellationToken);
        if (workOrder is null)
        {
            throw new EntityNotFoundException($"Work order {uid} not found.");
        }

        if (workOrder.SyncStatus is not WorkOrderSyncStatus.Failed || workOrder.NextSyncAt is not null)
        {
            throw new StateConflictException("Work order is not in a permanently failed state.");
        }

        return workOrder;
    }

    // Resets a permanently failed order back to the processing queue. ErrorMessage is deliberately
    // left intact: the processor overwrites it on the next failed attempt and clears it on success,
    // so nulling it here would just hide the failure reason while the retry is in flight.
    private static void RequeueForRetry(WorkOrderEntity workOrder)
    {
        workOrder.SyncStatus = WorkOrderSyncStatus.Pending;
        workOrder.RetryCount = 0;
        workOrder.NextSyncAt = DateTimeOffset.UtcNow;
        workOrder.UpdatedAt = DateTimeOffset.UtcNow;
    }
}
