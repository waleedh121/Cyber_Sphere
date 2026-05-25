using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CyberSphere.Domain.Entities
{

    /// <summary>
    /// One rating per user per tool (enforced by unique index on UserId + ToolId).
    /// Stars are 1–5. Comment is optional.
    /// AverageRating on the Tool entity is updated via Tool.AddRating / Tool.UpdateRating
    /// whenever a Rating row is created or modified — the two are always in sync.
    /// </summary>
    public sealed class Rating
    {
        public Guid Id { get; private set; }
        public Guid ToolId { get; private set; }
        public Guid UserId { get; private set; }
        public int Stars { get; private set; }
        public string? Comment { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime UpdatedAt { get; private set; }

        // ── Navigation ────────────────────────────────────────────────────────────
        public Tool Tool { get; private set; } = null!;
        public User User { get; private set; } = null!;

        private Rating() { }

        public static Rating Create(Guid toolId, Guid userId, int stars, string? comment)
        {
            ValidateStars(stars);
            return new Rating
            {
                Id = Guid.NewGuid(),
                ToolId = toolId,
                UserId = userId,
                Stars = stars,
                Comment = comment?.Trim(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Updates the rating. Returns the previous Stars value so the
        /// caller can pass it to Tool.UpdateRating(oldStars, newStars).
        /// </summary>
        public int Update(int newStars, string? newComment)
        {
            ValidateStars(newStars);
            var oldStars = Stars;
            Stars = newStars;
            Comment = newComment?.Trim();
            UpdatedAt = DateTime.UtcNow;
            return oldStars;
        }

        private static void ValidateStars(int stars)
        {
            if (stars < 1 || stars > 5)
                throw new ArgumentOutOfRangeException(nameof(stars), "Rating must be between 1 and 5.");
        }
    }
}
