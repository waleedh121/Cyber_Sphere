using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CyberSphere.Domain.Entities;

namespace CyberSphere.Domain.Interfaces.Repositories
{
    public interface IAiSessionRepository
    {
        Task<AiSession?> GetByIdAsync(Guid id, CancellationToken ct = default);

        /// <summary>Loads AiSession with its Messages — for history view.</summary>
        Task<AiSession?> GetByIdWithMessagesAsync(Guid id, CancellationToken ct = default);

        Task<IReadOnlyList<AiSession>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);

        /// <summary>Checks ownership — handler uses this before loading full entity.</summary>
        Task<bool> IsOwnedByAsync(Guid sessionId, Guid userId, CancellationToken ct = default);

        Task AddAsync(AiSession session, CancellationToken ct = default);
        void Update(AiSession session);
        Task AddMessageAsync(AiMessage message, CancellationToken ct = default);
        Task SaveChangesAsync(CancellationToken ct = default);
    }

}
