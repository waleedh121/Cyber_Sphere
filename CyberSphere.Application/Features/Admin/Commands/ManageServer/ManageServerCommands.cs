using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CyberSphere.Application.Exceptions;
using CyberSphere.Application.Features.Auth.DTOs;
using CyberSphere.Application.Features.Sessions.DTOs;
using CyberSphere.Domain.Entities;
using CyberSphere.Domain.Enums;
using CyberSphere.Domain.Interfaces.Repositories;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CyberSphere.Application.Features.Admin.Commands.ManageServer
{

    // ═══════════════════════════════════════════════════════════════════════════════
    // Register a new VM server in the pool
    // ═══════════════════════════════════════════════════════════════════════════════

    public sealed record RegisterServerCommand(
        string Name,
        string IpAddress,
        ServerType Type,
        int MaxSessions,
        int SshPort
    ) : IRequest<ServerStatusResponse>;

    public sealed class RegisterServerCommandValidator : AbstractValidator<RegisterServerCommand>
    {
        public RegisterServerCommandValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().MaximumLength(100);

            RuleFor(x => x.IpAddress)
                .NotEmpty()
                .Matches(@"^(\d{1,3}\.){3}\d{1,3}$")
                .WithMessage("IpAddress must be a valid IPv4 address.");

            RuleFor(x => x.MaxSessions)
                .InclusiveBetween(1, 100).WithMessage("MaxSessions must be between 1 and 100.");

            RuleFor(x => x.SshPort)
                .InclusiveBetween(1, 65535).WithMessage("SshPort must be between 1 and 65535.");
        }
    }

    public sealed class RegisterServerCommandHandler
        : IRequestHandler<RegisterServerCommand, ServerStatusResponse>
    {
        private readonly IServerRepository _servers;

        public RegisterServerCommandHandler(IServerRepository servers) => _servers = servers;

        public async Task<ServerStatusResponse> Handle(RegisterServerCommand cmd, CancellationToken ct)
        {
            if (await _servers.ExistsByIpAndTypeAsync(cmd.IpAddress, cmd.Type, ct))
                throw new ConflictException(
                    $"A server of type '{cmd.Type}' at '{cmd.IpAddress}' is already registered.");

            var server = Server.Create(
                name: cmd.Name,
                ipAddress: cmd.IpAddress,
                type: cmd.Type,
                maxSessions: cmd.MaxSessions,
                sshPort: cmd.SshPort);

            await _servers.AddAsync(server, ct);
            await _servers.SaveChangesAsync(ct);

            return MapToStatusResponse(server);
        }

        internal static ServerStatusResponse MapToStatusResponse(Server s) =>
            new(s.Id, s.Name, s.IpAddress, s.SshPort, s.Type.ToString(), s.Status.ToString(),
                s.ActiveSessions, s.MaxSessions,
                Math.Max(0, s.MaxSessions - s.ActiveSessions),
                s.LastHealthCheckAt);
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    // Set server status (Online / Offline / Maintenance)
    // ═══════════════════════════════════════════════════════════════════════════════

    public sealed record SetServerStatusCommand(Guid ServerId, ServerStatus Status)
        : IRequest<ServerStatusResponse>;

    public sealed class SetServerStatusCommandHandler
        : IRequestHandler<SetServerStatusCommand, ServerStatusResponse>
    {
        private readonly IServerRepository _servers;

        public SetServerStatusCommandHandler(IServerRepository servers) => _servers = servers;

        public async Task<ServerStatusResponse> Handle(SetServerStatusCommand cmd, CancellationToken ct)
        {
            var server = await _servers.GetByIdAsync(cmd.ServerId, ct)
                ?? throw new NotFoundException(nameof(Server), cmd.ServerId);

            switch (cmd.Status)
            {
                case ServerStatus.Online: server.SetOnline(); break;
                case ServerStatus.Offline: server.SetOffline(); break;
                case ServerStatus.Maintenance: server.SetMaintenance(); break;
            }

            _servers.Update(server);
            await _servers.SaveChangesAsync(ct);

            return RegisterServerCommandHandler.MapToStatusResponse(server);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    // Health-check a specific server on demand
    // ═══════════════════════════════════════════════════════════════════════════════

    public sealed record HealthCheckServerCommand(Guid ServerId) : IRequest<MessageResponse>;

    public sealed class HealthCheckServerCommandHandler
        : IRequestHandler<HealthCheckServerCommand, MessageResponse>
    {
        private readonly IServerRepository _servers;
        private readonly IVmService _vmService;
        private readonly ILogger<HealthCheckServerCommandHandler> _logger;

        public HealthCheckServerCommandHandler(
            IServerRepository servers,
            IVmService vmService,
            ILogger<HealthCheckServerCommandHandler> logger)
        {
            _servers = servers;
            _vmService = vmService;
            _logger = logger;
        }

        public async Task<MessageResponse> Handle(HealthCheckServerCommand cmd, CancellationToken ct)
        {
            var server = await _servers.GetByIdAsync(cmd.ServerId, ct)
                ?? throw new NotFoundException(nameof(Server), cmd.ServerId);

            var result = await _vmService.CheckHealthAsync(server.IpAddress, server.SshPort, ct);

            server.RecordHealthCheck();

            if (!result.IsHealthy)
            {
                _logger.LogWarning(
                    "Health check failed for server {ServerId} ({Name}): {Error}",
                    server.Id, server.Name, result.ErrorMessage);

                server.SetOffline();
            }
            else
            {
                if (server.Status == ServerStatus.Offline)
                    server.SetOnline();   // auto-recover if it was offline
            }

            _servers.Update(server);
            await _servers.SaveChangesAsync(ct);

            return result.IsHealthy
                ? new MessageResponse($"Server '{server.Name}' is healthy.")
                : new MessageResponse($"Server '{server.Name}' failed health check: {result.ErrorMessage}. Status set to Offline.");
        }
    }
}
