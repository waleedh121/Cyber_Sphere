using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CyberSphere.Application.Features.Ai.DTOs
{

    // ── Requests ──────────────────────────────────────────────────────────────────

    public sealed record StartAiSessionRequest(string? Title);

    public sealed record SendAiMessageRequest(string Message);

    // ── Responses ─────────────────────────────────────────────────────────────────

    public sealed record AiSessionResponse(
        Guid Id,
        string Title,
        string Status,
        DateTime StartedAt,
        DateTime? EndedAt,
        int MessageCount
    );

    public sealed record AiMessageResponse(
        Guid Id,
        string Role,
        string Content,
        bool IsError,
        int? TokensUsed,
        int? LatencyMs,
        DateTime CreatedAt
    );

    public sealed record AiChatResponse(
        Guid SessionId,
        AiMessageResponse UserMessage,
        AiMessageResponse AssistantMessage
    );

    public sealed record AiSessionWithHistoryResponse(
        Guid Id,
        string Title,
        string Status,
        DateTime StartedAt,
        DateTime? EndedAt,
        IReadOnlyList<AiMessageResponse> Messages
    );

    public sealed record UserAiStatsResponse(
        int TotalRequests,
        int TotalTokensUsed,
        int TotalSessions,
        DateTime? LastActivityAt
    );
}
