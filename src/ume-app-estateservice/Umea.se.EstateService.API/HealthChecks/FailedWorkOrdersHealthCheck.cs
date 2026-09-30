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
public class FailedWorkOrdersHealthCheck(IWorkOrderRepository workOrderRepository, ILogger<FailedWorkOrdersHealthCheck> logger)
    : CachedRetryHealthCheck<FailedWorkOrdersHealthCheck>
{
    private readonly IWorkOrderRepository _workOrderRepository = workOrderRepository;
    private readonly ILogger<FailedWorkOrdersHealthCheck> _logger = logger;

    protected override async Task<HealthCheckResult> ExecuteAsync(CancellationToken cancellationToken)
    {
        int count = await _workOrderRepository.GetFailedCountAsync(cancellationToken);

        Dictionary<string, object> data = new() { ["count"] = count };

        string description = count == 1
            ? "1 misslyckad arbetsorder"
            : $"{count} misslyckade arbetsordrar";

        if (count == 0)
        {
            return HealthCheckResult.Healthy("Inga misslyckade arbetsordrar.", data);
        }

        _logger.LogInformation("Failed work orders health check degraded: {FailedCount} failed work orders", count);
        return HealthCheckResult.Degraded(description, data: data);
    }

    // Failing to read the count is itself only a Degraded signal — it must never turn the
    // EstateService health red, since the service itself is unaffected by a counting hiccup.
    protected override HealthCheckResult BuildUnhealthyResult(Exception exception, int attempts)
    {
        _logger.LogInformation("Failed work orders health check could not read count after {Attempts} attempts: {Reason}", attempts, exception.Message);
        return HealthCheckResult.Degraded($"Kunde inte läsa antal misslyckade arbetsordrar: {exception.Message} (efter {attempts} försök).");
    }
}
