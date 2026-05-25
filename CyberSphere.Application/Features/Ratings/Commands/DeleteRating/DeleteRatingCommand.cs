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

namespace CyberSphere.Application.Features.Ratings.Commands.DeleteRating
{

    // ── Command ───────────────────────────────────────────────────────────────────

    public sealed record DeleteRatingCommand(Guid RatingId) : IRequest<MessageResponse>;

    // ── Handler ───────────────────────────────────────────────────────────────────

    public sealed class DeleteRatingCommandHandler : IRequestHandler<DeleteRatingCommand, MessageResponse>
    {
        private readonly IRatingRepository _ratings;
        private readonly IToolRepository _tools;
        private readonly ICurrentUserService _currentUser;

        public DeleteRatingCommandHandler(
            IRatingRepository ratings,
            IToolRepository tools,
            ICurrentUserService currentUser)
        {
            _ratings = ratings;
            _tools = tools;
            _currentUser = currentUser;
        }

        public async Task<MessageResponse> Handle(DeleteRatingCommand cmd, CancellationToken ct)
        {
            var rating = await _ratings.GetByIdAsync(cmd.RatingId, ct)
                ?? throw new NotFoundException(nameof(Rating), cmd.RatingId);

            var isAdmin = _currentUser.Role == Role.Admin.ToString();
            var isAuthor = rating.UserId == _currentUser.UserId;

            if (!isAuthor && !isAdmin)
                throw new ForbiddenException("You can only delete your own ratings.");

            var tool = await _tools.GetByIdAsync(rating.ToolId, ct);

            // Correct the denormalized average on Tool before deleting the rating row
            if (tool is not null && tool.RatingCount > 0)
            {
                // Reverse the Welford contribution of this rating
                var newCount = tool.RatingCount - 1;
                var newAverage = newCount == 0
                    ? 0m
                    : ((tool.AverageRating * tool.RatingCount) - rating.Stars) / newCount;

                tool.RemoveRating(newAverage, newCount);
                _tools.Update(tool);
            }

            _ratings.Delete(rating);
            await _ratings.SaveChangesAsync(ct);

            return new MessageResponse("Rating deleted successfully.");
        }
    }
}
