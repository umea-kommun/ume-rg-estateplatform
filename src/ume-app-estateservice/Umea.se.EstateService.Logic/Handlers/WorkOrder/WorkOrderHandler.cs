using System.Globalization;
using Microsoft.Extensions.Logging;
using Umea.se.EstateService.Logic.HostedServices;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Enums;
using Umea.se.EstateService.Shared.Data;
using Umea.se.EstateService.Shared.Data.Entities;
using Umea.se.EstateService.Shared.Data.Enums;
using Umea.se.EstateService.Shared.Exceptions;
using Umea.se.EstateService.Shared.Infrastructure;
using Umea.se.EstateService.Shared.Models;
using Umea.se.Toolkit.Logging;

namespace Umea.se.EstateService.Logic.Handlers.WorkOrder;

public class WorkOrderHandler(
    IWorkOrderRepository workOrderRepository,
    IDataStore dataStore,
    WorkOrderChannel workOrderChannel,
    IWorkOrderFileStorage fileStorage,
    WorkOrderFileValidator fileValidator,
    WorkOrderCategoryProvider categoryProvider,
    WorkOrderAccessPolicy accessPolicy,
    WorkOrderStatusSyncService statusSyncService,
    ILogger<WorkOrderHandler> logger) : IWorkOrderHandler
{
    // Custom event emitted on every successful submission
    private const string WorkOrderSubmittedEventName = "EstateWorkOrderSubmitted";

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

        // Building is mandatory for every type except SpaceRequirement, which may be submitted
        // without one (Pythagoras does not require a bound object for type 3 — see findings doc).
        BuildingEntity? building = null;
        if (request.BuildingId.HasValue)
        {
            if (!dataStore.BuildingsById.TryGetValue(request.BuildingId.Value, out building))
            {
                errors.AddError("buildingId", ValidationErrorCode.NotFound);
            }
            else if (!building.WorkOrderTypes.Contains(request.WorkOrderType))
            {
                errors.AddError("workOrderType", ValidationErrorCode.NotSupported);
            }
        }
        else if (request.WorkOrderType != WorkOrderType.SpaceRequirement)
        {
            errors.AddError("buildingId", ValidationErrorCode.Required);
        }

        int? roomId = null;
        string? roomName = null;
        string? roomPopularName = null;
        // Room applies to all work order types (fault reports and orders). For orders
        // location is never set, so the Outdoor conflict check below is a no-op for them.
        if (request.RoomId.HasValue)
        {
            if (!request.BuildingId.HasValue)
            {
                // A room can't be bound without its building.
                errors.AddError("roomId", ValidationErrorCode.InvalidValue);
            }
            else if (location == WorkOrderLocation.Outdoor)
            {
                errors.AddError("roomId", ValidationErrorCode.Conflict);
            }
            else if (!dataStore.RoomsById.TryGetValue(request.RoomId.Value, out RoomEntity? room))
            {
                errors.AddError("roomId", ValidationErrorCode.NotFound);
            }
            else if (room.BuildingId != request.BuildingId.Value)
            {
                errors.AddError("roomId", ValidationErrorCode.InvalidValue);
            }
            else
            {
                roomId = room.Id;
                roomName = room.Name;
                roomPopularName = room.PopularName;
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
            // The popular name is what users recognise the building by; Name is the fallback
            // for buildings Pythagoras has no popular name for.
            BuildingName = building is null
                ? null
                : (!string.IsNullOrWhiteSpace(building.PopularName) ? building.PopularName : building.Name),
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

        TrackWorkOrderSubmitted(request, workOrderType, building, roomId, roomName, roomPopularName, location);

        return WorkOrderMapper.MapToSubmission(workOrder);
    }

    // Buckets the fine-grained work order type into the errand kinds the users think in: a fault
    // report, a space requirement, or an order. The raw type is still logged separately as
    // WorkOrderType, which doubles as the order category for orders. Every order type is listed
    // explicitly so a type added later falls to "N/A" (prompting a deliberate decision) rather than
    // being silently swept into "Order".
    private static string ResolveErrandType(WorkOrderType type) => type switch
    {
        WorkOrderType.ErrorReport => "FaultReport",
        WorkOrderType.SpaceRequirement => "SpaceRequirement",
        WorkOrderType.BuildingService
            or WorkOrderType.FacilityService
            or WorkOrderType.TownHallService => "Order",
        _ => "N/A",
    };

    private void TrackWorkOrderSubmitted(
        CreateWorkOrderRequest request,
        PythagorasWorkOrderType workOrderType,
        BuildingEntity? building,
        int? roomId,
        string? roomName,
        string? roomPopularName,
        WorkOrderLocation? location)
    {
        logger.LogCustomEvent(WorkOrderSubmittedEventName, options =>
        {
            options.WithProperty("ErrandType", ResolveErrandType(request.WorkOrderType));
            options.WithProperty("WorkOrderType", request.WorkOrderType.ToString());

            // Building is absent for space requirements submitted without one; "N/A" keeps those
            // apart from a real "No" so the dimensions aren't muddled in App Insights.
            options.WithProperty("BuildingId", building?.Id.ToString(CultureInfo.InvariantCulture) ?? "N/A");
            // Log the popular (human-recognisable) name, falling back to the formal name when a
            // building has none.
            options.WithProperty("BuildingName", building is null
                ? "N/A"
                : string.IsNullOrWhiteSpace(building.PopularName) ? building.Name : building.PopularName);
            options.WithProperty("HasRoomInformation", building is null
                ? "N/A"
                : EstateModelMapper.HasRoomInformation(building) ? "Yes" : "No");
            options.WithProperty("HasBlueprint", building is null
                ? "N/A"
                : building.BlueprintAvailable == true ? "Yes" : "No");

            options.WithProperty("RoomId", roomId?.ToString(CultureInfo.InvariantCulture) ?? "N/A");
            // Log the formal name and the popular name together ("Name - PopularName"), falling
            // back to just the name when a room has no popular name.
            options.WithProperty("RoomName", roomName is null
                ? "N/A"
                : string.IsNullOrWhiteSpace(roomPopularName) ? roomName : $"{roomName} - {roomPopularName}");

            // Indoor/Outdoor is only asked for (and posted) on fault reports; "N/A" for the other
            // types, which don't carry a location.
            options.WithProperty("Location", location?.ToString() ?? "N/A");

            // Count doubles as the "did they attach anything?" flag — 0 means none.
            options.WithProperty("AttachmentCount", (request.Files?.Count ?? 0).ToString(CultureInfo.InvariantCulture));

            // Only types where the user picks an explicit leaf category (e.g. SpaceRequirement)
            // carry a CategoryId; resolve its human-readable path so the event is self-describing.
            if (request.CategoryId.HasValue)
            {
                options.WithProperty("CategoryId", request.CategoryId.Value.ToString(CultureInfo.InvariantCulture));

                string? categoryName = categoryProvider
                    .GetLeafCategoriesForType((int)workOrderType)
                    .FirstOrDefault(c => c.Id == request.CategoryId.Value)?.Name;
                options.WithProperty("CategoryName", categoryName ?? "N/A");
            }
        });
    }

    public async Task<IReadOnlyList<WorkOrderListItemModel>> GetWorkOrdersAsync(string email, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<WorkOrderEntity> entities = await workOrderRepository.GetByEmailAsync(email, cancellationToken);
        return WorkOrderMapper.MapToListItems(entities, dataStore.BuildingsById);
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

    public Task<WorkOrderDetailModel> SyncWorkOrderAsync(Guid uid, string email, CancellationToken cancellationToken = default)
        => statusSyncService.RefreshOneAsync(email, uid, cancellationToken);

    public Task<WorkOrderRefreshModel> SyncWorkOrdersAsync(string email, CancellationToken cancellationToken = default)
        => statusSyncService.RefreshAsync(email, cancellationToken);

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
