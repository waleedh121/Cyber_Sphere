using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CyberSphere.Domain.Entities;
using CyberSphere.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CyberSphere.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of IUserRepository.
/// Scoped — lives for the duration of one HTTP request.
/// </summary>
public sealed class UserRepository : IUserRepository
{
    private readonly AppDbContext _db;

    public UserRepository(AppDbContext db) => _db = db;

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await _db.Users.FindAsync([id], ct);

    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default) =>
        await _db.Users
            .FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant(), ct);

    public async Task<User?> GetByUserNameAsync(string userName, CancellationToken ct = default) =>
        await _db.Users
            .FirstOrDefaultAsync(u => u.UserName == userName, ct);

    /// <summary>
    /// Loads user + Tools + Category navigation in one query.
    /// The ThenInclude(t => t.Category) is mandatory for Feature 6 —
    /// GetPublicProfileQuery maps Category.Name and Category.Slug directly.
    /// Tools are filtered to Approved in the Application layer, not here,
    /// to keep the repository free of business logic.
    /// </summary>
    public async Task<User?> GetByUserNameWithToolsAsync(
        string userName, CancellationToken ct = default) =>
        await _db.Users
            .Include(u => u.Tools)
                .ThenInclude(t => t.Category)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserName == userName, ct);

    public async Task<User?> GetByRefreshTokenAsync(
        string refreshToken, CancellationToken ct = default) =>
        await _db.Users
            .FirstOrDefaultAsync(u => u.RefreshToken == refreshToken, ct);

    public async Task<bool> ExistsByEmailAsync(
        string email, CancellationToken ct = default) =>
        await _db.Users
            .AnyAsync(u => u.Email == email.ToLowerInvariant(), ct);

    public async Task<bool> ExistsByUserNameAsync(
        string userName, CancellationToken ct = default) =>
        await _db.Users
            .AnyAsync(u => u.UserName == userName, ct);
   
    public async Task<IReadOnlyList<User>> GetAllForDashboardAsync(
    CancellationToken ct = default)
    {
        var users = await _db.Users
            .OrderBy(u => u.CreatedAt)
            .AsNoTracking()
            .ToListAsync(ct);

        return users.AsReadOnly();
    }


    public async Task AddAsync(User user, CancellationToken ct = default) =>
        await _db.Users.AddAsync(user, ct);

    public async Task SaveChangesAsync(CancellationToken ct = default) =>
        await _db.SaveChangesAsync(ct);
}
