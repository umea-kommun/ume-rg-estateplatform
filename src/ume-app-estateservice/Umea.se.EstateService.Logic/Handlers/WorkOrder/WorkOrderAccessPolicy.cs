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

    public IReadOnlyList<WorkOrderType> FilterAllowed(IEnumerable<WorkOrderType> types, IReadOnlyCollection<string>? userGroups)
        => [.. types.Where(type => IsTypeAllowed(type, userGroups))];

    public void StampAllowedWorkOrderTypes(
        IEnumerable<BuildingInfoModel> buildings, IReadOnlyCollection<string>? userGroups)
    {
        foreach (BuildingInfoModel building in buildings)
        {
            StampAllowedWorkOrderTypes(building, userGroups);
        }
    }

    public void StampAllowedWorkOrderTypes(
        BuildingInfoModel building, IReadOnlyCollection<string>? userGroups)
        => building.WorkOrderTypes = FilterAllowed(building.WorkOrderTypes, userGroups);
}
