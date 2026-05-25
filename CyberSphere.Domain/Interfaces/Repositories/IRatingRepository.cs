using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CyberSphere.Domain.Entities;

namespace CyberSphere.Domain.Interfaces.Repositories
{
    public interface IRatingRepository
    {
        /// <summary>Get a specific user's rating for a specific tool. Returns null if not rated.</summary>
        Task<Rating?> GetByUserAndToolAsync(Guid userId, Guid toolId, CancellationToken ct = default);

        /// <summary>Get a rating by its ID.</summary>
        Task<Rating?> GetByIdAsync(Guid id, CancellationToken ct = default);

        /// <summary>All ratings for a tool, newest first, paged.</summary>
        Task<(IReadOnlyList<Rating> Items, int TotalCount)> GetByToolIdAsync(
            Guid toolId, int page, int pageSize, CancellationToken ct = default);

        /// <summary>Has the specified user already rated this tool?</summary>
        Task<bool> ExistsAsync(Guid userId, Guid toolId, CancellationToken ct = default);

        Task AddAsync(Rating rating, CancellationToken ct = default);
        void Update(Rating rating);
        void Delete(Rating rating);
        Task SaveChangesAsync(CancellationToken ct = default);
    }
}
