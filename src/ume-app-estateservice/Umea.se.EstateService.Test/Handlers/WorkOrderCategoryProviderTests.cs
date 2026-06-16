using Umea.se.EstateService.Logic.Data;
using Umea.se.EstateService.Logic.Handlers.WorkOrder;
using Umea.se.EstateService.Shared.Data.Entities;
using Umea.se.EstateService.Shared.Models;
using Umea.se.EstateService.Test.TestHelpers;

namespace Umea.se.EstateService.Test.Handlers;

public class WorkOrderCategoryProviderTests
{
    // Pythagoras type id used for SpaceRequirement categories in the seeded fixtures.
    private const int SpaceRequirementTypeId = 3;

    private static WorkOrderCategoryProvider CreateProvider(params WorkOrderCategoryNode[] categories)
    {
        InMemoryDataStore dataStore = new();
        DataStoreSeeder.Seed(dataStore, workOrderCategories: categories);
        return new WorkOrderCategoryProvider(dataStore);
    }

    [Fact]
    public void SingleChildWrapper_CollapsesToFullestName()
    {
        // Parent and leaf duplicate each other → one label, no " / " path.
        // Parent is fuller wording than the abbreviated leaf → parent wins.
        WorkOrderCategoryProvider provider = CreateProvider(
            new WorkOrderCategoryNode { Id = 1, Name = "Mindre anpassningar i befintliga lokaler", WorkOrderTypeIds = [SpaceRequirementTypeId] },
            new WorkOrderCategoryNode { Id = 2, ParentId = 1, Name = "Mindre anpassingar", WorkOrderTypeIds = [SpaceRequirementTypeId] });

        WorkOrderCategoryOption option = provider.GetLeafCategoriesForType(SpaceRequirementTypeId).ShouldHaveSingleItem();
        option.Id.ShouldBe(2); // still the selectable leaf id
        option.Name.ShouldBe("Mindre anpassningar i befintliga lokaler");
    }

    [Fact]
    public void RootCategoryWithoutParent_UsesOwnName()
    {
        WorkOrderCategoryProvider provider = CreateProvider(
            new WorkOrderCategoryNode { Id = 1, Name = "Generella utredningar", WorkOrderTypeIds = [SpaceRequirementTypeId] });

        WorkOrderCategoryOption option = provider.GetLeafCategoriesForType(SpaceRequirementTypeId).ShouldHaveSingleItem();
        option.Name.ShouldBe("Generella utredningar");
    }

    [Fact]
    public void ForkParent_KeepsSiblingLeafNamesDistinct()
    {
        // A parent with several leaves is a real fork: each leaf must keep its own name
        // instead of all collapsing to the shared parent label.
        WorkOrderCategoryProvider provider = CreateProvider(
            new WorkOrderCategoryNode { Id = 1, Name = "Ombyggnad", WorkOrderTypeIds = [SpaceRequirementTypeId] },
            new WorkOrderCategoryNode { Id = 2, ParentId = 1, Name = "Kök", WorkOrderTypeIds = [SpaceRequirementTypeId] },
            new WorkOrderCategoryNode { Id = 3, ParentId = 1, Name = "Badrum", WorkOrderTypeIds = [SpaceRequirementTypeId] });

        IReadOnlyList<WorkOrderCategoryOption> options = provider.GetLeafCategoriesForType(SpaceRequirementTypeId);

        options.Select(o => o.Name).ShouldBe(["Kök", "Badrum"], ignoreOrder: true);
    }
}
