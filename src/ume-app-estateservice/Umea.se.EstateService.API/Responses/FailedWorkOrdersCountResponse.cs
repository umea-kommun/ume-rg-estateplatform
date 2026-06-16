namespace Umea.se.EstateService.API.Responses;

public sealed class FailedWorkOrdersCountResponse
{
    /// <summary>Number of permanently failed work orders awaiting admin remediation.</summary>
    public int Count { get; init; }
}
