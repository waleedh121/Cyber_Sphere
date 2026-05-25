using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CyberSphere.Domain.Entities
{
    /// <summary>
    /// Denormalized AI usage analytics per user.
    /// Updated on every successful AI message exchange.
    /// One row per user — created lazily on first AI request.
    /// Using a separate table avoids bloating the Users table with high-churn counters.
    /// </summary>
    public sealed class UserAiStats
    {
        public Guid UserId { get; private set; }
        public int TotalRequests { get; private set; }
        public int TotalTokensUsed { get; private set; }
        public int TotalSessions { get; private set; }
        public DateTime? LastActivityAt { get; private set; }
        public DateTime CreatedAt { get; private set; }

        // ── Navigation ────────────────────────────────────────────────────────────
        public User User { get; private set; } = null!;

        private UserAiStats() { }

        public static UserAiStats CreateForUser(Guid userId) =>
            new()
            {
                UserId = userId,
                TotalRequests = 0,
                TotalTokensUsed = 0,
                TotalSessions = 0,
                CreatedAt = DateTime.UtcNow
            };

        public void RecordRequest(int tokensUsed = 0)
        {
            TotalRequests++;
            TotalTokensUsed += tokensUsed;
            LastActivityAt = DateTime.UtcNow;
        }

        public void RecordSessionStarted() => TotalSessions++;
    }
}
