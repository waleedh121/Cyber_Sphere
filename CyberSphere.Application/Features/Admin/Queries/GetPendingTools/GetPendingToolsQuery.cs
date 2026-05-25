using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CyberSphere.Application.Features.Tools.DTOs;
using CyberSphere.Domain.Interfaces.Repositories;
using FluentValidation;
using MediatR;

namespace CyberSphere.Application.Features.Admin.Queries.GetPendingTools
{
    // ── Query ─────────────────────────────────────────────────────────────────────

    public sealed record GetPendingToolsQuery(
        int Page = 1,
        int PageSize = 20
    ) : IRequest<PagedPendingToolsResponse>;

    // ── Validator ─────────────────────────────────────────────────────────────────

    public sealed class GetPendingToolsQueryValidator : AbstractValidator<GetPendingToolsQuery>
    {
        public GetPendingToolsQueryValidator()
        {
            RuleFor(x => x.Page)
                .GreaterThanOrEqualTo(1).WithMessage("Page must be at least 1.");

            RuleFor(x => x.PageSize)
                .InclusiveBetween(1, 50).WithMessage("Page size must be between 1 and 50.");
        }
    }

    // ── Handler ───────────────────────────────────────────────────────────────────

    public sealed class GetPendingToolsQueryHandler
        : IRequestHandler<GetPendingToolsQuery, PagedPendingToolsResponse>
    {
        private readonly IToolRepository _tools;
        private readonly IToolReviewRepository _reviews;

        public GetPendingToolsQueryHandler(
            IToolRepository tools,
            IToolReviewRepository reviews)
        {
            _tools = tools;
            _reviews = reviews;
        }

        public async Task<PagedPendingToolsResponse> Handle(
            GetPendingToolsQuery query, CancellationToken ct)
        {
            var (items, totalCount) =
                await _tools.GetAllPendingAsync(query.Page, query.PageSize, ct);

            var toolIds = items.Select(t => t.Id).ToList();

            // Load the most recent review for each tool in one query
            var latestReviews = new Dictionary<Guid, ToolReviewHistoryEntry?>();
            foreach (var toolId in toolIds)
            {
                var latest = await _reviews.GetLatestByToolIdAsync(toolId, ct);
                latestReviews[toolId] = latest is null ? null : new ToolReviewHistoryEntry(
                    Decision: latest.Decision.ToString(),
                    Notes: latest.Notes,
                    AdminUserName: latest.Admin.UserName,
                    ReviewedAt: latest.ReviewedAt
                );
            }

            var dtos = items.Select(t => new PendingToolResponse(
                Id: t.Id,
                Name: t.Name,
                Description: t.Description,
                CategoryName: t.Category.Name,
                DifficultyLevel: t.DifficultyLevel.ToString(),
                GitHubUrl: t.GitHubUrl,
                Tags: t.GetTags(),
                CreatedAt: t.CreatedAt,
                Owner: new PendingToolOwnerInfo(
                    Id: t.Owner.Id,
                    UserName: t.Owner.UserName,
                    Email: t.Owner.Email,
                    TotalToolsUploaded: t.Owner.TotalToolsUploaded),
                LastReview: latestReviews.GetValueOrDefault(t.Id)
            )).ToList().AsReadOnly();

            var totalPages = (int)Math.Ceiling(totalCount / (double)query.PageSize);

            return new PagedPendingToolsResponse(
                Items: dtos,
                Page: query.Page,
                PageSize: query.PageSize,
                TotalCount: totalCount,
                TotalPages: totalPages,
                HasNextPage: query.Page < totalPages,
                HasPreviousPage: query.Page > 1
            );
        }
    }
}
