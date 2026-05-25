using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CyberSphere.Domain.Enums;

namespace CyberSphere.Domain.Entities
{

    public sealed class Tool
    {
        // ── Identity ──────────────────────────────────────────────────────────────
        public Guid Id { get; private set; }
        public Guid OwnerId { get; private set; }
        public Guid CategoryId { get; private set; }

        // ── Content ───────────────────────────────────────────────────────────────
        public string Name { get; private set; } = string.Empty;
        public string Description { get; private set; } = string.Empty;
        public DifficultyLevel DifficultyLevel { get; private set; }
        public string? GitHubUrl { get; private set; }
        public string Tags { get; private set; } = string.Empty; // CSV — e.g. "recon,network,passive"

        // ── Lifecycle ─────────────────────────────────────────────────────────────
        public ToolStatus Status { get; private set; }
        public DateTime CreatedAt { get; private set; }

        // ── Stats (denormalized for fast marketplace reads) ───────────────────────
        public int UsageCount { get; private set; }
        public decimal AverageRating { get; private set; }
        public int RatingCount { get; private set; }

        // ── Navigation ────────────────────────────────────────────────────────────
        public User Owner { get; private set; } = null!;
        public Category Category { get; private set; } = null!;
        public ICollection<ToolCommand> Commands { get; private set; } = new List<ToolCommand>();

        private Tool() { }

        // ── Factory ───────────────────────────────────────────────────────────────

        public static Tool Create(
            Guid ownerId,
            Guid categoryId,
            string name,
            string description,
            DifficultyLevel difficultyLevel,
            string? gitHubUrl = null,
            IEnumerable<string>? tags = null)
        {
            return new Tool
            {
                Id = Guid.NewGuid(),
                OwnerId = ownerId,
                CategoryId = categoryId,
                Name = name.Trim(),
                Description = description.Trim(),
                DifficultyLevel = difficultyLevel,
                GitHubUrl = gitHubUrl?.Trim(),
                Tags = tags is null ? string.Empty : string.Join(",", tags.Select(t => t.Trim().ToLowerInvariant())),
                Status = ToolStatus.Pending,
                UsageCount = 0,
                AverageRating = 0m,
                RatingCount = 0,
                CreatedAt = DateTime.UtcNow
            };
        }

        // ── Domain methods ────────────────────────────────────────────────────────

        public void Approve() => Status = ToolStatus.Approved;

        public void Reject() => Status = ToolStatus.Rejected;

        public void ResetToPending() => Status = ToolStatus.Pending;

        public void IncrementUsage() => UsageCount++;

        /// <summary>
        /// Recalculates AverageRating after a new rating is submitted.
        /// Incremental formula avoids loading all ratings from DB.
        /// </summary>
        public void AddRating(int stars)
        {
            if (stars < 1 || stars > 5) throw new ArgumentOutOfRangeException(nameof(stars), "Rating must be between 1 and 5.");
            RatingCount++;
            AverageRating +=  (stars - AverageRating) / RatingCount;
        }

        /// <summary>
        /// Recalculates AverageRating when an existing rating is updated.
        /// oldStars = previous value, newStars = updated value.
        /// </summary>
        public void UpdateRating(int oldStars, int newStars)
        {
            if (RatingCount == 0) return;
            AverageRating = ((AverageRating * RatingCount) - oldStars + newStars) / RatingCount;
        }

        public void UpdateDetails(
            Guid categoryId,
            string name,
            string description,
            DifficultyLevel difficultyLevel,
            string? gitHubUrl,
            IEnumerable<string>? tags)
        {
            CategoryId = categoryId;
            Name = name.Trim();
            Description = description.Trim();
            DifficultyLevel = difficultyLevel;
            GitHubUrl = gitHubUrl?.Trim();
            Tags = tags is null ? string.Empty : string.Join(",", tags.Select(t => t.Trim().ToLowerInvariant()));
        }

        /// <summary>
        /// Corrects the denormalized average when a rating is deleted.
        /// The caller pre-computes the new average and new count to avoid
        /// loading all ratings — we only store the corrected values.
        /// </summary>
        public void RemoveRating(decimal newAverage, int newCount)
        {
            AverageRating = newCount == 0 ? 0m : Math.Round(newAverage, 10);
            RatingCount = Math.Max(0, newCount);
        }

        /// <summary>Returns tags as a parsed list (never null).</summary>
        public IReadOnlyList<string> GetTags() =>
            string.IsNullOrWhiteSpace(Tags)
                ? []
                : Tags.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList().AsReadOnly();
    }
}
