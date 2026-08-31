using Umea.se.EstateService.Shared.Models;

namespace Umea.se.EstateService.API.Responses;

/// <summary>
/// The calling user: identity from the token plus what they are allowed to do, so the
/// client never has to inspect the token itself. Capabilities only - never the AAD
/// groups behind them, so the policy stays in configuration and off the client.
/// Add a new property per area as more user-specific gates appear.
/// </summary>
public sealed class CurrentUserResponse
{
    public string? Email { get; init; }

    public string? FullName { get; init; }

    public CurrentUserPermissions Permissions { get; init; } = new();
}

public sealed class CurrentUserPermissions
{
    /// <summary>Work order types the user may see and submit (see WorkOrder:RequiredGroupByType).</summary>
    public IReadOnlyList<WorkOrderType> WorkOrderTypes { get; init; } = [];
}
