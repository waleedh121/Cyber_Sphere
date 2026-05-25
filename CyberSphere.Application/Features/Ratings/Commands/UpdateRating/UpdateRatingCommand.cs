using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CyberSphere.Application.Exceptions;
using CyberSphere.Application.Features.Ratings.Commands.RateTool;
using CyberSphere.Application.Features.Ratings.DTOs;
using CyberSphere.Domain.Entities;
using CyberSphere.Domain.Interfaces.Repositories;
using FluentValidation;
using MediatR;

namespace CyberSphere.Application.Features.Ratings.Commands.UpdateRating
{
    // ── Command ───────────────────────────────────────────────────────────────────

    public sealed record UpdateRatingCommand(
        Guid RatingId,
        int Stars,
        string? Comment
    ) : IRequest<RatingResponse>;

    // ── Validator ─────────────────────────────────────────────────────────────────

    public sealed class UpdateRatingCommandValidator : AbstractValidator<UpdateRatingCommand>
    {
        public UpdateRatingCommandValidator()
        {
            RuleFor(x => x.RatingId)
                .NotEmpty().WithMessage("RatingId is required.");

            RuleFor(x => x.Stars)
                .InclusiveBetween(1, 5).WithMessage("Stars must be between 1 and 5.");

            RuleFor(x => x.Comment)
                .MaximumLength(1000).WithMessage("Comment must not exceed 1000 characters.")
                .When(x => x.Comment is not null);
        }
    }

    // ── Handler ───────────────────────────────────────────────────────────────────

    public sealed class UpdateRatingCommandHandler : IRequestHandler<UpdateRatingCommand, RatingResponse>
    {
        private readonly IRatingRepository _ratings;
        private readonly IToolRepository _tools;
        private readonly ICurrentUserService _currentUser;

        public UpdateRatingCommandHandler(
            IRatingRepository ratings,
            IToolRepository tools,
            ICurrentUserService currentUser)
        {
            _ratings = ratings;
            _tools = tools;
            _currentUser = currentUser;
        }

        public async Task<RatingResponse> Handle(UpdateRatingCommand cmd, CancellationToken ct)
        {
            var rating = await _ratings.GetByIdAsync(cmd.RatingId, ct)
                ?? throw new NotFoundException(nameof(Rating), cmd.RatingId);

            // Only the rating author can update it
            if (rating.UserId != _currentUser.UserId)
                throw new ForbiddenException("You can only update your own ratings.");

            var tool = await _tools.GetByIdAsync(rating.ToolId, ct)
                ?? throw new NotFoundException(nameof(Tool), rating.ToolId);

            // Capture old stars before mutating — needed by Tool.UpdateRating
            var oldStars = rating.Update(cmd.Stars, cmd.Comment);

            // Re-sync Tool's denormalized average with the corrected value
            tool.UpdateRating(oldStars, cmd.Stars);

            _ratings.Update(rating);
            _tools.Update(tool);
            await _ratings.SaveChangesAsync(ct);

            return RateToolCommandHandler.MapToResponse(rating, tool.Name, _currentUser.UserName);
        }
    }
}
