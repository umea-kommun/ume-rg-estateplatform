using OpenTelemetry.Trace;
using Umea.se.EstateService.API.Infrastructure;
using Umea.se.EstateService.Logic.Handlers;
using Umea.se.Toolkit.EntryPoints;

namespace Umea.se.EstateService.API;

public static class DependencyInjectionApi
{
    public static IServiceCollection AddApiDependencies(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddSingleton<SearchHandler>();
        services.AddUserFromToken();

        services.AddOpenTelemetry()
            .WithTracing(tracing => tracing.AddProcessor<HttpStatusSuccessProcessor>());

        // HealthChecks
        services.AddHealthChecks()
            .AddCheck<HealthChecks.DatabaseHealthCheck>("EstateServiceDatabase",
            failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy)
            .AddCheck<HealthChecks.PythagorasHealthCheck>("PythagorasApi",
            failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded,
            timeout: TimeSpan.FromSeconds(20))
            .AddCheck<HealthChecks.FailedWorkOrdersHealthCheck>("FailedWorkOrders",
            failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded)
            ;

        return services;
    }
}
