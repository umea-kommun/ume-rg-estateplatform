using Microsoft.Extensions.Diagnostics.HealthChecks;
using Umea.se.EstateService.DataStore;
using Umea.se.Toolkit.HealthChecks;

namespace Umea.se.EstateService.API.HealthChecks;

public class DatabaseHealthCheck(EstateDbContext dbContext)
    : CachedRetryHealthCheck<DatabaseHealthCheck>
{
    private readonly EstateDbContext _dbContext = dbContext;

    protected override async Task<HealthCheckResult> ExecuteAsync(CancellationToken cancellationToken)
    {
        bool canConnect = await _dbContext.Database.CanConnectAsync(cancellationToken);

        if (!canConnect)
        {
            throw new InvalidOperationException("Unable to connect to the database.");
        }

        return HealthCheckResult.Healthy("Database connection is healthy.");
    }
}
