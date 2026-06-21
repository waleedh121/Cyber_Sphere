using Serilog;
using Serilog.Events;
using Serilog.Filters;

namespace CyberSphere.Api.Extensions
{

    /// <summary>
    /// Configures Serilog as the sole logging provider.
    /// Call builder.Host.UseSerilogWithConfiguration() before builder.Build().
    /// Sinks: Console (dev) + File (always) + optional Seq (when URL is configured).
    /// </summary>
    public static class SerilogExtensions
    {
        public static IHostBuilder UseSerilogWithConfiguration(this IHostBuilder host)
        {
            return host.UseSerilog((context, services, configuration) =>
            {
                configuration
                    // ── Minimum levels ────────────────────────────────────────────
                    .MinimumLevel.Information()
                    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                    .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
                    .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command",
                        context.HostingEnvironment.IsDevelopment()
                            ? LogEventLevel.Information
                            : LogEventLevel.Warning)
                    .MinimumLevel.Override("System", LogEventLevel.Warning)

                    // ── Enrichers ─────────────────────────────────────────────────
                    .Enrich.FromLogContext()
                    .Enrich.WithMachineName()
                    .Enrich.WithEnvironmentName()
                    .Enrich.WithProperty("Application", "CyperSphere.API")

                    // ── Filter out noisy health-check polling ─────────────────────
                    .Filter.ByExcluding(Matching.WithProperty<string>(
                        "RequestPath", path =>
                            path.StartsWith("/health", StringComparison.OrdinalIgnoreCase)))

                    // ── Sinks ─────────────────────────────────────────────────────
                    // Console — human-readable in dev, compact JSON in prod
                    .WriteTo.Conditional(
                        _ => context.HostingEnvironment.IsDevelopment(),
                        wt => wt.Console(
                            outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}"))
                    .WriteTo.Conditional(
                        _ => !context.HostingEnvironment.IsDevelopment(),
                        wt => wt.Console(new Serilog.Formatting.Compact.CompactJsonFormatter()))

                    // Rolling file — one file per day, keep 14 days
                    .WriteTo.File(
                        path: "logs/cypersphere-.log",
                        rollingInterval: RollingInterval.Day,
                        retainedFileCountLimit: 14,
                        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")

                    // Seq — optional structured log server (configure URL in appsettings)
                    .WriteTo.Conditional(
                        _ => !string.IsNullOrWhiteSpace(
                            context.Configuration["Serilog:SeqUrl"]),
                        wt => wt.Seq(
                            context.Configuration["Serilog:SeqUrl"] ?? "http://localhost:5341"));
            });
        }
    }

}
