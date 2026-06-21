using Microsoft.OpenApi.Models;

namespace CyberSphere.Api.Extensions
{

    //public static class SwaggerExtensions
    //{
    //    public static IServiceCollection AddSwaggerWithJwt(this IServiceCollection services)
    //    {
    //        services.AddEndpointsApiExplorer();
    //        services.AddSwaggerGen(options =>
    //        {
    //            options.SwaggerDoc("v1", new OpenApiInfo
    //            {
    //                Title = "CyperSphere API",
    //                Version = "v1",
    //                Description = "Community cybersecurity platform — backend API",
    //                Contact = new OpenApiContact { Name = "CyperSphere Dev" }
    //            });

    //            // JWT bearer definition
    //            var scheme = new OpenApiSecurityScheme
    //            {
    //                Name = "Authorization",
    //                Type = SecuritySchemeType.Http,
    //                Scheme = "bearer",
    //                BearerFormat = "JWT",
    //                In = ParameterLocation.Header,
    //                Description = "Enter your JWT token. Example: eyJhbGci..."
    //            };

    //            options.AddSecurityDefinition("Bearer", scheme);
    //            options.AddSecurityRequirement(new OpenApiSecurityRequirement
    //        {
    //            {
    //                new OpenApiSecurityScheme
    //                {
    //                    Reference = new OpenApiReference
    //                    {
    //                        Type = ReferenceType.SecurityScheme,
    //                        Id   = "Bearer"
    //                    }
    //                },
    //                Array.Empty<string>()
    //            }
    //        });

    //            // Include XML doc comments if generated
    //            var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    //            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    //            if (File.Exists(xmlPath))
    //                options.IncludeXmlComments(xmlPath);
    //        });

    //        return services;
    //    }
    //}

    public static class SwaggerExtensions
    {
        public static IServiceCollection AddSwaggerWithJwt(this IServiceCollection services)
        {
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "CyperSphere API",
                    Version = "v1",
                    Description = """
                    Community Cybersecurity Platform — Backend API
 
                    **Authentication**: Use `POST /api/auth/login` to obtain a JWT access token,
                    then click **Authorize** and enter `Bearer {token}`.
 
                    **Rate limits**: Auth endpoints — 10 req/min. AI endpoints — 20 req/min. All others — 60 req/min.
 
                    **Role-based access**: Endpoints under `/api/admin` require the `Admin` role.
                    """,
                    Contact = new OpenApiContact
                    {
                        Name = "CyperSphere Team",
                        Email = "dev@cypersphere.dev"
                    }
                });

                // ── JWT bearer definition ─────────────────────────────────────────
                var jwtScheme = new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Enter your JWT access token (without the 'Bearer' prefix)."
                };

                options.AddSecurityDefinition("Bearer", jwtScheme);
                options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id   = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });

                // ── Tag ordering — group endpoints logically in the UI ────────────
                options.TagActionsBy(api =>
                {
                    if (api.GroupName is not null)
                        return [api.GroupName];

                    if (api.ActionDescriptor is Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor desc)
                        return [desc.ControllerName];

                    return ["Other"];
                });

                options.DocInclusionPredicate((_, _) => true);

                // ── XML doc comments ──────────────────────────────────────────────
                var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                if (File.Exists(xmlPath))
                    options.IncludeXmlComments(xmlPath);
            });

            return services;
        }

        /// <summary>
        /// Maps Swagger UI at /swagger in all environments.
        /// In production, restrict access at the infrastructure layer (reverse proxy / IP allowlist).
        /// </summary>
        public static IApplicationBuilder UseCyperSphereSwagger(this IApplicationBuilder app)
        {
            app.UseSwagger(c =>
            {
                c.RouteTemplate = "swagger/{documentName}/swagger.json";
            });

            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "CyperSphere API v1");
                c.RoutePrefix = "swagger";
                c.DocumentTitle = "CyperSphere API";
                c.DisplayRequestDuration();
                c.EnableFilter();
                c.EnablePersistAuthorization();
                c.DefaultModelsExpandDepth(-1); // collapse schemas by default
            });

            return app;
        }
    }

}


