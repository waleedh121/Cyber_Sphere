using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CyberSphere.Domain.Interfaces.Services
{
    /// <summary>
    /// Gateway interface for the external Python AI microservice.
    /// The Application layer depends only on this contract —
    /// it has zero knowledge of HttpClient, URLs, or serialization.
    /// Implemented in Infrastructure/Services/AI/PythonAiGatewayService.cs.
    /// The backend NEVER executes AI models directly.
    /// </summary>
    public interface IAiGatewayService
    {
        /// <summary>
        /// Forwards a conversation to the Python AI service and returns the assistant reply.
        /// </summary>
        /// <param name="conversationHistory">
        ///   Full ordered message history for this session — the Python service is stateless.
        ///   Include system prompt first, then alternating user/assistant messages.
        /// </param>
        /// <param name="ct">Cancellation token — respects the configured TimeoutSeconds.</param>
        /// <returns>The AI response payload including reply content and token usage.</returns>
        Task<AiGatewayResponse> SendAsync(
            IReadOnlyList<AiGatewayMessage> conversationHistory,
            CancellationToken ct = default);
    }

    // ── Value objects passed between Application and Infrastructure ───────────────

    public sealed record AiGatewayMessage(
        string Role,     // "user" | "assistant" | "system"
        string Content
    );

    public sealed record AiGatewayResponse(
        bool Success,
        string Content,
        int TokensUsed,
        int LatencyMs,
        string? ErrorMessage = null
    );
}
