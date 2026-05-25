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

    public sealed class ServerRepository : IServerRepository
    {
        private readonly AppDbContext _db;

        public ServerRepository(AppDbContext db) => _db = db;

        public async Task<Server?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
            await _db.Servers.FindAsync([id], ct);

        /// <summary>
        /// Returns Online servers of the given type with available capacity,
        /// sorted by available slots descending (most headroom first).
        /// VmAssignmentService picks servers[0] from this list.
        /// </summary>
        public async Task<IReadOnlyList<Server>> GetAvailableByTypeAsync(
            ServerType type, CancellationToken ct = default)
        {
            var servers = await _db.Servers
                .Where(s => s.Type == type
                         && s.Status == ServerStatus.Online
                         && s.ActiveSessions < s.MaxSessions)
                .OrderByDescending(s => s.MaxSessions - s.ActiveSessions)
                .ToListAsync(ct);    // tracked — VmAssignmentService will call ReserveSlot()

            return servers.AsReadOnly();
        }

        public async Task<IReadOnlyList<Server>> GetAllAsync(CancellationToken ct = default)
        {
            var servers = await _db.Servers
                .OrderBy(s => s.Type)
                .ThenBy(s => s.Name)
                .AsNoTracking()
                .ToListAsync(ct);

            return servers.AsReadOnly();
        }

        public async Task<bool> ExistsByIpAndTypeAsync(
            string ipAddress, ServerType type, CancellationToken ct = default) =>
            await _db.Servers.AnyAsync(
                s => s.IpAddress == ipAddress && s.Type == type, ct);

        public async Task AddAsync(Server server, CancellationToken ct = default) =>
            await _db.Servers.AddAsync(server, ct);

        public void Update(Server server) =>
            _db.Servers.Update(server);

        public async Task SaveChangesAsync(CancellationToken ct = default) =>
            await _db.SaveChangesAsync(ct);
    }

}
