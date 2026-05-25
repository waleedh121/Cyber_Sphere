using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CyberSphere.Domain.Interfaces.Repositories;
using CyberSphere.Domain.Interfaces.Services;
using CyberSphere.Infrastructure.Identity;
using CyberSphere.Infrastructure.Persistence.Repositories;
using CyberSphere.Infrastructure.Persistence;
using CyberSphere.Infrastructure.Services;
using CyberSphere.Infrastructure.Services.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using CyberSphere.Application.Features.Sessions.Services;
using CyberSphere.Infrastructure.Services.VM;
using System.Runtime;

namespace CyberSphere.Infrastructure
{

    public static class InfrastructureServiceExtensions
    {
        public static IServiceCollection AddInfrastructureServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // ── Database ──────────────────────────────────────────────────────────
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(
                    configuration.GetConnectionString("DefaultConnection"),
                    sql => sql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)
                ));

            // ── Repositories ──────────────────────────────────────────────────────
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IToolRepository, ToolRepository>();
            services.AddScoped<ICategoryRepository, CategoryRepository>();
            services.AddScoped<IToolReviewRepository, ToolReviewRepository>();
            services.AddScoped<IRatingRepository, RatingRepository>();

            // ── Identity services ─────────────────────────────────────────────────
            services.Configure<JwtSettings>(
                configuration.GetSection(JwtSettings.SectionName));

            services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
            services.AddScoped<ITokenService, JwtTokenService>();

            // ── Email ─────────────────────────────────────────────────────────────
            services.AddScoped<IEmailService, MailKitEmailService>();

            // ── VM / SSH service ──────────────────────────────────────────────────
            services.Configure<VmSettings>(configuration.GetSection(VmSettings.SectionName));
            services.AddScoped<IVmService, SshVmService>();

            // ── VM repositories ────────────────────────────────────────────────────
            services.AddScoped<ISessionRepository, SessionRepository>();
            services.AddScoped<IServerRepository, ServerRepository>();

            // ── Session application service ────────────────────────────────────────
            services.AddScoped<VmAssignmentService>();

            // ── AI Gateway (Python microservice) ─────────────────────────────────
            var aiSettings = configuration
                .GetSection(AiSettings.SectionName)
                .Get<AiSettings>()
                ?? new AiSettings();

            services.Configure<AiSettings>(configuration.GetSection(AiSettings.SectionName));

            services.AddHttpClient(AiSettings.HttpClientName, client =>
            {
                client.BaseAddress = new Uri(aiSettings.BaseUrl);
                client.Timeout = TimeSpan.FromSeconds(aiSettings.TimeoutSeconds);
                client.DefaultRequestHeaders.Add("Accept", "application/json");
            });

            services.AddScoped<IAiGatewayService, PythonAiGatewayService>();

            // ── AI repositories ───────────────────────────────────────────────────
            services.AddScoped<IAiSessionRepository, AiSessionRepository>();
            services.AddScoped<IUserAiStatsRepository, UserAiStatsRepository>();

            // ── JWT Bearer authentication ─────────────────────────────────────────
            var jwtSettings = configuration
                .GetSection(JwtSettings.SectionName)
                .Get<JwtSettings>()
                ?? throw new InvalidOperationException("JwtSettings section is missing.");

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtSettings.Audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtSettings.SecretKey))
                };

                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = ctx =>
                    {
                        if (ctx.Exception is SecurityTokenExpiredException)
                            ctx.Response.Headers["Token-Expired"] = "true";
                        return Task.CompletedTask;
                    }
                };
            });

            return services;
        }
    }

}


