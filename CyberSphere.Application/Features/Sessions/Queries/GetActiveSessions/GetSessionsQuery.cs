using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CyberSphere.Application.Features.Sessions.Commands.StartSession;
using CyberSphere.Domain.Interfaces.Repositories;
using CyberSphere.Application.Features.Sessions.DTOs;
using FluentValidation;
using MediatR;

namespace CyberSphere.Application.Features.Sessions.Queries.GetActiveSessions
{

    // ── Active sessions query ─────────────────────────────────────────────────────

    public sealed record GetSessionsQuery : IRequest<IReadOnlyList<SessionResponse>>;

    public sealed class GetActiveSessionsQueryHandler
        : IRequestHandler<GetSessionsQuery, IReadOnlyList<SessionResponse>>
    {
        private readonly ISessionRepository _sessions;
        private readonly ICurrentUserService _currentUser;

        public GetActiveSessionsQueryHandler(ISessionRepository sessions, ICurrentUserService currentUser)
        {
            _sessions = sessions;
            _currentUser = currentUser;
        }

        public async Task<IReadOnlyList<SessionResponse>> Handle(
            GetSessionsQuery _, CancellationToken ct)
        {
            var sessions = await _sessions.GetActiveByUserIdAsync(_currentUser.UserId, ct);

            return sessions
                .Select(s => StartSessionCommandHandler.MapToResponse(s, s.Server))
                .ToList()
                .AsReadOnly();
        }
    }

    // ── Session history query ─────────────────────────────────────────────────────

    public sealed record GetSessionHistoryQuery(
        int Page = 1,
        int PageSize = 20
    ) : IRequest<PagedSessionsResponse>;

    public sealed class GetSessionHistoryQueryValidator : AbstractValidator<GetSessionHistoryQuery>
    {
        public GetSessionHistoryQueryValidator()
        {
            RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 50);
        }
    }

    public sealed class GetSessionHistoryQueryHandler
        : IRequestHandler<GetSessionHistoryQuery, PagedSessionsResponse>
    {
        private readonly ISessionRepository _sessions;
        private readonly ICurrentUserService _currentUser;

        public GetSessionHistoryQueryHandler(
            ISessionRepository sessions,
            ICurrentUserService currentUser)
        {
            _sessions = sessions;
            _currentUser = currentUser;
        }

        public async Task<PagedSessionsResponse> Handle(
            GetSessionHistoryQuery query, CancellationToken ct)
        {
            var (items, totalCount) = await _sessions.GetHistoryByUserIdAsync(
                _currentUser.UserId, query.Page, query.PageSize, ct);

            var dtos = items
                .Select(s => new SessionSummaryResponse(
                    Id: s.Id,
                    Type: s.Type.ToString(),
                    Status: s.Status.ToString(),
                    StartedAt: s.StartedAt,
                    EndedAt: s.EndedAt,
                    TerminationReason: s.TerminationReason,
                    ServerName: s.Server.Name))
                .ToList()
                .AsReadOnly();

            var totalPages = (int)Math.Ceiling(totalCount / (double)query.PageSize);

            return new PagedSessionsResponse(
                Items: dtos,
                Page: query.Page,
                PageSize: query.PageSize,
                TotalCount: totalCount,
                TotalPages: totalPages,
                HasNextPage: query.Page < totalPages,
                HasPreviousPage: query.Page > 1);
        }
    }
}
