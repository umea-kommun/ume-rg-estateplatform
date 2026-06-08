using Umea.se.EstateService.Shared.Data;
using Umea.se.EstateService.Shared.Data.Entities;
using Umea.se.EstateService.Shared.Models;

namespace Umea.se.EstateService.Logic.Handlers.WorkOrder;

public sealed class WorkOrderCategoryProvider(IDataStore dataStore)
{
    public IReadOnlyList<WorkOrderCategoryOption> GetLeafCategoriesForType(int workOrderTypeId)
    {
        List<WorkOrderCategoryNode> categories = [.. dataStore.WorkOrderCategories.Where(c =>
            c.WorkOrderTypeIds.Count == 0 || c.WorkOrderTypeIds.Contains(workOrderTypeId))];

        if (categories.Count == 0)
        {
            return [];
        }

        Dictionary<int, WorkOrderCategoryNode> byId = categories.ToDictionary(c => c.Id);
        HashSet<int> parentIds = [.. categories.Where(c => c.ParentId.HasValue).Select(c => c.ParentId!.Value)];
        Dictionary<int, int> childCount = categories
            .Where(c => c.ParentId.HasValue)
            .GroupBy(c => c.ParentId!.Value)
            .ToDictionary(g => g.Key, g => g.Count());

        return [.. categories
            .Where(c => !parentIds.Contains(c.Id))
            .Select(leaf => new WorkOrderCategoryOption { Id = leaf.Id, Name = GetDisplayName(leaf, byId, childCount) })];
    }

    private static string GetDisplayName(
        WorkOrderCategoryNode leaf,
        Dictionary<int, WorkOrderCategoryNode> byId,
        Dictionary<int, int> childCount)
    {
        // Pythagoras stores each selectable category as a chain of single-child nodes whose names
        // duplicate or abbreviate one another — e.g. a "Tillgänglighetsanpassningar" parent wrapping
        // a same-named leaf. Collapse such a chain to a single label, preferring the fullest name.
        // Stop climbing at a genuine fork (a parent with more than one child) so that sibling leaves
        // keep their own distinct names instead of collapsing to a shared parent label.
        string best = leaf.Name;
        WorkOrderCategoryNode current = leaf;
        while (current.ParentId.HasValue
            && byId.TryGetValue(current.ParentId.Value, out WorkOrderCategoryNode? parent)
            && childCount.GetValueOrDefault(parent.Id) == 1)
        {
            if (parent.Name.Length > best.Length)
            {
                best = parent.Name;
            }

            current = parent;
        }

        return best;
    }
}
