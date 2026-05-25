using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CyberSphere.Domain.Entities;

namespace CyberSphere.Domain.Interfaces.Repositories
{
    public interface IToolReviewRepository
    {
        /// <summary>Full review history for a tool, newest first.</summary>
        Task<IReadOnlyList<ToolReview>> GetByToolIdAsync(Guid toolId, CancellationToken ct = default);

        /// <summary>Most recent review record for a tool (for display on detail page).</summary>
        Task<ToolReview?> GetLatestByToolIdAsync(Guid toolId, CancellationToken ct = default);

        Task AddAsync(ToolReview review, CancellationToken ct = default);
        Task SaveChangesAsync(CancellationToken ct = default);
    }
}
