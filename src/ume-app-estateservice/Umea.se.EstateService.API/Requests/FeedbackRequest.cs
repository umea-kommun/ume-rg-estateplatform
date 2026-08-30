namespace Umea.se.EstateService.API.Requests;

public class FeedbackRequest
{
    public required string Category { get; init; }
    public required int Rating { get; init; }
    public Dictionary<string, object> AdditionalInfo { get; init; } = [];
    public string? Comment { get; init; }
}
