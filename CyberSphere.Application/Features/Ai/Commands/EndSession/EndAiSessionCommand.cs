using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CyberSphere.Application.Exceptions;
using CyberSphere.Application.Features.Ai.Commands.StartSession;
using CyberSphere.Application.Features.Ai.DTOs;
using CyberSphere.Domain.Entities;
using CyberSphere.Domain.Interfaces.Repositories;
using MediatR;

namespace CyberSphere.Application.Features.Ai.Commands.EndSession
{

    // ── Command ───────────────────────────────────────────────────────────────────

    public sealed record EndAiSessionCommand(Guid SessionId) : IRequest<AiSessionResponse>;

    // ── Handler ───────────────────────────────────────────────────────────────────

    public sealed class EndAiSessionCommandHandler : IRequestHandler<EndAiSessionCommand, AiSessionResponse>
    {
        private readonly IAiSessionRepository _sessions;
        private readonly ICurrentUserService _currentUser;

        public EndAiSessionCommandHandler(
            IAiSessionRepository sessions,
            ICurrentUserService currentUser)
        {
            _sessions = sessions;
            _currentUser = currentUser;
        }

        public async Task<AiSessionResponse> Handle(EndAiSessionCommand cmd, CancellationToken ct)
        {
            var session = await _sessions.GetByIdAsync(cmd.SessionId, ct)
                ?? throw new NotFoundException(nameof(AiSession), cmd.SessionId);

            if (session.UserId != _currentUser.UserId)
                throw new ForbiddenException("You do not own this AI session.");

            session.End(); // idempotent — no-op if already ended

            _sessions.Update(session);
            await _sessions.SaveChangesAsync(ct);

            var messageCount = await _sessions.GetByIdWithMessagesAsync(cmd.SessionId, ct);

            return StartAiSessionCommandHandler.MapToResponse(
                session,
                messageCount: messageCount?.Messages.Count ?? 0);
        }
    }

}
