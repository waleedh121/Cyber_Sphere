using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CyberSphere.Application.Exceptions;
using CyberSphere.Application.Features.Ai.Commands.SendMessage;
using CyberSphere.Application.Features.Ai.Commands.StartSession;
using CyberSphere.Application.Features.Ai.DTOs;
using CyberSphere.Domain.Entities;
using CyberSphere.Domain.Interfaces.Repositories;
using MediatR;

namespace CyberSphere.Application.Features.Ai.Queries.GetHistory
{

    // ── Query: all sessions (list view, no messages) ──────────────────────────────

    public sealed record GetAiSessionsQuery : IRequest<IReadOnlyList<AiSessionResponse>>;

    public sealed class GetAiSessionsQueryHandler
        : IRequestHandler<GetAiSessionsQuery, IReadOnlyList<AiSessionResponse>>
    {
        private readonly IAiSessionRepository _sessions;
        private readonly ICurrentUserService _currentUser;

        public GetAiSessionsQueryHandler(IAiSessionRepository sessions, ICurrentUserService currentUser)
        {
            _sessions = sessions;
            _currentUser = currentUser;
        }

        public async Task<IReadOnlyList<AiSessionResponse>> Handle(
            GetAiSessionsQuery _, CancellationToken ct)
        {
            var sessions = await _sessions.GetByUserIdAsync(_currentUser.UserId, ct);

            return sessions
                .Select(s => StartAiSessionCommandHandler.MapToResponse(s, s.Messages.Count))
                .ToList()
                .AsReadOnly();
        }
    }

    // ── Query: single session with full message history ───────────────────────────

    public sealed record GetAiSessionHistoryQuery(Guid SessionId)
        : IRequest<AiSessionWithHistoryResponse>;

    public sealed class GetAiSessionHistoryQueryHandler
        : IRequestHandler<GetAiSessionHistoryQuery, AiSessionWithHistoryResponse>
    {
        private readonly IAiSessionRepository _sessions;
        private readonly ICurrentUserService _currentUser;

        public GetAiSessionHistoryQueryHandler(
            IAiSessionRepository sessions,
            ICurrentUserService currentUser)
        {
            _sessions = sessions;
            _currentUser = currentUser;
        }

        public async Task<AiSessionWithHistoryResponse> Handle(
            GetAiSessionHistoryQuery query, CancellationToken ct)
        {
            var session = await _sessions.GetByIdWithMessagesAsync(query.SessionId, ct)
                ?? throw new NotFoundException(nameof(AiSession), query.SessionId);

            if (session.UserId != _currentUser.UserId)
                throw new ForbiddenException("You do not own this AI session.");

            var messages = session.Messages
                .OrderBy(m => m.CreatedAt)
                .Select(SendAiMessageCommandHandler.MapMessage)
                .ToList()
                .AsReadOnly();

            return new AiSessionWithHistoryResponse(
                Id: session.Id,
                Title: session.Title,
                Status: session.Status.ToString(),
                StartedAt: session.StartedAt,
                EndedAt: session.EndedAt,
                Messages: messages
            );
        }
    }
}
