using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CyberSphere.Domain.Enums;

namespace CyberSphere.Domain.Entities
{
    /// <summary>
    /// Immutable chat message within an AiSession.
    /// Stores both the user's prompt and the AI assistant's reply as separate rows.
    /// Tokens are tracked for analytics; latency allows performance monitoring.
    /// </summary>
    public sealed class AiMessage
    {
        public Guid Id { get; private set; }
        public Guid SessionId { get; private set; }
        public AiMessageRole Role { get; private set; }
        public string Content { get; private set; } = string.Empty;

        // ── Analytics fields (populated on assistant messages only) ──────────────
        public int? TokensUsed { get; private set; }
        public int? LatencyMs { get; private set; }
        public bool IsError { get; private set; }
        public string? ErrorMessage { get; private set; }

        public DateTime CreatedAt { get; private set; }

        // ── Navigation ────────────────────────────────────────────────────────────
        public AiSession Session { get; private set; } = null!;

        private AiMessage() { }

        public static AiMessage CreateUserMessage(Guid sessionId, string content) =>
            new()
            {
                Id = Guid.NewGuid(),
                SessionId = sessionId,
                Role = AiMessageRole.User,
                Content = content.Trim(),
                CreatedAt = DateTime.UtcNow
            };

        public static AiMessage CreateAssistantMessage(
            Guid sessionId,
            string content,
            int? tokensUsed = null,
            int? latencyMs = null) =>
            new()
            {
                Id = Guid.NewGuid(),
                SessionId = sessionId,
                Role = AiMessageRole.Assistant,
                Content = content.Trim(),
                TokensUsed = tokensUsed,
                LatencyMs = latencyMs,
                CreatedAt = DateTime.UtcNow
            };

        public static AiMessage CreateErrorMessage(Guid sessionId, string errorMessage) =>
            new()
            {
                Id = Guid.NewGuid(),
                SessionId = sessionId,
                Role = AiMessageRole.Assistant,
                Content = "I'm sorry, I couldn't process your request right now. Please try again.",
                IsError = true,
                ErrorMessage = errorMessage,
                CreatedAt = DateTime.UtcNow
            };
    }
}
