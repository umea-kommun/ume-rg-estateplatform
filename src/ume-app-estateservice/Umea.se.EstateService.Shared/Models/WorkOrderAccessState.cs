using System.Text.Json.Serialization;

namespace Umea.se.EstateService.Shared.Models;

/// <summary>
/// How the client should offer a work order type on a building. Types the user may not use at all
/// are absent from the map rather than carrying a state of their own.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum WorkOrderAccessState
{
    /// <summary>The building offers the type and the user may submit it.</summary>
    Enabled,

    /// <summary>The user may use the type, but this building does not offer it - show it disabled.</summary>
    Disabled,
}
