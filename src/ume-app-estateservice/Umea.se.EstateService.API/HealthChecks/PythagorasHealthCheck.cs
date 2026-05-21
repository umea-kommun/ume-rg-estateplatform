using System.Net;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Umea.se.EstateService.ServiceAccess;
using Umea.se.Toolkit.HealthChecks;

namespace Umea.se.EstateService.API.HealthChecks;

public class PythagorasHealthCheck : DownstreamServiceHealthCheck<PythagorasHealthCheck>
{
    private readonly IHttpClientFactory _httpClientFactory;

    public PythagorasHealthCheck(IHttpClientFactory httpClientFactory, ILogger<PythagorasHealthCheck> logger)
        : base(httpClientFactory, logger)
    {
        _httpClientFactory = httpClientFactory;
    }

    protected override string HttpClientName => HttpClientNames.Pythagoras;
    protected override string PingFallbackUrl => "/rest/v1/errandrole/currentuser";

    protected override async Task<HealthCheckResult> ExecuteAsync(CancellationToken cancellationToken)
    {
        HttpClient client = _httpClientFactory.CreateClient(HttpClientName);
        string url = client.BaseAddress?.ToString().TrimEnd('/') + PingFallbackUrl;

        HttpResponseMessage response = await client.GetAsync(url, cancellationToken);

        return response.StatusCode == HttpStatusCode.OK
            ? HealthCheckResult.Healthy($"{HttpClientName} responded successfully.")
            : throw new HttpRequestException(
                $"{HttpClientName} returned HTTP {(int)response.StatusCode}",
                null,
                response.StatusCode);
    }
}
