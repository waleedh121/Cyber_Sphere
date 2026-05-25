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
    public sealed class SessionRepository : ISessionRepository
    {
        private readonly AppDbContext _db;

        public SessionRepository(AppDbContext db) => _db = db;

        public async Task<Session?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
            await _db.Sessions.FindAsync([id], ct);

        public async Task<Session?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct = default) =>
            await _db.Sessions
                .Include(s => s.Server)
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.Id == id, ct);

        public async Task<IReadOnlyList<Session>> GetActiveByUserIdAsync(
            Guid userId, CancellationToken ct = default)
        {
            var sessions = await _db.Sessions
                .Include(s => s.Server)
                .Where(s => s.UserId == userId && s.Status == SessionStatus.Active)
                .OrderBy(s => s.StartedAt)
                .AsNoTracking()
                .ToListAsync(ct);

            return sessions.AsReadOnly();
        }

        public async Task<(IReadOnlyList<Session> Items, int TotalCount)> GetHistoryByUserIdAsync(
            Guid userId, int page, int pageSize, CancellationToken ct = default)
        {
            var query = _db.Sessions
                .Include(s => s.Server)
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.StartedAt)
                .AsNoTracking();

            var totalCount = await query.CountAsync(ct);
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            return (items.AsReadOnly(), totalCount);
        }

        public async Task<IReadOnlyList<Session>> GetAllActiveAsync(CancellationToken ct = default)
        {
            var sessions = await _db.Sessions
                .Include(s => s.Server)
                .Include(s => s.User)
                .Where(s => s.Status == SessionStatus.Active)
                .OrderBy(s => s.StartedAt)
                .AsNoTracking()
                .ToListAsync(ct);

            return sessions.AsReadOnly();
        }

        public async Task<(IReadOnlyList<Session> Items, int TotalCount)> GetAllPagedAsync(
            int page, int pageSize, CancellationToken ct = default)
        {
            var query = _db.Sessions
                .Include(s => s.Server)
                .Include(s => s.User)
                .OrderByDescending(s => s.StartedAt)
                .AsNoTracking();

            var totalCount = await query.CountAsync(ct);
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            return (items.AsReadOnly(), totalCount);
        }

        public async Task<IReadOnlyList<Session>> GetActiveByServerIdAsync(
            Guid serverId, CancellationToken ct = default)
        {
            var sessions = await _db.Sessions
                .Where(s => s.ServerId == serverId && s.Status == SessionStatus.Active)
                .AsNoTracking()
                .ToListAsync(ct);

            return sessions.AsReadOnly();
        }

        public async Task<bool> HasActiveSessionOfTypeAsync(
            Guid userId, SessionType type, CancellationToken ct = default) =>
            await _db.Sessions.AnyAsync(
                s => s.UserId == userId && s.Type == type && s.Status == SessionStatus.Active, ct);

        public async Task AddAsync(Session session, CancellationToken ct = default) =>
            await _db.Sessions.AddAsync(session, ct);

        public void Update(Session session) =>
            _db.Sessions.Update(session);

        public async Task SaveChangesAsync(CancellationToken ct = default) =>
            await _db.SaveChangesAsync(ct);
    }
}
