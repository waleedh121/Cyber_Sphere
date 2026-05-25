using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CyberSphere.Application.Exceptions;
using CyberSphere.Domain.Entities;
using CyberSphere.Domain.Interfaces.Repositories;
using CyberSphere.Application.Features.Auth.DTOs;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using static System.Collections.Specialized.BitVector32;

namespace CyberSphere.Application.Features.Admin.Commands.TerminateSession
{
    // ═══════════════════════════════════════════════════════════════════════════════
    // Admin: Force-terminate any active session
    // ═══════════════════════════════════════════════════════════════════════════════

    public sealed record AdminTerminateSessionCommand(
        Guid SessionId,
        string? Reason
    ) : IRequest<MessageResponse>;

    public sealed class AdminTerminateSessionCommandValidator
        : AbstractValidator<AdminTerminateSessionCommand>
    {
        public AdminTerminateSessionCommandValidator()
        {
            RuleFor(x => x.SessionId)
                .NotEmpty().WithMessage("SessionId is required.");

            RuleFor(x => x.Reason)
                .MaximumLength(500).WithMessage("Termination reason must not exceed 500 characters.")
                .When(x => x.Reason is not null);
        }
    }

    public sealed class AdminTerminateSessionCommandHandler
        : IRequestHandler<AdminTerminateSessionCommand, MessageResponse>
    {
        private readonly ISessionRepository _sessions;
        private readonly IServerRepository _servers;
        private readonly IVmService _vmService;
        private readonly ILogger<AdminTerminateSessionCommandHandler> _logger;

        public AdminTerminateSessionCommandHandler(
            ISessionRepository sessions,
            IServerRepository servers,
            IVmService vmService,
            ILogger<AdminTerminateSessionCommandHandler> logger)
        {
            _sessions = sessions;
            _servers = servers;
            _vmService = vmService;
            _logger = logger;
        }
        public async Task<MessageResponse> Handle(
            AdminTerminateSessionCommand cmd, CancellationToken ct)
        {
            var session = await _sessions.GetByIdWithDetailsAsync(cmd.SessionId, ct)
                ?? throw new NotFoundException(nameof(Session), cmd.SessionId);

            if (!session.IsActive)
                throw new BadRequestException(
                    $"Session '{cmd.SessionId}' is already '{session.Status}'.");

            var reason = cmd.Reason ?? "Terminated by administrator.";
            var server = session.Server;

            // SSH teardown — best-effort; admin termination proceeds regardless
            try
            {
                await _vmService.TerminateSessionAsync(
                    session.Id, server.IpAddress, server.SshPort, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "SSH teardown for admin-terminated session {SessionId} failed (non-fatal)",
                    session.Id);
            }

            session.Terminate(reason);
            server.ReleaseSlot();

            _servers.Update(server);
            _sessions.Update(session);
            await _sessions.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Admin terminated session {SessionId}. Reason: {Reason}", session.Id, reason);

            return new MessageResponse(
                $"Session '{session.Id}' has been terminated. Reason: {reason}");
        }
    }
}
