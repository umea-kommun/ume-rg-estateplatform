using System.Globalization;
using Cronos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Umea.se.EstateService.DataStore;
using Umea.se.EstateService.Logic.Sync;
using Umea.se.EstateService.Shared.Infrastructure;
using Umea.se.EstateService.Shared.Infrastructure.ConfigurationModels;
using Umea.se.Toolkit.HealthChecks;

namespace Umea.se.EstateService.API.HealthChecks;

/// <summary>
/// Reports Degraded when a scheduled data sync has passed without a newer snapshot being persisted.
/// Reads the persisted SyncMetadata rather than the in-memory refresh time, because the in-memory
/// data can be fresh while the database write failed and was rolled back.
/// </summary>
public class DataSyncHealthCheck(EstateDbContext dbContext, ApplicationConfig appConfig, ILogger<DataSyncHealthCheck> logger)
    : CachedRetryHealthCheck<DataSyncHealthCheck>
{
    private readonly EstateDbContext _dbContext = dbContext;
    private readonly DataSyncConfiguration _options = appConfig.DataSync;
    private readonly ILogger<DataSyncHealthCheck> _logger = logger;

    protected override async Task<HealthCheckResult> ExecuteAsync(CancellationToken cancellationToken)
    {
        if (!CronHelper.TryParse(_options.Schedule.Default, _options.TimeZone, out CronExpression? cron, out TimeZoneInfo? tz))
        {
            return HealthCheckResult.Healthy("Schemalagd synk är inte konfigurerad.");
        }

        DateTimeOffset? lastSuccess = await _dbContext.SyncMetadata
            .AsNoTracking()
            .Select(m => (DateTimeOffset?)m.LastRefreshUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (lastSuccess is null)
        {
            return HealthCheckResult.Degraded("Ingen synk har sparats i databasen.");
        }

        string lastSuccessLocal = TimeZoneInfo.ConvertTime(lastSuccess.Value, tz).ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
        Dictionary<string, object> data = new() { ["lastSuccessUtc"] = lastSuccess.Value.UtcDateTime };

        DateTimeOffset? missedRun = CronHelper.GetMissedRun(lastSuccess.Value, cron, tz, DateTimeOffset.UtcNow);
        if (missedRun is null)
        {
            return HealthCheckResult.Healthy($"Senaste synk sparad {lastSuccessLocal}.", data);
        }

        _logger.LogInformation("Data sync health check degraded: last persisted sync {LastSuccess}, missed run {MissedRun}", lastSuccess, missedRun);
        return HealthCheckResult.Degraded($"Synk har inte sparats sedan {lastSuccessLocal}.", data: data);
    }

    // A failed read here must not turn the service red; DatabaseHealthCheck already covers connectivity.
    protected override HealthCheckResult BuildUnhealthyResult(Exception exception, int attempts)
    {
        _logger.LogInformation("Data sync health check could not read sync metadata after {Attempts} attempts: {Reason}", attempts, exception.Message);
        return HealthCheckResult.Degraded($"Kunde inte läsa synkstatus: {exception.Message} (efter {attempts} försök).");
    }
}
