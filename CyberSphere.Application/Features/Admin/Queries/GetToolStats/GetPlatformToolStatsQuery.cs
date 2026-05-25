using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CyberSphere.Application.Features.Ratings.DTOs;
using CyberSphere.Domain.Enums;
using CyberSphere.Domain.Interfaces.Repositories;
using MediatR;

namespace CyberSphere.Application.Features.Admin.Queries.GetToolStats
{

    // ── Query ─────────────────────────────────────────────────────────────────────

    public sealed record GetPlatformToolStatsQuery : IRequest<PlatformToolStatsResponse>;

    // ── Handler ───────────────────────────────────────────────────────────────────

    public sealed class GetPlatformToolStatsQueryHandler
        : IRequestHandler<GetPlatformToolStatsQuery, PlatformToolStatsResponse>
    {
        private readonly IToolRepository _tools;

        public GetPlatformToolStatsQueryHandler(IToolRepository tools) => _tools = tools;

        public async Task<PlatformToolStatsResponse> Handle(
            GetPlatformToolStatsQuery _, CancellationToken ct)
        {
            var allTools = await _tools.GetAllForStatsAsync(ct);

            var approved = allTools.Where(t => t.Status == ToolStatus.Approved).ToList();
            var pending = allTools.Where(t => t.Status == ToolStatus.Pending).ToList();
            var rejected = allTools.Where(t => t.Status == ToolStatus.Rejected).ToList();

            var totalRatings = approved.Sum(t => t.RatingCount);
            var totalUsage = approved.Sum(t => t.UsageCount);
            var overallAverage = approved.Count > 0 && totalRatings > 0
                ? Math.Round(approved.Where(t => t.RatingCount > 0)
                                     .Average(t => t.AverageRating), 2)
                : 0m;

            var topRated = approved
                .Where(t => t.RatingCount >= 1)
                .OrderByDescending(t => t.AverageRating)
                .ThenByDescending(t => t.RatingCount)
                .Take(10)
                .Select(t => new TopRatedToolResponse(
                    t.Id, t.Name, t.Category.Name,
                    Math.Round(t.AverageRating, 2), t.RatingCount))
                .ToList()
                .AsReadOnly();

            var mostUsed = approved
                .OrderByDescending(t => t.UsageCount)
                .Take(10)
                .Select(t => new MostUsedToolResponse(
                    t.Id, t.Name, t.Category.Name, t.UsageCount))
                .ToList()
                .AsReadOnly();

            return new PlatformToolStatsResponse(
                TotalTools: allTools.Count,
                ApprovedTools: approved.Count,
                PendingTools: pending.Count,
                RejectedTools: rejected.Count,
                TotalRatings: totalRatings,
                TotalUsageCount: totalUsage,
                OverallAverageRating: overallAverage,
                TopRatedTools: topRated,
                MostUsedTools: mostUsed
            );
        }
    }

}
