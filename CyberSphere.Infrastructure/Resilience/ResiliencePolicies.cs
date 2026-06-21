using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using Microsoft.Extensions.Logging;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;
using Polly;

namespace CyberSphere.Infrastructure.Resilience
{

    /// <summary>
    /// Factory for reusable Polly v8 resilience pipelines.
    /// Pipelines are composed in order (outermost to innermost):
    ///   Timeout → CircuitBreaker → Retry
    /// This means the timeout applies to each individual attempt,
    /// and the circuit-breaker trips after enough failures accumulate.
    /// </summary>
    public static class ResiliencePolicies
    {
        // ── AI Gateway (HttpClient) ───────────────────────────────────────────────

        /// <summary>
        /// Resilience pipeline for outbound calls to the Python AI microservice.
        ///
        /// Strategy:
        ///   1. Retry — up to 2 retries on transient HTTP failures (5xx, 408, 429)
        ///              with exponential back-off (2s → 4s) plus jitter.
        ///   2. Circuit-breaker — trips after 5 consecutive failures;
        ///                        stays open for 30s before half-opening.
        ///
        /// Per-request timeout is handled by HttpClient.Timeout in AiSettings (60s).
        /// </summary>
        public static ResiliencePipeline<HttpResponseMessage> CreateAiHttpPipeline(
            ILogger logger)
        {
            return new ResiliencePipelineBuilder<HttpResponseMessage>()
                // Inner: retry — closest to the actual call
                .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
                {
                    MaxRetryAttempts = 2,
                    Delay = TimeSpan.FromSeconds(2),
                    BackoffType = DelayBackoffType.Exponential,
                    UseJitter = true,
                    ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                        .HandleResult(r =>
                            r.StatusCode is HttpStatusCode.InternalServerError
                                         or HttpStatusCode.BadGateway
                                         or HttpStatusCode.ServiceUnavailable
                                         or HttpStatusCode.GatewayTimeout
                                         or HttpStatusCode.RequestTimeout
                                         or HttpStatusCode.TooManyRequests)
                        .Handle<HttpRequestException>(),
                    OnRetry = args =>
                    {
                        logger.LogWarning(
                            "AI gateway retry #{Attempt} after {Delay}ms. Outcome: {Outcome}",
                            args.AttemptNumber + 1,
                            args.RetryDelay.TotalMilliseconds,
                            args.Outcome.Result?.StatusCode.ToString() ?? args.Outcome.Exception?.Message);
                        return ValueTask.CompletedTask;
                    }
                })
                // Outer: circuit-breaker — wraps the retry
                .AddCircuitBreaker(new CircuitBreakerStrategyOptions<HttpResponseMessage>
                {
                    FailureRatio = 0.5,
                    MinimumThroughput = 5,
                    SamplingDuration = TimeSpan.FromSeconds(30),
                    BreakDuration = TimeSpan.FromSeconds(30),
                    ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                        .HandleResult(r => !r.IsSuccessStatusCode)
                        .Handle<HttpRequestException>(),
                    OnOpened = args =>
                    {
                        logger.LogError(
                            "AI gateway circuit breaker OPENED for {Duration}s. " +
                            "All AI requests will fail-fast until it half-opens.",
                            args.BreakDuration.TotalSeconds);
                        return ValueTask.CompletedTask;
                    },
                    OnClosed = _ =>
                    {
                        logger.LogInformation("AI gateway circuit breaker CLOSED — service recovered.");
                        return ValueTask.CompletedTask;
                    },
                    OnHalfOpened = _ =>
                    {
                        logger.LogInformation("AI gateway circuit breaker HALF-OPEN — probing service.");
                        return ValueTask.CompletedTask;
                    }
                })
                .Build();
        }

        // ── SSH VM calls ──────────────────────────────────────────────────────────

        /// <summary>
        /// Resilience pipeline for SSH operations against VM infrastructure.
        ///
        /// Strategy:
        ///   1. Retry — 1 retry on transient exceptions only (not auth failures).
        ///              Fixed 3s delay.
        ///   2. Timeout — 20s per attempt ceiling (guards against SSH hangs).
        ///
        /// Auth failures (SshAuthenticationException) are NOT retried — they are
        /// credential/config issues that require human intervention.
        /// </summary>
        public static ResiliencePipeline CreateVmSshPipeline(ILogger logger)
        {
            return new ResiliencePipelineBuilder()
                .AddRetry(new RetryStrategyOptions
                {
                    MaxRetryAttempts = 1,
                    Delay = TimeSpan.FromSeconds(3),
                    BackoffType = DelayBackoffType.Constant,
                    ShouldHandle = new PredicateBuilder()
                        .Handle<Exception>(ex =>
                            // Only retry transient network/connection errors, not auth
                            ex is not Renci.SshNet.Common.SshAuthenticationException
                               and not OperationCanceledException),
                    OnRetry = args =>
                    {
                        logger.LogWarning(
                            "SSH VM call retry #{Attempt}: {ExceptionMessage}",
                            args.AttemptNumber + 1,
                            args.Outcome.Exception?.Message);
                        return ValueTask.CompletedTask;
                    }
                })
                .AddTimeout(new TimeoutStrategyOptions
                {
                    Timeout = TimeSpan.FromSeconds(20),
                    OnTimeout = args =>
                    {
                        logger.LogWarning(
                            "SSH VM call timed out after {Timeout}s",
                            args.Timeout.TotalSeconds);
                        return ValueTask.CompletedTask;
                    }
                })
                .Build();
        }
    }

}
