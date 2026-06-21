using CyberSphere.Api.Extensions;
using CyberSphere.Api.Middleware;
using CyberSphere.Api.Services;
using CyberSphere.Application;
using CyberSphere.Domain.Interfaces.Repositories;
using CyberSphere.Infrastructure;
using CyberSphere.Infrastructure.Services;
using Serilog;



// ── Bootstrap Serilog early so startup errors are captured ────────────────────
// This minimal logger catches any exception before the full host is built.
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting CyperSphere API");

    var builder = WebApplication.CreateBuilder(args);

    // ── Serilog ───────────────────────────────────────────────────────────────
    builder.Host.UseSerilogWithConfiguration();

    // ── Application services ──────────────────────────────────────────────────
    builder.Services.AddApplicationServices();
    builder.Services.AddInfrastructureServices(builder.Configuration);

    // Current user — reads JWT claims from IHttpContextAccessor
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

    // ── API layer ─────────────────────────────────────────────────────────────
    builder.Services.AddControllers();
    builder.Services.AddSwaggerWithJwt();

    // ── Rate limiting ─────────────────────────────────────────────────────────
    builder.Services.AddCyperSphereRateLimiting();

    // ── Health checks ─────────────────────────────────────────────────────────
    builder.Services.AddCyperSphereHealthChecks(builder.Configuration);

    //// ── CORS ──────────────────────────────────────────────────────────────────
    //builder.Services.AddCors(options =>
    //{
    //    options.AddPolicy("AllowFrontend", policy =>
    //        policy.WithOrigins(
    //                builder.Configuration.GetSection("AllowedOrigins").Get<string[]>()
    //                ?? ["http://localhost:3000"])
    //              .AllowAnyHeader()
    //              .AllowAnyMethod());
    //});

    // ── CORS ──────────────────────────────────────────────────────────────────
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowFrontend", policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
    });

    var app = builder.Build();

    // ── Request pipeline ──────────────────────────────────────────────────────

    // Global error handler — outermost middleware, wraps everything
    app.UseMiddleware<ErrorHandlingMiddleware>();

    // Serilog request logging — after error handler so we capture status codes
    app.UseSerilogRequestLogging(opts =>
    {
        opts.MessageTemplate =
            "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000}ms";

        // Enrich with user info when available
        opts.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
            diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
            diagnosticContext.Set("UserId",
                httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);
        };
    });


    app.UseCors("AllowFrontend");

    app.UseHttpsRedirection();

    // Swagger — available in all environments (restrict at infra level in prod)
    //app.UseCyperSphereSwagger();
    app.UseCyperSphereSwagger();

 
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "CyberSphere API v1");
        c.RoutePrefix = string.Empty; 
    });


    //app.UseHttpsRedirection();
    //app.UseCors("AllowFrontend");

    // Rate limiting — before auth so unauthenticated brute-force is also throttled
    app.UseRateLimiter();

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    // Health check endpoints — exempt from rate limiting, no auth required
    app.MapCyperSphereHealthChecks();

    // DB migration + seed lookup data
    await DatabaseSeeder.SeedAsync(app.Services);

    await app.RunAsync();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "CyperSphere API terminated unexpectedly");
}
finally
{
    Log.Information("CyperSphere API shut down");
    await Log.CloseAndFlushAsync();
}