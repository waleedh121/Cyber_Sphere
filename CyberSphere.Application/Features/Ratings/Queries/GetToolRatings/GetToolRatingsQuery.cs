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

namespace CyberSphere.Application.Features.Ratings.Queries.GetToolRatings
{

    // ═══════════════════════════════════════════════════════════════════════════════
    // Get paged ratings for a tool (public)
    // ═══════════════════════════════════════════════════════════════════════════════

    public sealed record GetToolRatingsQuery(
        Guid ToolId,
        int Page = 1,
        int PageSize = 10
    ) : IRequest<PagedRatingsResponse>;

    public sealed class GetToolRatingsQueryValidator : AbstractValidator<GetToolRatingsQuery>
    {
        public GetToolRatingsQueryValidator()
        {
            RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 50);
        }
    }

    public sealed class GetToolRatingsQueryHandler
        : IRequestHandler<GetToolRatingsQuery, PagedRatingsResponse>
    {
        private readonly IRatingRepository _ratings;
        private readonly IToolRepository _tools;

        public GetToolRatingsQueryHandler(IRatingRepository ratings, IToolRepository tools)
        {
            _ratings = ratings;
            _tools = tools;
        }

        public async Task<PagedRatingsResponse> Handle(GetToolRatingsQuery query, CancellationToken ct)
        {
            var tool = await _tools.GetByIdAsync(query.ToolId, ct)
                ?? throw new NotFoundException(nameof(Tool), query.ToolId);

            var (items, totalCount) =
                await _ratings.GetByToolIdAsync(query.ToolId, query.Page, query.PageSize, ct);

            var dtos = items
                .Select(r => RateToolCommandHandler.MapToResponse(r, tool.Name, r.User.UserName))
                .ToList()
                .AsReadOnly();

            var totalPages = (int)Math.Ceiling(totalCount / (double)query.PageSize);

            return new PagedRatingsResponse(
                Items: dtos,
                Page: query.Page,
                PageSize: query.PageSize,
                TotalCount: totalCount,
                TotalPages: totalPages,
                HasNextPage: query.Page < totalPages,
                HasPreviousPage: query.Page > 1);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    // Get the current user's rating for a specific tool (returns null if not rated)
    // ═══════════════════════════════════════════════════════════════════════════════

    public sealed record GetMyRatingQuery(Guid ToolId) : IRequest<RatingResponse?>;

    public sealed class GetMyRatingQueryHandler : IRequestHandler<GetMyRatingQuery, RatingResponse?>
    {
        private readonly IRatingRepository _ratings;
        private readonly IToolRepository _tools;
        private readonly ICurrentUserService _currentUser;

        public GetMyRatingQueryHandler(
            IRatingRepository ratings,
            IToolRepository tools,
            ICurrentUserService currentUser)
        {
            _ratings = ratings;
            _tools = tools;
            _currentUser = currentUser;
        }

        public async Task<RatingResponse?> Handle(GetMyRatingQuery query, CancellationToken ct)
        {
            var rating = await _ratings.GetByUserAndToolAsync(_currentUser.UserId, query.ToolId, ct);

            if (rating is null) return null;

            var tool = await _tools.GetByIdAsync(rating.ToolId, ct);

            return RateToolCommandHandler.MapToResponse(
                rating,
                tool?.Name ?? string.Empty,
                _currentUser.UserName);
        }
    }
}
