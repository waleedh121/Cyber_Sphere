using CyberSphere.Application.Exceptions;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace CyberSphere.Api.Middleware
{

    /// <summary>
    /// Catches all unhandled exceptions and maps them to RFC 7807 ProblemDetails responses.
    /// Registered as the outermost middleware so it wraps the entire pipeline.
    /// </summary>
    public sealed class ErrorHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ErrorHandlingMiddleware> _logger;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        //private async Task HandleExceptionAsync(HttpContext context, Exception exception)

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var (statusCode, title, errors) = exception switch
            {
                // FluentValidation.ValidationException — thrown by ValidationBehavior pipeline
                FluentValidation.ValidationException fve =>
                    (StatusCodes.Status422UnprocessableEntity, "Validation failed",
                     (IDictionary<string, string[]>?)fve.Errors
                        .GroupBy(e => e.PropertyName)
                        .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())),

                Application.Exceptions.ValidationException ve =>
                    (StatusCodes.Status422UnprocessableEntity, "Validation failed", ve.Errors),

                UnauthorizedException =>
                    (StatusCodes.Status401Unauthorized, exception.Message, (IDictionary<string, string[]>?)null),

                ForbiddenException =>
                    (StatusCodes.Status403Forbidden, exception.Message, null),

                NotFoundException =>
                    (StatusCodes.Status404NotFound, exception.Message, null),

                ConflictException =>
                    (StatusCodes.Status409Conflict, exception.Message, null),

                BadRequestException =>
                    (StatusCodes.Status400BadRequest, exception.Message, null),

                //_ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.", null)
                _ => (
                    StatusCodes.Status500InternalServerError,
                    exception.InnerException?.Message ?? exception.Message,
                    new Dictionary<string, string[]>
                    {
                    { "exception", new[]
                        {
                            exception.ToString()
                        }
                    }
                                }
                            )
            };
            // Only log 5xx as errors; 4xx are expected client mistakes
            if (statusCode >= 500)
                _logger.LogError(exception, "Unhandled exception for {Method} {Path}",
                    context.Request.Method, context.Request.Path);
            else
                _logger.LogWarning("Client error {StatusCode}: {Message}", statusCode, exception.Message);

            var problem = new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Instance = context.Request.Path
            };

            if (errors is not null)
                problem.Extensions["errors"] = errors;

            context.Response.ContentType = "application/problem+json";
            context.Response.StatusCode = statusCode;

            await context.Response.WriteAsync(JsonSerializer.Serialize(problem, JsonOptions));
        }
    }
}
