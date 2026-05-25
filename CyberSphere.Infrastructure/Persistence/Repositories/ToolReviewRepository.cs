using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CyberSphere.Domain.Entities;
using CyberSphere.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CyberSphere.Infrastructure.Persistence.Repositories
{
    public sealed class ToolReviewRepository : IToolReviewRepository
    {
        private readonly AppDbContext _db;

        public ToolReviewRepository(AppDbContext db) => _db = db;

        public async Task<IReadOnlyList<ToolReview>> GetByToolIdAsync(
            Guid toolId, CancellationToken ct = default)
        {
            var reviews = await _db.ToolReviews
                .Include(r => r.Admin)
                .Where(r => r.ToolId == toolId)
                .OrderByDescending(r => r.ReviewedAt)
                .AsNoTracking()
                .ToListAsync(ct);

            return reviews.AsReadOnly();
        }

        public async Task<ToolReview?> GetLatestByToolIdAsync(
            Guid toolId, CancellationToken ct = default) =>
            await _db.ToolReviews
                .Include(r => r.Admin)
                .Where(r => r.ToolId == toolId)
                .OrderByDescending(r => r.ReviewedAt)
                .AsNoTracking()
                .FirstOrDefaultAsync(ct);

        public async Task AddAsync(ToolReview review, CancellationToken ct = default) =>
            await _db.ToolReviews.AddAsync(review, ct);

        public async Task SaveChangesAsync(CancellationToken ct = default) =>
            await _db.SaveChangesAsync(ct);
    }
}
