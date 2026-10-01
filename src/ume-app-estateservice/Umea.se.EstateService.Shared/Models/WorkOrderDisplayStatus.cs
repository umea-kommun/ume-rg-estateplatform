namespace Umea.se.EstateService.Shared.Models;

/// <summary>
/// What a Pythagoras status means to the person who reported the order: a few statuses by name,
/// the rest by category. Null until a status is read.
/// </summary>
public enum WorkOrderDisplayStatus
{
    /// <summary>Category NOT_STARTED.</summary>
    Received,

    /// <summary>Category ONGOING.</summary>
    InProgress,

    /// <summary>Categories PERFORMED and COMPLETED.</summary>
    Closed,

    /// <summary>Status name "Vilande".</summary>
    OnHold,

    /// <summary>Status name "Beställt material".</summary>
    MaterialOrdered,

    /// <summary>Status name "Skickad till entreprenör".</summary>
    SentToContractor,
}
