using Microsoft.Extensions.Diagnostics.HealthChecks;
using Umea.se.EstateService.Shared.Data;
using Umea.se.Toolkit.HealthChecks;

namespace Umea.se.EstateService.API.HealthChecks;

/// <summary>
/// Surfaces permanently failed work orders (SyncStatus.Failed with no scheduled retry) as an
/// operational signal on the status dashboard. Reports Degraded — never Unhealthy — when there is
/// a backlog: the service itself is healthy, the orders just need manual remediation. The count is
/// exposed both in the description and in the data so the dashboard can display it.
/// </summary>
public class FailedWorkOrdersHealthCheck(IWorkOrderRepository workOrderRepository)
    : CachedRetryHealthCheck<FailedWorkOrdersHealthCheck>
{
    private readonly IWorkOrderRepository _workOrderRepository = workOrderRepository;

    protected override async Task<HealthCheckResult> ExecuteAsync(CancellationToken cancellationToken)
    {
        int count = await _workOrderRepository.GetFailedCountAsync(cancellationToken);

        Dictionary<string, object> data = new() { ["count"] = count };

        return count == 0
            ? HealthCheckResult.Healthy("Inga misslyckade arbetsordrar.", data)
            : HealthCheckResult.Degraded($"{count} misslyckade arbetsordrar väntar på åtgärd.", data: data);
    }

    // Failing to read the count is itself only a Degraded signal — it must never turn the
    // EstateService health red, since the service itself is unaffected by a counting hiccup.
    protected override HealthCheckResult BuildUnhealthyResult(Exception exception, int attempts) =>
        HealthCheckResult.Degraded($"Kunde inte läsa antal misslyckade arbetsordrar: {exception.Message} (efter {attempts} försök).");
}
