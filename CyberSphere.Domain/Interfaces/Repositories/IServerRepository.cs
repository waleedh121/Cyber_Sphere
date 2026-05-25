using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CyberSphere.Domain.Entities;
using CyberSphere.Domain.Enums;

namespace CyberSphere.Domain.Interfaces.Repositories
{

    public interface IServerRepository
    {
        Task<Server?> GetByIdAsync(Guid id, CancellationToken ct = default);

        /// <summary>
        /// Returns all Online servers of a given type that still have capacity,
        /// ordered by available slots descending (most available first).
        /// Used by VmAssignmentService to find the best candidate.
        /// </summary>
        Task<IReadOnlyList<Server>> GetAvailableByTypeAsync(ServerType type, CancellationToken ct = default);

        Task<IReadOnlyList<Server>> GetAllAsync(CancellationToken ct = default);
        Task<bool> ExistsByIpAndTypeAsync(string ipAddress, ServerType type, CancellationToken ct = default);

        Task AddAsync(Server server, CancellationToken ct = default);
        void Update(Server server);
        Task SaveChangesAsync(CancellationToken ct = default);
    }
}
