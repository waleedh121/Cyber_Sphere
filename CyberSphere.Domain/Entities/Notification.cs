using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CyberSphere.Domain.Enums;

namespace CyberSphere.Domain.Entities
{

    /// <summary>
    /// A single notification delivered to a user.
    /// Notifications are created by application handlers (never by the user).
    /// RelatedEntityId is an optional FK reference — e.g. the ToolId that was
    /// approved, or the RatingId that was left — so the frontend can deep-link.
    /// </summary>
    public sealed class Notification
    {
        public Guid Id { get; private set; }
        public Guid UserId { get; private set; }
        public NotificationType Type { get; private set; }
        public string Title { get; private set; } = string.Empty;
        public string Message { get; private set; } = string.Empty;
        public bool IsRead { get; private set; }
        public Guid? RelatedEntityId { get; private set; }   // ToolId / RatingId
        public string? RelatedEntityType { get; private set; } // "Tool" / "Rating"
        public DateTime CreatedAt { get; private set; }
        public DateTime? ReadAt { get; private set; }

        // ── Navigation ────────────────────────────────────────────────────────────
        public User User { get; private set; } = null!;

        private Notification() { }

        public static Notification Create(
            Guid userId,
            NotificationType type,
            string title,
            string message,
            Guid? relatedEntityId = null,
            string? relatedEntityType = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(title);
            ArgumentException.ThrowIfNullOrWhiteSpace(message);

            return new Notification
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Type = type,
                Title = title.Trim(),
                Message = message.Trim(),
                IsRead = false,
                RelatedEntityId = relatedEntityId,
                RelatedEntityType = relatedEntityType,
                CreatedAt = DateTime.UtcNow
            };
        }

        /// <summary>Marks the notification as read. Idempotent.</summary>
        public void MarkAsRead()
        {
            if (!IsRead)
            {
                IsRead = true;
                ReadAt = DateTime.UtcNow;
            }
        }
    }
}
