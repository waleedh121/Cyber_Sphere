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
    public sealed class AiSessionRepository : IAiSessionRepository
    {
        private readonly AppDbContext _db;

        public AiSessionRepository(AppDbContext db) => _db = db;

        public async Task<AiSession?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
            await _db.AiSessions.FindAsync([id], ct);

        public async Task<AiSession?> GetByIdWithMessagesAsync(Guid id, CancellationToken ct = default) =>
            await _db.AiSessions
                .Include(s => s.Messages)
                .FirstOrDefaultAsync(s => s.Id == id, ct);

        public async Task<IReadOnlyList<AiSession>> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
        {
            // Load session list with message count; don't load full message content for list view
            var sessions = await _db.AiSessions
                .Include(s => s.Messages)
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.StartedAt)
                .AsNoTracking()
                .ToListAsync(ct);

            return sessions.AsReadOnly();
        }

        public async Task<bool> IsOwnedByAsync(Guid sessionId, Guid userId, CancellationToken ct = default) =>
            await _db.AiSessions.AnyAsync(s => s.Id == sessionId && s.UserId == userId, ct);

        public async Task AddAsync(AiSession session, CancellationToken ct = default) =>
            await _db.AiSessions.AddAsync(session, ct);

        public void Update(AiSession session) =>
            _db.AiSessions.Update(session);

        public async Task AddMessageAsync(AiMessage message, CancellationToken ct = default) =>
             await _db.Set<AiMessage>().AddAsync(message, ct);

        public async Task SaveChangesAsync(CancellationToken ct = default) =>
            await _db.SaveChangesAsync(ct);

    }
}
