using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;


namespace CyberSphere.Application.Common.Behaviors
{
    /// <summary>
    /// Logs the start, completion, and duration of every MediatR request.
    /// Slow requests (> 500ms) are logged at Warning level.
    /// </summary>
    public sealed class LoggingBehavior<TRequest, TResponse>
        : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

        public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
            => _logger = logger;

        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken ct)
        {
            var requestName = typeof(TRequest).Name;
            _logger.LogInformation("Handling {RequestName}", requestName);

            var sw = Stopwatch.StartNew();

            try
            {
                var response = await next();
                sw.Stop();

                if (sw.ElapsedMilliseconds > 500)
                    _logger.LogWarning("Slow request: {RequestName} took {ElapsedMs}ms", requestName, sw.ElapsedMilliseconds);
                else
                    _logger.LogInformation("Handled {RequestName} in {ElapsedMs}ms", requestName, sw.ElapsedMilliseconds);

                return response;
            }
            catch (Exception ex)
            {
                sw.Stop();
                _logger.LogError(ex, "Request {RequestName} failed after {ElapsedMs}ms", requestName, sw.ElapsedMilliseconds);
                throw;
            }
        }
    }
}
