using Umea.se.EstateService.ServiceAccess.Pythagoras.Enums;
using Umea.se.EstateService.Shared.Data.Entities;
using Umea.se.EstateService.Shared.Data.Enums;
using Umea.se.EstateService.Shared.Models;

namespace Umea.se.EstateService.Logic.Handlers.WorkOrder;

internal static class WorkOrderMapper
{
    private static readonly Dictionary<PythagorasWorkOrderType, WorkOrderType> _reverseTypeMap = new()
    {
        [PythagorasWorkOrderType.ErrorReport] = WorkOrderType.ErrorReport,
        [PythagorasWorkOrderType.BuildingService] = WorkOrderType.BuildingService,
        [PythagorasWorkOrderType.FacilityService] = WorkOrderType.FacilityService,
        [PythagorasWorkOrderType.TownHallService] = WorkOrderType.TownHallService,
        [PythagorasWorkOrderType.SpaceRequirement] = WorkOrderType.SpaceRequirement,
    };

    private static WorkOrderType? MapWorkOrderType(int workOrderTypeId) =>
        _reverseTypeMap.GetValueOrDefault((PythagorasWorkOrderType)workOrderTypeId);

    // Matched by name: a status renamed in Pythagoras falls back to its category.
    private static readonly Dictionary<string, WorkOrderDisplayStatus> _displayStatusByName = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Vilande"] = WorkOrderDisplayStatus.OnHold,
        ["Beställt material"] = WorkOrderDisplayStatus.MaterialOrdered,
        ["Skickad till entreprenör"] = WorkOrderDisplayStatus.SentToContractor,
    };

    /// <summary>Maps a few statuses by name and the rest by category. Unknown categories map to null.</summary>
    internal static WorkOrderDisplayStatus? MapDisplayStatus(string? statusName, string? statusCategory) =>
        statusName is not null && _displayStatusByName.TryGetValue(statusName.Trim(), out WorkOrderDisplayStatus displayStatus)
            ? displayStatus
            : MapStatusCategory(statusCategory);

    private static WorkOrderDisplayStatus? MapStatusCategory(string? statusCategory) => statusCategory?.ToUpperInvariant() switch
    {
        "NOT_STARTED" => WorkOrderDisplayStatus.Received,
        "ONGOING" => WorkOrderDisplayStatus.InProgress,
        "PERFORMED" or "COMPLETED" => WorkOrderDisplayStatus.Closed,
        _ => null,
    };

    public static WorkOrderSubmissionModel MapToSubmission(WorkOrderEntity entity) => new()
    {
        Id = entity.Uid,
        SyncStatus = entity.SyncStatus.ToString(),
        CreatedAt = entity.CreatedAt
    };

    public static WorkOrderDetailModel MapToDetail(WorkOrderEntity entity, WorkOrderRefreshOutcome? refreshOutcome = null) => new()
    {
        RefreshOutcome = refreshOutcome,
        Id = entity.Uid,
        WorkOrderType = MapWorkOrderType(entity.WorkOrderTypeId),
        WorkOrderNumber = string.IsNullOrEmpty(entity.PythagorasWorkOrderName) ? null : entity.PythagorasWorkOrderName,
        BuildingName = entity.BuildingName,
        RoomName = entity.RoomName,
        Location = entity.Location?.ToString(),
        Description = entity.Description,
        SyncStatus = entity.SyncStatus.ToString(),
        Status = entity.PythagorasStatusName,
        StatusCategory = entity.PythagorasStatusCategory,
        DisplayStatus = MapDisplayStatus(entity.PythagorasStatusName, entity.PythagorasStatusCategory),
        StatusCheckedAt = entity.StatusCheckedAt,
        PerformedDescription = entity.PerformedDescription,
        PerformedDescriptionAt = entity.PerformedDescriptionAt,
        PythagorasWorkOrderId = entity.PythagorasWorkOrderId,
        ErrorMessage = entity.ErrorMessage,
        FileCount = entity.Files.Count,
        Files = [.. entity.Files.Select(f => new WorkOrderFileModel
        {
            FileName = f.FileName,
            FileSize = f.FileSize,
            Uploaded = f.Uploaded
        })],
        CreatedAt = entity.CreatedAt,
        SubmittedAt = entity.SubmittedAt
    };

    public static FailedWorkOrderModel MapToFailed(WorkOrderEntity entity) => new()
    {
        Id = entity.Uid,
        WorkOrderType = MapWorkOrderType(entity.WorkOrderTypeId),
        BuildingName = entity.BuildingName,
        RoomName = entity.RoomName,
        Description = entity.Description,
        SyncStatus = entity.SyncStatus.ToString(),
        ErrorMessage = entity.ErrorMessage,
        RetryCount = entity.RetryCount,
        FileCount = entity.Files.Count,
        Files = [.. entity.Files.Select(f => new WorkOrderFileModel
        {
            FileName = f.FileName,
            FileSize = f.FileSize,
            Uploaded = f.Uploaded
        })],
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt
    };

    public static List<WorkOrderListItemModel> MapToListItems(
        IReadOnlyList<WorkOrderEntity> entities, IReadOnlyDictionary<int, BuildingEntity> buildingsById) =>
    [
        .. entities
            .Select(e =>
            {
                BuildingEntity? building = e.BuildingId.HasValue ? buildingsById.GetValueOrDefault(e.BuildingId.Value) : null;
                return MapToListItem(e, building);
            })
            .OrderByDescending(item => item.LastChangedAt)
            .ThenByDescending(item => item.CreatedAt)
    ];

    private static DateTimeOffset LastChangedAt(WorkOrderEntity e)
    {
        DateTimeOffset latest = e.CreatedAt;
        if (e.StatusChangedAt > latest)
        {
            latest = e.StatusChangedAt.Value;
        }

        if (e.PerformedDescriptionAt > latest)
        {
            latest = e.PerformedDescriptionAt.Value;
        }

        // Dismissal is terminal, so UpdatedAt is when the admin dismissed it.
        if (e.SyncStatus == WorkOrderSyncStatus.Dismissed && e.UpdatedAt > latest)
        {
            latest = e.UpdatedAt;
        }

        return latest;
    }

    private static WorkOrderListItemModel MapToListItem(WorkOrderEntity e, BuildingEntity? building) => new()
    {
        Id = e.Uid,
        WorkOrderType = MapWorkOrderType(e.WorkOrderTypeId),
        WorkOrderNumber = string.IsNullOrEmpty(e.PythagorasWorkOrderName) ? null : e.PythagorasWorkOrderName,
        BuildingId = e.BuildingId,
        BuildingName = e.BuildingName,
        BuildingPopularName = building is null || string.IsNullOrWhiteSpace(building.PopularName) ? null : building.PopularName,
        BuildingImageUrl = building is null ? null : EstateDataQueryHandler.GetBuildingImageUrl(building),
        RoomName = e.RoomName,
        Location = e.Location?.ToString(),
        Description = e.Description,
        SyncStatus = e.SyncStatus.ToString(),
        Status = e.PythagorasStatusName,
        StatusCategory = e.PythagorasStatusCategory,
        DisplayStatus = MapDisplayStatus(e.PythagorasStatusName, e.PythagorasStatusCategory),
        StatusCheckedAt = e.StatusCheckedAt,
        PerformedDescription = e.PerformedDescription,
        PerformedDescriptionAt = e.PerformedDescriptionAt,
        FileCount = e.Files.Count,
        CreatedAt = e.CreatedAt,
        SubmittedAt = e.SubmittedAt,
        LastChangedAt = LastChangedAt(e)
    };
}
