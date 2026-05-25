using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CyberSphere.Application.Exceptions;
using CyberSphere.Application.Features.Ratings.DTOs;
using CyberSphere.Domain.Entities;
using CyberSphere.Domain.Interfaces.Repositories;
using MediatR;

namespace CyberSphere.Application.Features.Tools.Queries.GetToolStats
{

    // ── Query ─────────────────────────────────────────────────────────────────────

    public sealed record GetToolStatsQuery(Guid ToolId) : IRequest<ToolStatsResponse>;

    // ── Handler ───────────────────────────────────────────────────────────────────

    public sealed class GetToolStatsQueryHandler : IRequestHandler<GetToolStatsQuery, ToolStatsResponse>
    {
        private readonly IToolRepository _tools;
        private readonly IRatingRepository _ratings;

        public GetToolStatsQueryHandler(IToolRepository tools, IRatingRepository ratings)
        {
            _tools = tools;
            _ratings = ratings;
        }

        public async Task<ToolStatsResponse> Handle(GetToolStatsQuery query, CancellationToken ct)
        {
            var tool = await _tools.GetByIdAsync(query.ToolId, ct)
                ?? throw new NotFoundException(nameof(Tool), query.ToolId);

            // Load all ratings for this tool to compute the per-star breakdown
            var (allRatings, _) = await _ratings.GetByToolIdAsync(
                query.ToolId, page: 1, pageSize: int.MaxValue, ct);

            var breakdown = new RatingBreakdown(
                OneStar: allRatings.Count(r => r.Stars == 1),
                TwoStars: allRatings.Count(r => r.Stars == 2),
                ThreeStars: allRatings.Count(r => r.Stars == 3),
                FourStars: allRatings.Count(r => r.Stars == 4),
                FiveStars: allRatings.Count(r => r.Stars == 5)
            );

            return new ToolStatsResponse(
                ToolId: tool.Id,
                ToolName: tool.Name,
                Status: tool.Status.ToString(),
                UsageCount: tool.UsageCount,
                AverageRating: Math.Round(tool.AverageRating, 2),
                RatingCount: tool.RatingCount,
                RatingBreakdown: breakdown
            );
        }
    }

}
