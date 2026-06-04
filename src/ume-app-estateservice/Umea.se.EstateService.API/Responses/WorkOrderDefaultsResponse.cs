namespace Umea.se.EstateService.API.Responses;

public sealed class WorkOrderDefaultsResponse
{
    /// <summary>Phone number from the user's most recent submission, or null if they have none.</summary>
    public string? NotifierPhone { get; init; }
}
