using Umea.se.EstateService.Shared.Data.Entities;

namespace Umea.se.EstateService.Shared.Data;

public interface IWorkOrderRepository
{
    Task AddAsync(WorkOrderEntity workOrder, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WorkOrderEntity>> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<string?> GetLatestNotifierPhoneAsync(string email, CancellationToken cancellationToken = default);
    Task<WorkOrderEntity?> GetByUidAsync(Guid uid, string email, CancellationToken cancellationToken = default);
    Task<WorkOrderEntity?> GetByUidAsync(Guid uid, CancellationToken cancellationToken = default);
    Task<int> GetFailedCountAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WorkOrderEntity>> GetFailedWorkOrdersAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WorkOrderEntity>> GetDueForProcessingAsync(DateTimeOffset asOf, CancellationToken cancellationToken = default);
    Task<bool> TryClaimForProcessingAsync(int id, DateTimeOffset processingTimeout, CancellationToken cancellationToken = default);
    Task UpdateAsync(WorkOrderEntity workOrder, CancellationToken cancellationToken = default);
    /// <summary>Stamps StatusCheckedAt on due orders and returns the ids it stamped.</summary>
    Task<IReadOnlyList<int>> ClaimForStatusRefreshAsync(IReadOnlyList<int> ids, DateTimeOffset staleBefore, DateTimeOffset now, CancellationToken cancellationToken = default);
    /// <summary>Saves the status columns only, for a whole batch.</summary>
    Task SaveStatusesAsync(IReadOnlyList<WorkOrderEntity> workOrders, CancellationToken cancellationToken = default);
}
