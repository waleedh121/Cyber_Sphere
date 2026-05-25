using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CyberSphere.Domain.Entities;
using CyberSphere.Domain.Enums;

namespace CyberSphere.Domain.Interfaces.Repositories
{

    public interface IToolRepository
    {
        // ── Single-entity fetches ─────────────────────────────────────────────────
        Task<Tool?> GetByIdAsync(Guid id, CancellationToken ct = default);

        /// <summary>Loads Tool + Owner + Category + Commands (full detail page).</summary>
        Task<Tool?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct = default);

        /// <summary>Loads Tool + Owner only — used by submission review flow.</summary>
        Task<Tool?> GetByIdWithOwnerAsync(Guid id, CancellationToken ct = default);

        // ── Admin review queries ───────────────────────────────────────────────────
        Task<(IReadOnlyList<Tool> Items, int TotalCount)> GetAllPendingAsync(int page, int pageSize, CancellationToken ct = default);
        Task<IReadOnlyList<Tool>> GetAllByOwnerAsync(Guid ownerId, CancellationToken ct = default);

        // ── Paged marketplace queries (Specification pattern) ─────────────────────
        Task<(IReadOnlyList<Tool> Items, int TotalCount)> GetPagedAsync(
            ToolFilterParams filter, CancellationToken ct = default);

        // ── Existence / ownership checks ──────────────────────────────────────────
        Task<bool> ExistsByNameAsync(string name, CancellationToken ct = default);
        Task<bool> IsOwnedByAsync(Guid toolId, Guid userId, CancellationToken ct = default);

        /// <summary>All Approved tools for a given owner (profile page, owner attribution).</summary>
        Task<IReadOnlyList<Tool>> GetApprovedByOwnerAsync(Guid ownerId, CancellationToken ct = default);

        // ── Stats queries ─────────────────────────────────────────────────────────
        /// <summary>
        /// Loads all tools with Category navigation for platform-wide stats.
        /// Returns all statuses — caller filters by status.
        /// </summary>
        Task<IReadOnlyList<Domain.Entities.Tool>> GetAllForStatsAsync(CancellationToken ct = default);

        // ── Write ─────────────────────────────────────────────────────────────────
        Task AddAsync(Tool tool, CancellationToken ct = default);
        void Update(Tool tool);
        void Delete(Tool tool);
        Task SaveChangesAsync(CancellationToken ct = default);
    }

    // ── Filter parameters (replaces Specification class for simplicity) ───────────

    public sealed class ToolFilterParams
    {
        public string? Search { get; init; }
        public Guid? CategoryId { get; init; }
        public DifficultyLevel? DifficultyLevel { get; init; }
        public ToolSortBy SortBy { get; init; } = ToolSortBy.Newest;
        public int Page { get; init; } = 1;
        public int PageSize { get; init; } = 12;

        public int Skip => (Page - 1) * PageSize;
    }

    public enum ToolSortBy
    {
        Newest = 0,
        MostUsed = 1,
        HighestRated = 2,
        Alphabetical = 3
    }
}
