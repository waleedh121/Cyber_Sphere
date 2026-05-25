using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CyberSphere.Domain.Entities;

namespace CyberSphere.Domain.Interfaces.Repositories
{
    public interface IUserAiStatsRepository
    {
        /// <summary>
        /// Returns existing stats row or null if the user has never used AI.
        /// Handlers create the row lazily on first use via CreateForUser().
        /// </summary>
        Task<UserAiStats?> GetByUserIdAsync(Guid userId, CancellationToken ct = default);

        Task AddAsync(UserAiStats stats, CancellationToken ct = default);
        void Update(UserAiStats stats);
        Task SaveChangesAsync(CancellationToken ct = default);
    }

}
