using Umea.se.EstateService.ServiceAccess.Pythagoras.Enums;
using Umea.se.EstateService.Shared.Infrastructure.ConfigurationModels;

namespace Umea.se.EstateService.Logic.Handlers.WorkOrder;

/// <summary>
/// Single source of truth for which fields Pythagoras marks as MANDATORY_WHEN_CREATED per
/// submissible work order type. Derived from <c>GET /rest/v1/workordertype/{id}/workorderfieldsetting</c>,
/// filtered to <c>setting == MANDATORY_WHEN_CREATED</c>. Used by both runtime create logic
/// and startup config validation so they cannot drift apart.
/// </summary>
internal static class PythagorasWorkOrderCreateRequirements
{
    private static readonly IReadOnlyDictionary<PythagorasWorkOrderType, Requirements> Map =
        new Dictionary<PythagorasWorkOrderType, Requirements>
        {
            // ErrorReport: Pythagoras marks operating group MANDATORY_WHEN_CREATED, but supplying
            // it overrides Pythagoras's own assignment routing and forces every portal-created
            // fault report into the configured default inbox (Byggservice). Fault reports created
            // in Pythagoras Web without an operating group route correctly, so we deliberately do
            // not send the field and let Pythagoras assign the driftgrupp itself.
            [PythagorasWorkOrderType.ErrorReport] = new(Category: false, OperatingGroup: false),
            [PythagorasWorkOrderType.BuildingService] = new(Category: true, OperatingGroup: false),
            [PythagorasWorkOrderType.FacilityService] = new(Category: false, OperatingGroup: true),
            [PythagorasWorkOrderType.TownHallService] = new(Category: false, OperatingGroup: true),
            // SpaceRequirement: Pythagoras marks both category and operating group
            // MANDATORY_WHEN_CREATED. We supply a category (classifier pick, else the configured
            // default), but deliberately do not send an operating group: existing type-3 orders
            // are routed to varying geographic/role driftgrupper (e.g. Västra, Fastighetsförvaltare)
            // by Pythagoras's own assignment rules based on the bound object. Sending a single
            // default would mis-route every order, so we let Pythagoras assign it (like ErrorReport).
            [PythagorasWorkOrderType.SpaceRequirement] = new(Category: true, OperatingGroup: false),
        };

    public static bool RequiresCategory(PythagorasWorkOrderType type)
        => Map.TryGetValue(type, out Requirements r) && r.Category;

    public static bool RequiresOperatingGroup(PythagorasWorkOrderType type)
        => Map.TryGetValue(type, out Requirements r) && r.OperatingGroup;

    /// <summary>
    /// Returns config problems that would prevent the app from submitting work orders to
    /// Pythagoras — missing per-type defaults and out-of-range threshold values. An empty
    /// list means the config is complete.
    /// </summary>
    public static IReadOnlyList<string> Validate(WorkOrderConfiguration config)
    {
        List<string> problems = [];

        foreach ((PythagorasWorkOrderType type, Requirements r) in Map)
        {
            int typeId = (int)type;
            if (r.Category && !config.DefaultCategoryIdByType.ContainsKey(typeId))
            {
                problems.Add($"WorkOrder:DefaultCategoryIdByType:{typeId} ({type}) is missing");
            }
            if (r.OperatingGroup && !config.DefaultOperatingGroupIdByType.ContainsKey(typeId))
            {
                problems.Add($"WorkOrder:DefaultOperatingGroupIdByType:{typeId} ({type}) is missing");
            }
        }

        double threshold = config.CategoryClassifierMinimumConfidence;
        if (double.IsNaN(threshold) || threshold < 0.0 || threshold > 1.0)
        {
            problems.Add($"WorkOrder:CategoryClassifierMinimumConfidence must be in [0.0, 1.0] but was {threshold}");
        }

        return problems;
    }

    private readonly record struct Requirements(bool Category, bool OperatingGroup);
}
