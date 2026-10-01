namespace Umea.se.EstateService.Shared.Models;

public class WorkOrderDetailModel
{
    /// <summary>Set only by the refresh endpoints; null on a plain read.</summary>
    public WorkOrderRefreshOutcome? RefreshOutcome { get; init; }
    public Guid Id { get; init; }
    public WorkOrderType? WorkOrderType { get; init; }
    public string? WorkOrderNumber { get; init; }
    public string? BuildingName { get; init; }
    public string? RoomName { get; init; }
    public string? Location { get; init; }
    public string Description { get; init; } = null!;
    public string SyncStatus { get; init; } = null!;
    public string? Status { get; init; }
    public string? StatusCategory { get; init; }
    public WorkOrderDisplayStatus? DisplayStatus { get; init; }
    public DateTimeOffset? StatusCheckedAt { get; init; }
    public string? PerformedDescription { get; init; }
    public DateTimeOffset? PerformedDescriptionAt { get; init; }
    public int? PythagorasWorkOrderId { get; init; }
    public string? ErrorMessage { get; init; }
    public int FileCount { get; init; }
    public List<WorkOrderFileModel> Files { get; init; } = [];
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? SubmittedAt { get; init; }
}

public class WorkOrderListItemModel
{
    public Guid Id { get; init; }
    public WorkOrderType? WorkOrderType { get; init; }
    public string? WorkOrderNumber { get; init; }
    public int? BuildingId { get; init; }
    public string? BuildingName { get; init; }

    /// <summary>Popular name from the data store; null when the building is unknown.</summary>
    public string? BuildingPopularName { get; init; }

    /// <summary>Null when the building is unknown or has no images.</summary>
    public string? BuildingImageUrl { get; init; }
    public string? RoomName { get; init; }
    public string? Location { get; init; }
    public string Description { get; init; } = null!;
    public string SyncStatus { get; init; } = null!;
    public string? Status { get; init; }
    public string? StatusCategory { get; init; }
    public WorkOrderDisplayStatus? DisplayStatus { get; init; }
    public DateTimeOffset? StatusCheckedAt { get; init; }
    public string? PerformedDescription { get; init; }
    public DateTimeOffset? PerformedDescriptionAt { get; init; }
    public int FileCount { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? SubmittedAt { get; init; }
    public DateTimeOffset LastChangedAt { get; init; }
}

public class WorkOrderSubmissionModel
{
    public Guid Id { get; init; }
    public string SyncStatus { get; init; } = null!;
    public DateTimeOffset CreatedAt { get; init; }
}

public class WorkOrderFileModel
{
    public string FileName { get; init; } = null!;
    public long FileSize { get; init; }
    public bool Uploaded { get; init; }
}

/// <summary>
/// Admin view of a permanently failed work order, surfaced on the status dashboard. Carries the
/// diagnostic fields the dashboard needs (error message, retry count, timestamps) plus the
/// attached file names for the manual-upload workflow.
/// </summary>
public class FailedWorkOrderModel
{
    public Guid Id { get; init; }
    public WorkOrderType? WorkOrderType { get; init; }
    public string? BuildingName { get; init; }
    public string? RoomName { get; init; }
    public string Description { get; init; } = null!;
    public string SyncStatus { get; init; } = null!;
    public string? ErrorMessage { get; init; }
    public int RetryCount { get; init; }
    public int FileCount { get; init; }
    public List<WorkOrderFileModel> Files { get; init; } = [];
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}

/// <summary>Status refresh result; failed reads keep the saved status.</summary>
public sealed class WorkOrderRefreshModel
{
    public IReadOnlyList<WorkOrderListItemModel> WorkOrders { get; init; } = [];
    public WorkOrderRefreshOutcome Outcome { get; init; } = WorkOrderRefreshOutcome.NotDue;
}
