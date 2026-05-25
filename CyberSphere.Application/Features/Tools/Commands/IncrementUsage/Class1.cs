using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CyberSphere.Application.Exceptions;
using CyberSphere.Application.Features.Auth.DTOs;
using CyberSphere.Domain.Entities;
using CyberSphere.Domain.Enums;
using CyberSphere.Domain.Interfaces.Repositories;
using MediatR;

namespace CyberSphere.Application.Features.Tools.Commands.IncrementUsage
{

    // ── Command ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// Increments the UsageCount on a Tool.
    /// Called internally when a user opens a CLI session containing a tool,
    /// or explicitly by a frontend action ("Use Tool").
    /// Only Approved tools track usage.
    /// </summary>
    public sealed record IncrementToolUsageCommand(Guid ToolId) : IRequest<MessageResponse>;

    // ── Handler ───────────────────────────────────────────────────────────────────

    public sealed class IncrementToolUsageCommandHandler
        : IRequestHandler<IncrementToolUsageCommand, MessageResponse>
    {
        private readonly IToolRepository _tools;

        public IncrementToolUsageCommandHandler(IToolRepository tools) => _tools = tools;

        public async Task<MessageResponse> Handle(IncrementToolUsageCommand cmd, CancellationToken ct)
        {
            var tool = await _tools.GetByIdAsync(cmd.ToolId, ct)
                ?? throw new NotFoundException(nameof(Tool), cmd.ToolId);

            if (tool.Status != ToolStatus.Approved)
                throw new BadRequestException(
                    "Usage can only be tracked for Approved tools.");

            tool.IncrementUsage();
            _tools.Update(tool);
            await _tools.SaveChangesAsync(ct);

            return new MessageResponse($"Usage count for '{tool.Name}' incremented.");
        }
    }
}
