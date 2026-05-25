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

    /// <summary>
    /// EF Core implementation of IRatingRepository.
    /// GetByToolIdAsync includes the User navigation so RateToolCommandHandler.MapToResponse
    /// can access r.User.UserName without a second query.
    /// </summary>
    public sealed class RatingRepository : IRatingRepository
    {
        private readonly AppDbContext _db;

        public RatingRepository(AppDbContext db) => _db = db;

        public async Task<Rating?> GetByUserAndToolAsync(
            Guid userId, Guid toolId, CancellationToken ct = default) =>
            await _db.Ratings
                .FirstOrDefaultAsync(r => r.UserId == userId && r.ToolId == toolId, ct);

        public async Task<Rating?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
            await _db.Ratings.FindAsync([id], ct);

        public async Task<(IReadOnlyList<Rating> Items, int TotalCount)> GetByToolIdAsync(
            Guid toolId, int page, int pageSize, CancellationToken ct = default)
        {
            var query = _db.Ratings
                .Include(r => r.User)
                .Where(r => r.ToolId == toolId)
                .OrderByDescending(r => r.CreatedAt)
                .AsNoTracking();

            var totalCount = await query.CountAsync(ct);

            // When pageSize is int.MaxValue (stats query), skip paging
            var items = pageSize == int.MaxValue
                ? await query.ToListAsync(ct)
                : await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

            return (items.AsReadOnly(), totalCount);
        }

        public async Task<bool> ExistsAsync(
            Guid userId, Guid toolId, CancellationToken ct = default) =>
            await _db.Ratings.AnyAsync(r => r.UserId == userId && r.ToolId == toolId, ct);

        public async Task AddAsync(Rating rating, CancellationToken ct = default) =>
            await _db.Ratings.AddAsync(rating, ct);

        public void Update(Rating rating) =>
            _db.Ratings.Update(rating);

        public void Delete(Rating rating) =>
            _db.Ratings.Remove(rating);

        public async Task SaveChangesAsync(CancellationToken ct = default) =>
            await _db.SaveChangesAsync(ct);
    }
}
