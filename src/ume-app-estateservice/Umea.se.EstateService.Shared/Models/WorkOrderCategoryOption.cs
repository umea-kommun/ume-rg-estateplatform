namespace Umea.se.EstateService.Shared.Models;

/// <summary>
/// A selectable leaf work order category, returned to clients so the user can pick a
/// category for types where the choice is explicit (e.g. SpaceRequirement). <see cref="Name"/>
/// is the full category path (e.g. "Parent / Leaf") so nested leaves stay unambiguous.
/// </summary>
public sealed class WorkOrderCategoryOption
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
}
