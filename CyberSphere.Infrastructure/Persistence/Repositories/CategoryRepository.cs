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
    /// EF Core implementation of ICategoryRepository.
    /// GetAllAsync includes Tools navigation so GetCategoriesQuery can count
    /// approved tools per category without an extra round-trip.
    /// </summary>
    public sealed class CategoryRepository : ICategoryRepository
    {
        private readonly AppDbContext _db;

        public CategoryRepository(AppDbContext db) => _db = db;

        public async Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken ct = default)
        {
            var categories = await _db.Categories
                .Include(c => c.Tools)
                .OrderBy(c => c.Name)
                .AsNoTracking()
                .ToListAsync(ct);

            return categories.AsReadOnly();
        }

        public async Task<Category?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
            await _db.Categories.FindAsync([id], ct);

        public async Task<Category?> GetBySlugAsync(string slug, CancellationToken ct = default) =>
            await _db.Categories
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Slug == slug.ToLowerInvariant(), ct);

        public async Task<bool> ExistsByNameAsync(string name, CancellationToken ct = default) =>
            await _db.Categories.AnyAsync(c => c.Name == name, ct);

        public async Task AddAsync(Category category, CancellationToken ct = default) =>
            await _db.Categories.AddAsync(category, ct);

        public async Task SaveChangesAsync(CancellationToken ct = default) =>
            await _db.SaveChangesAsync(ct);
    }
}
