using Umea.se.EstateService.Shared.Data.Entities;

namespace Umea.se.EstateService.Logic.Handlers.WorkOrder;

public interface IWorkOrderCategoryClassifier
{
    Task<IReadOnlyList<WorkOrderCategorySuggestion>> ClassifyAsync(
        string description, int workOrderTypeId, CancellationToken ct = default);
}
