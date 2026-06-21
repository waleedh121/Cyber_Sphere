using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using CyberSphere.Api.HealthChecks;
using System.Text.Json;

namespace CyberSphere.Api.Extensions
{
    public static class HealthCheckExtensions
    {
        public static IServiceCollection AddCyperSphereHealthChecks(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services
                .AddHealthChecks()

                // SQL Server — uses the same connection string as EF Core
                .AddSqlServer(
                    connectionString: configuration.GetConnectionString("DefaultConnection")
                        ?? throw new InvalidOperationException("DefaultConnection is required."),
                    name: "sql-server",
                    tags: ["db", "infrastructure"],
                    failureStatus: HealthStatus.Unhealthy)

                // Python AI microservice
                .AddCheck<PythonAiServiceHealthCheck>(
                    name: "python-ai-service",
                    failureStatus: HealthStatus.Degraded,
                    tags: ["external", "ai"])

                // Self — always healthy if the process is running
                .AddCheck("self", () => HealthCheckResult.Healthy("API is running."),
                    tags: ["live"]);

            return services;
        }

        public static IEndpointRouteBuilder MapCyperSphereHealthChecks(
            this IEndpointRouteBuilder app)
        {
            // Liveness — just checks the process is alive. No external dependencies.
            app.MapHealthChecks("/health/live", new HealthCheckOptions
            {
                Predicate = check => check.Tags.Contains("live"),
                ResponseWriter = WriteJsonResponse
            });

            // Readiness — all checks must pass before routing traffic.
            app.MapHealthChecks("/health/ready", new HealthCheckOptions
            {
                Predicate = _ => true,
                ResponseWriter = WriteJsonResponse
            });

            // Full detail — explicit endpoint for the admin dashboard.
            app.MapHealthChecks("/health", new HealthCheckOptions
            {
                Predicate = _ => true,
                ResponseWriter = WriteJsonResponse
            });

            return app;
        }

        // ── Response writer — structured JSON instead of plain text ───────────────

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        private static Task WriteJsonResponse(HttpContext context, HealthReport report)
        {
            context.Response.ContentType = "application/json";

            var response = new
            {
                status = report.Status.ToString(),
                elapsed = report.TotalDuration.TotalMilliseconds,
                checks = report.Entries.Select(e => new
                {
                    name = e.Key,
                    status = e.Value.Status.ToString(),
                    description = e.Value.Description,
                    duration = e.Value.Duration.TotalMilliseconds,
                    tags = e.Value.Tags,
                    exception = e.Value.Exception?.Message
                })
            };

            return context.Response.WriteAsync(
                JsonSerializer.Serialize(response, JsonOptions));
        }
    }

}
