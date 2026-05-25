using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CyberSphere.Application.Exceptions;
using CyberSphere.Application.Features.Ratings.DTOs;
using CyberSphere.Domain.Entities;
using CyberSphere.Domain.Enums;
using CyberSphere.Domain.Interfaces.Repositories;
using FluentValidation;
using MediatR;

namespace CyberSphere.Application.Features.Ratings.Commands.RateTool
{

    // ── Command ───────────────────────────────────────────────────────────────────

    public sealed record RateToolCommand(
        Guid ToolId,
        int Stars,
        string? Comment
    ) : IRequest<RatingResponse>;

    // ── Validator ─────────────────────────────────────────────────────────────────

    public sealed class RateToolCommandValidator : AbstractValidator<RateToolCommand>
    {
        public RateToolCommandValidator()
        {
            RuleFor(x => x.ToolId)
                .NotEmpty().WithMessage("ToolId is required.");

            RuleFor(x => x.Stars)
                .InclusiveBetween(1, 5).WithMessage("Stars must be between 1 and 5.");

            RuleFor(x => x.Comment)
                .MaximumLength(1000).WithMessage("Comment must not exceed 1000 characters.")
                .When(x => x.Comment is not null);
        }
    }

    // ── Handler ───────────────────────────────────────────────────────────────────

    public sealed class RateToolCommandHandler : IRequestHandler<RateToolCommand, RatingResponse>
    {
        private readonly IRatingRepository _ratings;
        private readonly IToolRepository _tools;
        private readonly ICurrentUserService _currentUser;

        public RateToolCommandHandler(
            IRatingRepository ratings,
            IToolRepository tools,
            ICurrentUserService currentUser)
        {
            _ratings = ratings;
            _tools = tools;
            _currentUser = currentUser;
        }

        public async Task<RatingResponse> Handle(RateToolCommand cmd, CancellationToken ct)
        {
            var tool = await _tools.GetByIdAsync(cmd.ToolId, ct)
                ?? throw new NotFoundException(nameof(Tool), cmd.ToolId);

            // Only approved tools can be rated
            if (tool.Status != ToolStatus.Approved)
                throw new BadRequestException(
                    "Only approved tools can be rated.");

            // Owners cannot rate their own tools
            if (tool.OwnerId == _currentUser.UserId)
                throw new BadRequestException(
                    "You cannot rate your own tool.");

            // One rating per user per tool
            if (await _ratings.ExistsAsync(_currentUser.UserId, cmd.ToolId, ct))
                throw new ConflictException(
                    "You have already rated this tool. Use the update endpoint to change your rating.");

            var rating = Rating.Create( cmd.ToolId, _currentUser.UserId, cmd.Stars, cmd.Comment);


            // Update denormalized stats on Tool using Welford incremental formula
            tool.AddRating(cmd.Stars);

            await _ratings.AddAsync(rating, ct);
            _tools.Update(tool);
            await _ratings.SaveChangesAsync(ct);

            return MapToResponse(rating, tool.Name, _currentUser.UserName);
        }

        internal static RatingResponse MapToResponse(Rating r, string toolName, string userName) =>
            new(r.Id, r.ToolId, toolName, r.Stars, r.Comment, userName, r.CreatedAt, r.UpdatedAt);
    }
}
