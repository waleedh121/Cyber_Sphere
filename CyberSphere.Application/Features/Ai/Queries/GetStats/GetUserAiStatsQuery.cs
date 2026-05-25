using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CyberSphere.Application.Features.Ai.DTOs;
using CyberSphere.Domain.Interfaces.Repositories;
using MediatR;

namespace CyberSphere.Application.Features.Ai.Queries.GetStats
{

    // ── Query ─────────────────────────────────────────────────────────────────────

    public sealed record GetUserAiStatsQuery : IRequest<UserAiStatsResponse>;

    // ── Handler ───────────────────────────────────────────────────────────────────

    public sealed class GetUserAiStatsQueryHandler
        : IRequestHandler<GetUserAiStatsQuery, UserAiStatsResponse>
    {
        private readonly IUserAiStatsRepository _stats;
        private readonly ICurrentUserService _currentUser;

        public GetUserAiStatsQueryHandler(
            IUserAiStatsRepository stats,
            ICurrentUserService currentUser)
        {
            _stats = stats;
            _currentUser = currentUser;
        }

        public async Task<UserAiStatsResponse> Handle(GetUserAiStatsQuery _, CancellationToken ct)
        {
            // Return zeroed response if user has never used AI — no row exists yet
            var stats = await _stats.GetByUserIdAsync(_currentUser.UserId, ct);

            if (stats is null)
                return new UserAiStatsResponse(
                    TotalRequests: 0,
                    TotalTokensUsed: 0,
                    TotalSessions: 0,
                    LastActivityAt: null);

            return new UserAiStatsResponse(
                TotalRequests: stats.TotalRequests,
                TotalTokensUsed: stats.TotalTokensUsed,
                TotalSessions: stats.TotalSessions,
                LastActivityAt: stats.LastActivityAt);
        }
    }
}
