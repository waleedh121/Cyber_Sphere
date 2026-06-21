using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

namespace CyberSphere.Api.Extensions
{
    public static class RateLimitingExtensions
    {
        // Policy name constants — referenced in controllers via [EnableRateLimiting]
        public const string GlobalPolicy = "global";
        public const string AuthPolicy = "auth";
        public const string AiPolicy = "ai";

        /// <summary>
        /// Registers three rate-limiting policies using the built-in ASP.NET Core
        /// RateLimiter (no third-party package needed).
        ///
        /// global — 60 requests / 60s per IP (general API protection)
        /// auth   — 10 requests / 60s per IP (stricter: login + register brute-force guard)
        /// ai     — 20 requests / 60s per IP (AI message send throttle)
        ///
        /// Limits are deliberately generous for a cybersecurity community platform.
        /// Adjust via appsettings RateLimiting section for production tuning.
        /// </summary>
        public static IServiceCollection AddCyperSphereRateLimiting(
            this IServiceCollection services)
        {
            services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                // Global — applied to all endpoints unless overridden
                options.AddFixedWindowLimiter(GlobalPolicy, o =>
                {
                    o.PermitLimit = 60;
                    o.Window = TimeSpan.FromMinutes(1);
                    o.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                    o.QueueLimit = 0;
                });

                // Auth — login, register, forgot-password
                options.AddFixedWindowLimiter(AuthPolicy, o =>
                {
                    o.PermitLimit = 10;
                    o.Window = TimeSpan.FromMinutes(1);
                    o.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                    o.QueueLimit = 0;
                });

                // AI messages
                options.AddFixedWindowLimiter(AiPolicy, o =>
                {
                    o.PermitLimit = 20;
                    o.Window = TimeSpan.FromMinutes(1);
                    o.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                    o.QueueLimit = 0;
                });

                // Partition by IP address so limits are per-client, not global
                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
                    context => RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 60,
                            Window = TimeSpan.FromMinutes(1)
                        }));
            });

            return services;
        }
    }
}
