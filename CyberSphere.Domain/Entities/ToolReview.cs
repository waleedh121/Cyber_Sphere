using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CyberSphere.Domain.Enums;

namespace CyberSphere.Domain.Entities
{
    /// <summary>
    /// Immutable audit record created whenever an admin approves or rejects a tool.
    /// A tool can have multiple ToolReview rows across successive submission cycles
    /// (submitted → rejected → resubmitted → approved).
    /// </summary>
    public sealed class ToolReview
    {
        public Guid Id { get; private set; }
        public Guid ToolId { get; private set; }
        public Guid AdminId { get; private set; }
        public ReviewDecision Decision { get; private set; }
        public string? Notes { get; private set; }
        public DateTime ReviewedAt { get; private set; }

        // ── Navigation ────────────────────────────────────────────────────────────
        public Tool Tool { get; private set; } = null!;
        public User Admin { get; private set; } = null!;

        private ToolReview() { }

        /// <summary>
        /// Factory — called by ReviewToolCommandHandler only.
        /// Every review decision creates a new row; existing rows are never mutated.
        /// </summary>
        public static ToolReview Create(
            Guid toolId,
            Guid adminId,
            ReviewDecision decision,
            string? notes = null)
        {
            return new ToolReview
            {
                Id = Guid.NewGuid(),
                ToolId = toolId,
                AdminId = adminId,
                Decision = decision,
                Notes = notes?.Trim(),
                ReviewedAt = DateTime.UtcNow
            };
        }
    }
}
