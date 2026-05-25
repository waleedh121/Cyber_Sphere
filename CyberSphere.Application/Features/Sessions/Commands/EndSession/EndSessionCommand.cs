using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CyberSphere.Application.Exceptions;
using CyberSphere.Application.Features.Sessions.Commands.StartSession;
using CyberSphere.Domain.Interfaces.Repositories;
using CyberSphere.Domain.Entities;
using CyberSphere.Application.Features.Sessions.DTOs;
using MediatR;
using Microsoft.Extensions.Logging;
using static System.Collections.Specialized.BitVector32;

namespace CyberSphere.Application.Features.Sessions.Commands.EndSession
{

    // ── Command ───────────────────────────────────────────────────────────────────

    public sealed record EndSessionCommand(Guid SessionId) : IRequest<SessionResponse>;

    // ── Handler ───────────────────────────────────────────────────────────────────

    public sealed class EndSessionCommandHandler : IRequestHandler<EndSessionCommand, SessionResponse>
    {
        private readonly ISessionRepository _sessions;
        private readonly IServerRepository _servers;
        private readonly IVmService _vmService;
        private readonly ICurrentUserService _currentUser;
        private readonly ILogger<EndSessionCommandHandler> _logger;

        public EndSessionCommandHandler(
            ISessionRepository sessions,
            IServerRepository servers,
            IVmService vmService,
            ICurrentUserService currentUser,
            ILogger<EndSessionCommandHandler> logger)
        {
            _sessions = sessions;
            _servers = servers;
            _vmService = vmService;
            _currentUser = currentUser;
            _logger = logger;
        }

        public async Task<SessionResponse> Handle(EndSessionCommand cmd, CancellationToken ct)
        {
            var session = await _sessions.GetByIdWithDetailsAsync(cmd.SessionId, ct)
                ?? throw new NotFoundException(nameof(Session), cmd.SessionId);

            if (session.UserId != _currentUser.UserId)
                throw new ForbiddenException("You do not own this session.");

            if (!session.IsActive)
                throw new BadRequestException(
                    $"Session is already '{session.Status}' — only Active sessions can be ended.");

            var server = session.Server;

            // ── SSH teardown (fire best-effort — must not block the End) ──────────
            try
            {
                await _vmService.TerminateSessionAsync(
                    session.Id, server.IpAddress, server.SshPort, ct);
            }
            catch (Exception ex)
            {
                // Log but continue — the DB record must be closed regardless of SSH outcome
                _logger.LogWarning(ex,
                    "SSH teardown for session {SessionId} failed (non-fatal) — proceeding with End",
                    session.Id);
            }

            session.End();
            server.ReleaseSlot();

            _servers.Update(server);
            _sessions.Update(session);
            await _sessions.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Session {SessionId} ended by user {UserId}", session.Id, _currentUser.UserId);

            return StartSessionCommandHandler.MapToResponse(session, server);
        }
    }
}
