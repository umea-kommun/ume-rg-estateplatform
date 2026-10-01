using Umea.se.EstateService.Shared.Data.Enums;

namespace Umea.se.EstateService.Shared.Data.Entities;

public class WorkOrderEntity : BaseEntity
{
    // Building is optional: SpaceRequirement work orders may be submitted without a building
    // (Pythagoras does not require a bound object for type 3). All other types still set it.
    public int? BuildingId { get; set; }
    public string? BuildingName { get; set; }

    public int? RoomId { get; set; }
    public string? RoomName { get; set; }

    public WorkOrderLocation? Location { get; set; }
    public string Description { get; set; } = string.Empty;
    public WorkOrderSyncStatus SyncStatus { get; set; }
    public int WorkOrderTypeId { get; set; }

    public int? PythagorasWorkOrderId { get; set; }

    /// <summary>
    /// Work order number from Pythagoras (e.g. "UK-2026-2121"). Set at creation and on status refresh.
    /// Null means not read yet; empty means read but Pythagoras had no number.
    /// </summary>
    public string? PythagorasWorkOrderName { get; set; }

    public int? CategoryId { get; set; }

    /// <summary>Status ID from Pythagoras. Refreshed on demand after submission.</summary>
    public int? PythagorasStatusId { get; set; }

    /// <summary>Status name from Pythagoras (e.g. "Registrerad", "Tilldelad").</summary>
    public string? PythagorasStatusName { get; set; }
    public string? PythagorasStatusCategory { get; set; }

    /// <summary>
    /// When a refresh saw the status change. Pythagoras has no status timestamp, so this is its last update
    /// when that moved, otherwise the time of the refresh.
    /// </summary>
    public DateTimeOffset? StatusChangedAt { get; set; }

    /// <summary>Last status refresh attempt, stamped before Pythagoras is called.</summary>
    public DateTimeOffset? StatusCheckedAt { get; set; }

    /// <summary>When a refresh first saw a completed status. Cleared if the order is reopened.</summary>
    public DateTimeOffset? CompletedAt { get; set; }

    /// <summary>"Beskriv utfört arbete" from Pythagoras.</summary>
    public string? PerformedDescription { get; set; }

    /// <summary>When the performed description was last added or edited.</summary>
    public DateTimeOffset? PerformedDescriptionAt { get; set; }

    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; }
    public DateTimeOffset? NextSyncAt { get; set; }

    public DateTimeOffset? SubmittedAt { get; set; }
    public string CreatedByEmail { get; set; } = string.Empty;
    public string? NotifierEmail { get; set; }
    public string? NotifierName { get; set; }
    public string? NotifierPhone { get; set; }

    public List<WorkOrderFileEntity> Files { get; set; } = [];
}
