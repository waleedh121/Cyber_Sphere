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
using FluentValidation;
using MediatR;

namespace CyberSphere.Application.Features.Tools.Commands.DeleteTool
{
    // ── Command ───────────────────────────────────────────────────────────────────

    public sealed record DeleteToolCommand(Guid ToolId) : IRequest<MessageResponse>;

    // ── Validator ─────────────────────────────────────────────────────────────────

    public sealed class DeleteToolCommandValidator : AbstractValidator<DeleteToolCommand>
    {
        public DeleteToolCommandValidator()
        {
            RuleFor(x => x.ToolId)
                .NotEmpty().WithMessage("ToolId is required.");
        }
    }

    // ── Handler ───────────────────────────────────────────────────────────────────

    public sealed class DeleteToolCommandHandler : IRequestHandler<DeleteToolCommand, MessageResponse>
    {
        private readonly IToolRepository _tools;
        private readonly IUserRepository _users;
        private readonly ICurrentUserService _currentUser;

        public DeleteToolCommandHandler(
            IToolRepository tools,
            IUserRepository users,
            ICurrentUserService currentUser)
        {
            _tools = tools;
            _users = users;
            _currentUser = currentUser;
        }

        public async Task<MessageResponse> Handle(DeleteToolCommand cmd, CancellationToken ct)
        {
            var tool = await _tools.GetByIdAsync(cmd.ToolId, ct)
                ?? throw new NotFoundException(nameof(Tool), cmd.ToolId);

            var isAdmin = _currentUser.Role == Role.Admin.ToString();
            var isOwner = tool.OwnerId == _currentUser.UserId;

            if (!isOwner && !isAdmin)
                throw new ForbiddenException("You do not have permission to delete this tool.");

            // Approved tools may be deleted only by an admin
            if (tool.Status == ToolStatus.Approved && !isAdmin)
                throw new BadRequestException(
                    "Approved tools can only be deleted by an administrator.");

            _tools.Delete(tool);

            // If approved, decrement owner's counter on deletion
            if (tool.Status == ToolStatus.Approved)
            {
                var owner = await _users.GetByIdAsync(tool.OwnerId, ct);
                owner?.DecrementToolsUploaded();
            }

            await _tools.SaveChangesAsync(ct);

            return new MessageResponse($"Tool '{tool.Name}' has been deleted.");
        }
    }
}
