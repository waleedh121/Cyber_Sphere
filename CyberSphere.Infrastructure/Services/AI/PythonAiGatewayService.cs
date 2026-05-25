using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;
using CyberSphere.Domain.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace CyberSphere.Infrastructure.Services.AI
{
    /// <summary>
    /// Forwards AI requests to the external Python AI microservice via HTTP.
    /// Uses IHttpClientFactory (named client "PythonAiService") — registered in
    /// InfrastructureServiceExtensions with the configured BaseUrl and TimeoutSeconds.
    ///
    /// CONTRACT: The .NET backend NEVER executes AI models directly.
    /// This class is the only place where Python service communication happens.
    ///
    /// Expected Python service endpoint:
    ///   POST /chat
    ///   Body: { "messages": [{ "role": "...", "content": "..." }] }
    ///   Response: { "content": "...", "tokens_used": 123 }
    /// </summary>
    public sealed class PythonAiGatewayService : IAiGatewayService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<PythonAiGatewayService> _logger;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNameCaseInsensitive = true
        };

        public PythonAiGatewayService(
            IHttpClientFactory httpClientFactory,
            ILogger<PythonAiGatewayService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<AiGatewayResponse> SendAsync(
            IReadOnlyList<AiGatewayMessage> conversationHistory,
            CancellationToken ct = default)
        {
            var sw = Stopwatch.StartNew();

            try
            {
                var client = _httpClientFactory.CreateClient(AiSettings.HttpClientName);

                var requestBody = new PythonAiRequest(
                    Messages: conversationHistory
                        .Select(m => new PythonAiMessagePayload(m.Role, m.Content))
                        .ToList());

                var json = JsonSerializer.Serialize(requestBody, JsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _logger.LogDebug(
                    "Sending {MessageCount} messages to Python AI service",
                    conversationHistory.Count);

                var httpResponse = await client.PostAsync("/chat", content, ct);
                sw.Stop();

                if (!httpResponse.IsSuccessStatusCode)
                {
                    var errorBody = await httpResponse.Content.ReadAsStringAsync(ct);
                    _logger.LogWarning(
                        "Python AI service returned {StatusCode}: {Body}",
                        httpResponse.StatusCode, errorBody);

                    return FailedResponse(
                        sw.Elapsed,
                        $"AI service returned {(int)httpResponse.StatusCode}: {httpResponse.ReasonPhrase}");
                }

                var responseJson = await httpResponse.Content.ReadAsStringAsync(ct);
                var aiReply = JsonSerializer.Deserialize<PythonAiResponse>(responseJson, JsonOptions);

                if (aiReply is null || string.IsNullOrWhiteSpace(aiReply.Content))
                {
                    _logger.LogWarning("Python AI service returned empty response body");
                    return FailedResponse(sw.Elapsed, "AI service returned an empty response");
                }

                return new AiGatewayResponse(
                    Success: true,
                    Content: aiReply.Content,
                    TokensUsed: aiReply.TokensUsed ?? 0,
                    LatencyMs: (int)sw.ElapsedMilliseconds,
                    ErrorMessage: null);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                sw.Stop();
                _logger.LogWarning("AI request cancelled after {Ms}ms", sw.ElapsedMilliseconds);
                return FailedResponse(sw.Elapsed, "The AI request was cancelled.");
            }
            catch (TaskCanceledException)
            {
                // HttpClient timeout fires as TaskCanceledException
                sw.Stop();
                _logger.LogWarning("AI request timed out after {Ms}ms", sw.ElapsedMilliseconds);
                return FailedResponse(sw.Elapsed, "The AI service did not respond in time. Please try again.");
            }
            catch (HttpRequestException ex)
            {
                sw.Stop();
                _logger.LogError(ex, "Network error reaching Python AI service after {Ms}ms", sw.ElapsedMilliseconds);
                return FailedResponse(sw.Elapsed, "Could not reach the AI service. Please try again later.");
            }
            catch (Exception ex)
            {
                sw.Stop();
                _logger.LogError(ex, "Unexpected error in AI gateway after {Ms}ms", sw.ElapsedMilliseconds);
                return FailedResponse(sw.Elapsed, "An unexpected error occurred.");
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private static AiGatewayResponse FailedResponse(TimeSpan elapsed, string error) =>
            new(
                Success: false,
                Content: string.Empty,
                TokensUsed: 0,
                LatencyMs: (int)elapsed.TotalMilliseconds,
                ErrorMessage: error);

        // ── Python service payload types ──────────────────────────────────────────

        private sealed record PythonAiRequest(List<PythonAiMessagePayload> Messages);
        private sealed record PythonAiMessagePayload(string Role, string Content);
        private sealed record PythonAiResponse(string Content, int? TokensUsed);
    }
}
