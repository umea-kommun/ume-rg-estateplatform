using System.Net;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Umea.se.EstateService.ServiceAccess;
using Umea.se.Toolkit.HealthChecks;

namespace Umea.se.EstateService.API.HealthChecks;

public class PythagorasHealthCheck : DownstreamServiceHealthCheck<PythagorasHealthCheck>
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<PythagorasHealthCheck> _logger;

    public PythagorasHealthCheck(IHttpClientFactory httpClientFactory, ILogger<PythagorasHealthCheck> logger)
        : base(httpClientFactory, logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    protected override string HttpClientName => HttpClientNames.PythagorasHealthCheck;
    protected override string PingFallbackUrl => "/rest/v1/errandrole/currentuser";

    protected override async Task<HealthCheckResult> ExecuteAsync(CancellationToken cancellationToken)
    {
        HttpClient client = _httpClientFactory.CreateClient(HttpClientName);
        string url = client.BaseAddress?.ToString().TrimEnd('/') + PingFallbackUrl;

        try
        {
            using HttpResponseMessage response = await client.GetAsync(url, cancellationToken);

            return response.StatusCode == HttpStatusCode.OK
                ? HealthCheckResult.Healthy($"{HttpClientName} responded successfully.")
                : HealthCheckResult.Degraded($"{HttpClientName} returned HTTP {(int)response.StatusCode}");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException or TimeoutException)
        {
            _logger.LogDebug("Pythagoras health probe failed: {Reason}", ex.Message);
            return HealthCheckResult.Degraded($"{HttpClientName} is unavailable: {ex.Message}");
        }
    }
}
