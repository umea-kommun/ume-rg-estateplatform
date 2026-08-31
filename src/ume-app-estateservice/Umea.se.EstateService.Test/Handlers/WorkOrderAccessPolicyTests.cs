using Umea.se.EstateService.Logic.Handlers.WorkOrder;
using Umea.se.EstateService.Shared.Infrastructure.ConfigurationModels;
using Umea.se.EstateService.Shared.Models;

namespace Umea.se.EstateService.Test.Handlers;

public class WorkOrderAccessPolicyTests
{
    private const string SpaceRequirementGroup = "c33cbefe-1dc8-40c5-bb41-6ca744ff96c7";

    private static WorkOrderAccessPolicy CreateGatedPolicy()
        => new(new WorkOrderConfiguration
        {
            RequiredGroupByType = { [WorkOrderType.SpaceRequirement] = SpaceRequirementGroup }
        });

    [Fact]
    public void BuildAccessMap_NoGates_CoversEveryType()
    {
        WorkOrderAccessPolicy policy = new(new WorkOrderConfiguration());

        IReadOnlyDictionary<WorkOrderType, WorkOrderAccessState> map =
            policy.BuildAccessMap([WorkOrderType.ErrorReport], null);

        map.Keys.ShouldBe(Enum.GetValues<WorkOrderType>(), ignoreOrder: true);
        map[WorkOrderType.ErrorReport].ShouldBe(WorkOrderAccessState.Enabled);
        map[WorkOrderType.BuildingService].ShouldBe(WorkOrderAccessState.Disabled);
    }

    [Fact]
    public void BuildAccessMap_UserLacksGroup_OmitsGatedTypeEvenWhenBuildingOffersIt()
    {
        IReadOnlyDictionary<WorkOrderType, WorkOrderAccessState> map = CreateGatedPolicy()
            .BuildAccessMap([WorkOrderType.ErrorReport, WorkOrderType.SpaceRequirement], ["some-other-group"]);

        // Gated for this user: absent, so the client hides it rather than greying it.
        map.ShouldNotContainKey(WorkOrderType.SpaceRequirement);

        // Not gated, but this building does not offer it: present and disabled.
        map[WorkOrderType.BuildingService].ShouldBe(WorkOrderAccessState.Disabled);
        map[WorkOrderType.ErrorReport].ShouldBe(WorkOrderAccessState.Enabled);
    }

    [Fact]
    public void BuildAccessMap_UserInGroup_KeepsGatedType()
    {
        IReadOnlyDictionary<WorkOrderType, WorkOrderAccessState> map = CreateGatedPolicy()
            .BuildAccessMap([WorkOrderType.SpaceRequirement], [SpaceRequirementGroup]);

        map[WorkOrderType.SpaceRequirement].ShouldBe(WorkOrderAccessState.Enabled);
    }

    [Fact]
    public void BuildAccessMap_UserInGroupButBuildingLacksType_IsDisabledNotAbsent()
    {
        IReadOnlyDictionary<WorkOrderType, WorkOrderAccessState> map = CreateGatedPolicy()
            .BuildAccessMap([WorkOrderType.ErrorReport], [SpaceRequirementGroup]);

        map[WorkOrderType.SpaceRequirement].ShouldBe(WorkOrderAccessState.Disabled);
    }

    [Fact]
    public void StampWorkOrderTypeAccess_ListOverload_StampsEveryBuilding()
    {
        List<BuildingInfoModel> buildings =
        [
            new() { WorkOrderTypes = [WorkOrderType.ErrorReport, WorkOrderType.SpaceRequirement] },
            new() { WorkOrderTypes = [WorkOrderType.SpaceRequirement] }
        ];

        CreateGatedPolicy().StampWorkOrderTypeAccess(buildings, ["some-other-group"]);

        buildings[0].WorkOrderTypeAccess[WorkOrderType.ErrorReport].ShouldBe(WorkOrderAccessState.Enabled);
        buildings[1].WorkOrderTypeAccess[WorkOrderType.ErrorReport].ShouldBe(WorkOrderAccessState.Disabled);
        buildings.ShouldAllBe(b => !b.WorkOrderTypeAccess.ContainsKey(WorkOrderType.SpaceRequirement));
    }

    [Fact]
    public void BuildAccessMap_NoGroupsClaim_OmitsGatedType()
    {
        // Fail-closed: a token without a groups claim is not a member of anything.
        IReadOnlyDictionary<WorkOrderType, WorkOrderAccessState> map = CreateGatedPolicy()
            .BuildAccessMap([WorkOrderType.SpaceRequirement], null);

        map.ShouldNotContainKey(WorkOrderType.SpaceRequirement);
    }
}
