namespace Umea.se.EstateService.Shared.Models;

/// <summary>Result of a status refresh. Refresh endpoints return 200 even on upstream failure, so check this.</summary>
public enum WorkOrderRefreshOutcome
{
    /// <summary>Every claimed order got a status.</summary>
    Refreshed,

    /// <summary>No order was due.</summary>
    NotDue,

    /// <summary>At least one claimed order got no status; saved values are returned.</summary>
    Failed,

    /// <summary>WorkOrder:StatusSyncEnabled is off.</summary>
    Disabled,
}
