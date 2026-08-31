using Umea.se.EstateService.Shared.Infrastructure.ConfigurationModels;
using Umea.se.EstateService.Shared.Models;

namespace Umea.se.EstateService.Logic.Handlers.WorkOrder;

public sealed class WorkOrderAccessPolicy(WorkOrderConfiguration config)
{
    public bool IsTypeAllowed(WorkOrderType type, IReadOnlyCollection<string>? userGroups)
    {
        if (!config.RequiredGroupByType.TryGetValue(type, out string? requiredGroup)
            || string.IsNullOrWhiteSpace(requiredGroup))
        {
            return true; // type is not gated
        }

        // Fail-closed: a user with no matching group (or no groups at all) is denied.
        return userGroups is not null && userGroups.Contains(requiredGroup, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Resolves the two independent questions the client would otherwise have to answer itself:
    /// may this user use the type at all (if not it is left out, so the client never shows it), and
    /// does this building offer it (if not it is Disabled, so the client can show it greyed).
    /// </summary>
    public IReadOnlyDictionary<WorkOrderType, WorkOrderAccessState> BuildAccessMap(
        IEnumerable<WorkOrderType> buildingTypes, IReadOnlyCollection<string>? userGroups)
    {
        HashSet<WorkOrderType> offered = [.. buildingTypes];

        return Enum.GetValues<WorkOrderType>()
            .Where(type => IsTypeAllowed(type, userGroups))
            .ToDictionary(
                type => type,
                type => offered.Contains(type) ? WorkOrderAccessState.Enabled : WorkOrderAccessState.Disabled);
    }

    public void StampWorkOrderTypeAccess(
        IEnumerable<BuildingInfoModel> buildings, IReadOnlyCollection<string>? userGroups)
    {
        foreach (BuildingInfoModel building in buildings)
        {
            StampWorkOrderTypeAccess(building, userGroups);
        }
    }

    public void StampWorkOrderTypeAccess(
        BuildingInfoModel building, IReadOnlyCollection<string>? userGroups)
        => building.WorkOrderTypeAccess = BuildAccessMap(building.WorkOrderTypes, userGroups);
}
