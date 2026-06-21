using CyberSphere.Infrastructure.Services.AI;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CyberSphere.Api.HealthChecks
{

    /// <summary>
    /// Probes the external Python AI microservice's /health endpoint.
    /// Uses the same named HttpClient registered for AI calls.
    /// A 200 response marks the check as Healthy; anything else is Degraded.
    /// Network failures are Unhealthy.
    /// </summary>
    public sealed class PythonAiServiceHealthCheck : IHealthCheck
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<PythonAiServiceHealthCheck> _logger;

        public PythonAiServiceHealthCheck(
            IHttpClientFactory httpClientFactory,
            ILogger<PythonAiServiceHealthCheck> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                using var client = _httpClientFactory.CreateClient(AiSettings.HttpClientName);
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromSeconds(5));

                var response = await client.GetAsync("/health", cts.Token);

                return response.IsSuccessStatusCode
                    ? HealthCheckResult.Healthy("Python AI service is reachable.")
                    : HealthCheckResult.Degraded(
                        $"Python AI service returned {(int)response.StatusCode} {response.ReasonPhrase}.");
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
            {
                _logger.LogWarning(ex, "Python AI service health check failed");
                return HealthCheckResult.Unhealthy(
                    "Python AI service is unreachable.", ex);
            }
        }
    }

}
