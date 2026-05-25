using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CyberSphere.Domain.Entities;
using CyberSphere.Domain.Enums;
using CyberSphere.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CyberSphere.Infrastructure.Persistence.Repositories
{


    /// <summary>
    /// EF Core implementation of IToolRepository.
    /// All marketplace queries include Owner + Category via .Include() — required
    /// to populate the mandatory OwnerProfileDto without N+1 queries.
    /// </summary>
    public sealed class ToolRepository : IToolRepository
    {
        private readonly AppDbContext _db;

        public ToolRepository(AppDbContext db) => _db = db;

        // ── Base queryable: approved tools with all navigation needed for DTOs ────

        private IQueryable<Tool> ApprovedWithOwnerAndCategory =>
            _db.Tools
                .Where(t => t.Status == ToolStatus.Approved)
                .Include(t => t.Owner)
                .Include(t => t.Category)
                .AsNoTracking();

        // ── Single fetches ────────────────────────────────────────────────────────

        public async Task<Tool?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
            await _db.Tools.FindAsync([id], ct);

        public async Task<Tool?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct = default) =>
            await _db.Tools
                .Include(t => t.Owner)
                .Include(t => t.Category)
                .Include(t => t.Commands)
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == id, ct);

        public async Task<Tool?> GetByIdWithOwnerAsync(Guid id, CancellationToken ct = default) =>
            await _db.Tools
                .Include(t => t.Owner)
                .FirstOrDefaultAsync(t => t.Id == id, ct);

        // ── Admin review queries ───────────────────────────────────────────────────

        public async Task<(IReadOnlyList<Tool> Items, int TotalCount)> GetAllPendingAsync(
            int page, int pageSize, CancellationToken ct = default)
        {
            var query = _db.Tools
                .Where(t => t.Status == ToolStatus.Pending)
                .Include(t => t.Owner)
                .Include(t => t.Category)
                .OrderBy(t => t.CreatedAt)   // oldest first — fair FIFO review queue
                .AsNoTracking();

            var totalCount = await query.CountAsync(ct);
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            return (items.AsReadOnly(), totalCount);
        }

        public async Task<IReadOnlyList<Tool>> GetAllByOwnerAsync(
            Guid ownerId, CancellationToken ct = default)
        {
            var tools = await _db.Tools
                .Where(t => t.OwnerId == ownerId)
                .Include(t => t.Category)
                .Include(t => t.Commands)
                .OrderByDescending(t => t.CreatedAt)
                .AsNoTracking()
                .ToListAsync(ct);

            return tools.AsReadOnly();
        }

        // ── Paged marketplace (Specification via inline LINQ) ─────────────────────

        public async Task<(IReadOnlyList<Tool> Items, int TotalCount)> GetPagedAsync(
            ToolFilterParams filter, CancellationToken ct = default)
        {
            var query = ApprovedWithOwnerAndCategory;

            // ── Filters ───────────────────────────────────────────────────────────
            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var term = filter.Search.ToLower();
                query = query.Where(t =>
                    t.Name.ToLower().Contains(term) ||
                    t.Description.ToLower().Contains(term) ||
                    t.Tags.ToLower().Contains(term));
            }

            if (filter.CategoryId.HasValue)
                query = query.Where(t => t.CategoryId == filter.CategoryId.Value);

            if (filter.DifficultyLevel.HasValue)
                query = query.Where(t => t.DifficultyLevel == filter.DifficultyLevel.Value);

            // ── Sort ──────────────────────────────────────────────────────────────
            query = filter.SortBy switch
            {
                ToolSortBy.MostUsed => query.OrderByDescending(t => t.UsageCount),
                ToolSortBy.HighestRated => query.OrderByDescending(t => t.AverageRating)
                                               .ThenByDescending(t => t.RatingCount),
                ToolSortBy.Alphabetical => query.OrderBy(t => t.Name),
                _ => query.OrderByDescending(t => t.CreatedAt)   // Newest
            };

            // ── Count before paging (single round-trip via Future or split query) ─
            var totalCount = await query.CountAsync(ct);

            var items = await query
                .Skip(filter.Skip)
                .Take(filter.PageSize)
                .ToListAsync(ct);

            return (items.AsReadOnly(), totalCount);
        }

        // ── Owner tools (profile page) ────────────────────────────────────────────

        public async Task<IReadOnlyList<Tool>> GetApprovedByOwnerAsync(
            Guid ownerId, CancellationToken ct = default)
        {
            var tools = await _db.Tools
                .Where(t => t.OwnerId == ownerId && t.Status == ToolStatus.Approved)
                .Include(t => t.Owner)
                .Include(t => t.Category)
                .OrderByDescending(t => t.CreatedAt)
                .AsNoTracking()
                .ToListAsync(ct);

            return tools.AsReadOnly();
        }

        // ── Existence / ownership ─────────────────────────────────────────────────

        public async Task<bool> ExistsByNameAsync(string name, CancellationToken ct = default) =>
            await _db.Tools.AnyAsync(t => t.Name == name, ct);

        public async Task<bool> IsOwnedByAsync(Guid toolId, Guid userId, CancellationToken ct = default) =>
            await _db.Tools.AnyAsync(t => t.Id == toolId && t.OwnerId == userId, ct);

        // ── Stats ─────────────────────────────────────────────────────────────────

        public async Task<IReadOnlyList<Domain.Entities.Tool>> GetAllForStatsAsync(
            CancellationToken ct = default)
        {
            var tools = await _db.Tools
                .Include(t => t.Category)
                .OrderBy(t => t.Name)
                .AsNoTracking()
                .ToListAsync(ct);

            return tools.AsReadOnly();
        }

        // ── Write ─────────────────────────────────────────────────────────────────

        public async Task AddAsync(Tool tool, CancellationToken ct = default) =>
            await _db.Tools.AddAsync(tool, ct);

        public void Update(Tool tool) =>
            _db.Tools.Update(tool);

        public void Delete(Tool tool) =>
            _db.Tools.Remove(tool);

        public async Task SaveChangesAsync(CancellationToken ct = default) =>
            await _db.SaveChangesAsync(ct);
    }
}
