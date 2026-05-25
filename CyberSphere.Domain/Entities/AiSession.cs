using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using CyberSphere.Domain.Enums;

namespace CyberSphere.Domain.Entities
{
    /// <summary>
    /// Logical AI conversation session — NOT a VM execution session.
    /// Groups a sequence of AiMessage rows for one continuous conversation.
    /// Stored separately from the Session entity (which tracks VM assignments).
    /// </summary>
    public sealed class AiSession
    {
        public Guid Id { get; private set; }
        public Guid UserId { get; private set; }
        public string Title { get; private set; } = string.Empty;
        public AiSessionStatus Status { get; private set; }
        public DateTime StartedAt { get; private set; }
        public DateTime? EndedAt { get; private set; }

        // ── Navigation ────────────────────────────────────────────────────────────
        public User User { get; private set; } = null!;
        public ICollection<AiMessage> Messages { get; private set; } = new List<AiMessage>();
        private AiSession() { }

        public static AiSession Create(Guid userId, string title = "New AI Chat")
        {
            return new AiSession
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Title = title.Trim(),
                Status = AiSessionStatus.Active,
                StartedAt = DateTime.UtcNow
            };
        }

        public void UpdateTitle(string title) =>
            Title = string.IsNullOrWhiteSpace(title) ? Title : title.Trim();

        public void End()
        {
            if (Status == AiSessionStatus.Active)
            {
                Status = AiSessionStatus.Ended;
                EndedAt = DateTime.UtcNow;
            }
        }

        public bool IsActive => Status == AiSessionStatus.Active;
    }
}
