namespace Umea.se.EstateService.Shared.Models;

public sealed class FeedbackModel
{
    public required string Category { get; init; }
    public required int Rating { get; init; }

    /// <summary>Null when the caller's token carries no parsable SSN.</summary>
    public string? LoggedInPersonGender { get; init; }

    /// <summary>Null when the caller's token carries no parsable SSN.</summary>
    public int? LoggedInPersonAge { get; init; }

    public Dictionary<string, object> AdditionalInfo { get; init; } = [];
    public string? Comment { get; init; }
}
