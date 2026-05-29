using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CyberSphere.Domain.Entities;
using CyberSphere.Domain.Enums;

namespace CyberSphere.Domain.Interfaces.Repositories
{



    public interface ISessionRepository
    {
        Task<Session?> GetByIdAsync(Guid id, CancellationToken ct = default);

        /// <summary>Loads Session + Server + User navigation (full detail).</summary>
        Task<Session?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct = default);

        /// <summary>All active sessions for a user (should normally be 0 or 1 per type).</summary>
        Task<IReadOnlyList<Session>> GetActiveByUserIdAsync(Guid userId, CancellationToken ct = default);

        /// <summary>Full paginated history for a user across all session types.</summary>
        Task<(IReadOnlyList<Session> Items, int TotalCount)> GetHistoryByUserIdAsync(
            Guid userId, int page, int pageSize, CancellationToken ct = default);

        /// <summary>Admin: all active sessions platform-wide.</summary>
        Task<IReadOnlyList<Session>> GetAllActiveAsync(CancellationToken ct = default);

        /// <summary>Admin: paginated full history.</summary>
        Task<(IReadOnlyList<Session> Items, int TotalCount)> GetAllPagedAsync(
            int page, int pageSize, CancellationToken ct = default);

        /// <summary>All active sessions on a given server (used when taking a server offline).</summary>
        Task<IReadOnlyList<Session>> GetActiveByServerIdAsync(Guid serverId, CancellationToken ct = default);

        /// <summary>Checks whether the user already has an active session of this type.</summary>
        Task<bool> HasActiveSessionOfTypeAsync(Guid userId, SessionType type, CancellationToken ct = default);

        Task AddAsync(Session session, CancellationToken ct = default);
        void Update(Session session);

        Task<IReadOnlyList<Session>> GetAllForDashboardAsync(CancellationToken ct = default);
        Task SaveChangesAsync(CancellationToken ct = default);
    }
}
