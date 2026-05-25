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

    public sealed class UserAiStatsRepository : IUserAiStatsRepository
    {
        private readonly AppDbContext _db;

        public UserAiStatsRepository(AppDbContext db) => _db = db;

        public async Task<UserAiStats?> GetByUserIdAsync(Guid userId, CancellationToken ct = default) =>
            await _db.UserAiStats
                .FirstOrDefaultAsync(s => s.UserId == userId, ct);

        public async Task AddAsync(UserAiStats stats, CancellationToken ct = default) =>
            await _db.UserAiStats.AddAsync(stats, ct);

        public void Update(UserAiStats stats) =>
            _db.UserAiStats.Update(stats);

        public async Task SaveChangesAsync(CancellationToken ct = default) =>
            await _db.SaveChangesAsync(ct);
    }
}
