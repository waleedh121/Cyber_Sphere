using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CyberSphere.Application.Exceptions;
using CyberSphere.Application.Features.Sessions.Services;
using CyberSphere.Application.Features.Sessions.DTOs;
using CyberSphere.Domain.Interfaces.Repositories;
using FluentValidation;
using CyberSphere.Domain.Enums;
using CyberSphere.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;
using static System.Collections.Specialized.BitVector32;

namespace CyberSphere.Application.Features.Sessions.Commands.StartSession
{

    // ── Command ───────────────────────────────────────────────────────────────────

    public sealed record StartSessionCommand(SessionType Type) : IRequest<SessionResponse>;

    // ── Validator ─────────────────────────────────────────────────────────────────

    public sealed class StartSessionCommandValidator : AbstractValidator<StartSessionCommand>
    {
        private static readonly SessionType[] ValidTypes =
            [SessionType.Scanning, SessionType.CliTools, SessionType.Sandbox];

        public StartSessionCommandValidator()
        {
            RuleFor(x => x.Type)
                .Must(t => ValidTypes.Contains(t))
                .WithMessage($"Session type must be one of: {string.Join(", ", ValidTypes.Select(t => $"{t} ({(int)t})"))}.");
        }
    }

    // ── Handler ───────────────────────────────────────────────────────────────────

    public sealed class StartSessionCommandHandler : IRequestHandler<StartSessionCommand, SessionResponse>
    {
        private readonly ISessionRepository _sessions;
        private readonly IServerRepository _servers;
        private readonly IVmService _vmService;
        private readonly ICurrentUserService _currentUser;
        private readonly VmAssignmentService _assignment;
        private readonly ILogger<StartSessionCommandHandler> _logger;

        public StartSessionCommandHandler(
            ISessionRepository sessions,
            IServerRepository servers,
            IVmService vmService,
            ICurrentUserService currentUser,
            VmAssignmentService assignment,
            ILogger<StartSessionCommandHandler> logger)
        {
            _sessions = sessions;
            _servers = servers;
            _vmService = vmService;
            _currentUser = currentUser;
            _assignment = assignment;
            _logger = logger;
        }

        public async Task<SessionResponse> Handle(StartSessionCommand cmd, CancellationToken ct)
        {
            var userId = _currentUser.UserId;

            // ── Guard: one active session per type ────────────────────────────────
            if (await _sessions.HasActiveSessionOfTypeAsync(userId, cmd.Type, ct))
                throw new ConflictException(
                    $"You already have an active {cmd.Type} session. " +
                    "End the existing session before starting a new one.");

            // ── VM assignment (application-layer routing logic) ───────────────────
            var server = await _assignment.AssignServerAsync(cmd.Type, ct);
            _logger.LogInformation(
                "Assigned server {ServerName} ({ServerId}) to user {UserId} for {SessionType} session",
                server.Name, server.Id, userId, cmd.Type);

            // ── Create session record ─────────────────────────────────────────────
            var session = Session.Create(userId, server.Id, cmd.Type);
            await _sessions.AddAsync(session, ct);

            // ── SSH health check before committing ────────────────────────────────
            var health = await _vmService.CheckHealthAsync(server.IpAddress, server.SshPort, ct);

            if (!health.IsHealthy)
            {
                _logger.LogWarning(
                    "Health check failed for server {ServerId}: {Error}",
                    server.Id, health.ErrorMessage);

                session.MarkFailed($"VM health check failed: {health.ErrorMessage}");
                server.ReleaseSlot();   // roll back the reserved slot

                _servers.Update(server);
                _sessions.Update(session);
                await _sessions.SaveChangesAsync(ct);

                throw new ServiceUnavailableException(
                    "The assigned VM failed its health check. " +
                    "A different server will be tried on your next request.");
            }

            // ── SSH initialise — environment setup + connection token ──────────────
            var connection = await _vmService.InitialiseSessionAsync(
                sessionId: session.Id,
                ipAddress: server.IpAddress,
                port: server.SshPort,
                userName: _currentUser.UserName,
                ct: ct);

            if (!connection.Success)
            {
                _logger.LogWarning(
                    "SSH initialisation failed for session {SessionId}: {Error}",
                    session.Id, connection.ErrorMessage);

                session.MarkFailed($"SSH initialisation failed: {connection.ErrorMessage}");
                server.ReleaseSlot();

                _servers.Update(server);
                _sessions.Update(session);
                await _sessions.SaveChangesAsync(ct);

                throw new ServiceUnavailableException(
                    "Could not establish a connection to the VM. Please try again.");
            }

            // ── Stamp session with connection info ────────────────────────────────
            session.SetConnectionInfo(
                connectionToken: connection.ConnectionToken!,
                vmIpAddress: server.IpAddress,
                vmPort: server.SshPort);

            server.RecordHealthCheck();
            _servers.Update(server);
            _sessions.Update(session);
            await _sessions.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Session {SessionId} started: user {UserId}, type {Type}, server {ServerName}",
                session.Id, userId, cmd.Type, server.Name);

            return MapToResponse(session, server);
        }

        // ── Mapping ───────────────────────────────────────────────────────────────

        internal static SessionResponse MapToResponse(Session s, Server srv) =>
            new(
                Id: s.Id,
                Type: s.Type.ToString(),
                Status: s.Status.ToString(),
                ConnectionToken: s.ConnectionToken,
                VmIpAddress: s.VmIpAddress,
                VmPort: s.VmPort,
                StartedAt: s.StartedAt,
                EndedAt: s.EndedAt,
                Server: new ServerInfo(srv.Id, srv.Name, srv.Type.ToString())
            );
    }
}
