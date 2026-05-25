using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CyberSphere.Application.Exceptions;
using CyberSphere.Application.Features.Tools.DTOs;
using CyberSphere.Domain.Entities;
using CyberSphere.Domain.Enums;
using CyberSphere.Domain.Interfaces.Repositories;
using CyberSphere.Domain.Interfaces.Services;
using FluentValidation;
using MediatR;

namespace CyberSphere.Application.Features.Admin.Commands.ReviewTool
{
    // ── Command ───────────────────────────────────────────────────────────────────

    public sealed record ReviewToolCommand(
        Guid ToolId,
        ReviewDecision Decision,
        string? Notes
    ) : IRequest<ReviewDecisionResponse>;

    // ── Validator ─────────────────────────────────────────────────────────────────

    public sealed class ReviewToolCommandValidator : AbstractValidator<ReviewToolCommand>
    {
        public ReviewToolCommandValidator()
        {
            RuleFor(x => x.ToolId)
                .NotEmpty().WithMessage("ToolId is required.");

            RuleFor(x => x.Notes)
                .MaximumLength(1000).WithMessage("Review notes must not exceed 1000 characters.")
                .When(x => x.Notes is not null);

            // Notes are mandatory when rejecting — the owner needs to know why
            RuleFor(x => x.Notes)
                .NotEmpty().WithMessage("Rejection notes are required — the contributor must know why their tool was rejected.")
                .When(x => x.Decision == ReviewDecision.Rejected);
        }
    }

    // ── Handler ───────────────────────────────────────────────────────────────────

    public sealed class ReviewToolCommandHandler : IRequestHandler<ReviewToolCommand, ReviewDecisionResponse>
    {
        private readonly IToolRepository _tools;
        private readonly IToolReviewRepository _reviews;
        private readonly IUserRepository _users;
        private readonly IEmailService _email;
        private readonly ICurrentUserService _currentUser;

        public ReviewToolCommandHandler(
            IToolRepository tools,
            IToolReviewRepository reviews,
            IUserRepository users,
            IEmailService email,
            ICurrentUserService currentUser)
        {
            _tools = tools;
            _reviews = reviews;
            _users = users;
            _email = email;
            _currentUser = currentUser;
        }

        public async Task<ReviewDecisionResponse> Handle(ReviewToolCommand cmd, CancellationToken ct)
        {
            // Load tool with owner navigation — needed to update TotalToolsUploaded
            var tool = await _tools.GetByIdWithOwnerAsync(cmd.ToolId, ct)
                ?? throw new NotFoundException(nameof(Tool), cmd.ToolId);

            // Only Pending tools can be reviewed
            if (tool.Status != ToolStatus.Pending)
                throw new BadRequestException(
                    $"Tool is '{tool.Status}' — only Pending tools can be reviewed. " +
                    "If resubmitted after rejection, it reverts to Pending automatically.");

            var owner = await _users.GetByIdAsync(tool.OwnerId, ct)
                ?? throw new NotFoundException(nameof(User), tool.OwnerId);

            // ── Apply decision ────────────────────────────────────────────────────

            if (cmd.Decision == ReviewDecision.Approved)
            {
                tool.Approve();

                // ★ Core requirement: increment owner's counter on approval
                owner.IncrementToolsUploaded();
            }
            else
            {
                tool.Reject();
                // TotalToolsUploaded is NOT incremented — the tool is not yet published
            }

            // ── Audit record ──────────────────────────────────────────────────────

            var review = ToolReview.Create(
                toolId: tool.Id,
                adminId: _currentUser.UserId,
                decision: cmd.Decision,
                notes: cmd.Notes);

            await _reviews.AddAsync(review, ct);

            // Persist tool status change + owner counter + review record in one transaction
            _tools.Update(tool);
            await _tools.SaveChangesAsync(ct);

            // ── Notifications (fire-and-forget — must not fail the request) ───────

            if (cmd.Decision == ReviewDecision.Approved)
                _ = _email.SendToolApprovedEmailAsync(owner.Email, owner.UserName, tool.Name, ct);
            else
                _ = _email.SendToolRejectedEmailAsync(owner.Email, owner.UserName, tool.Name, cmd.Notes, ct);

            return new ReviewDecisionResponse(
                ToolId: tool.Id,
                ToolName: tool.Name,
                Decision: cmd.Decision.ToString(),
                Notes: cmd.Notes,
                ReviewedAt: review.ReviewedAt
            );
        }
    }
}
