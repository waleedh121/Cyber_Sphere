using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CyberSphere.Application.Features.Marketplace.DTOs;
using CyberSphere.Domain.Enums;
using CyberSphere.Domain.Interfaces.Repositories;
using FluentValidation;
using MediatR;

namespace CyberSphere.Application.Features.Marketplace.Queries.GetTools
{
    // ── Query ─────────────────────────────────────────────────────────────────────

    public sealed record GetToolsQuery(
        string? Search,
        Guid? CategoryId,
        DifficultyLevel? DifficultyLevel,
        ToolSortBy SortBy = ToolSortBy.Newest,
        int Page = 1,
        int PageSize = 12
    ) : IRequest<PagedToolsResponse>;

    // ── Validator ─────────────────────────────────────────────────────────────────

    public sealed class GetToolsQueryValidator : AbstractValidator<GetToolsQuery>
    {
        public GetToolsQueryValidator()
        {
            RuleFor(x => x.Search)
                .MaximumLength(100).WithMessage("Search term must not exceed 100 characters.")
                .When(x => x.Search is not null);

            RuleFor(x => x.Page)
                .GreaterThanOrEqualTo(1).WithMessage("Page must be at least 1.");

            RuleFor(x => x.PageSize)
                .InclusiveBetween(1, 50).WithMessage("Page size must be between 1 and 50.");
        }
    }

    // ── Handler ───────────────────────────────────────────────────────────────────

    public sealed class GetToolsQueryHandler : IRequestHandler<GetToolsQuery, PagedToolsResponse>
    {
        private readonly IToolRepository _tools;

        public GetToolsQueryHandler(IToolRepository tools) => _tools = tools;

        public async Task<PagedToolsResponse> Handle(GetToolsQuery query, CancellationToken ct)
        {
            var filter = new ToolFilterParams
            {
                Search = query.Search?.Trim(),
                CategoryId = query.CategoryId,
                DifficultyLevel = query.DifficultyLevel,
                SortBy = query.SortBy,
                Page = query.Page,
                PageSize = query.PageSize
            };

            var (items, totalCount) = await _tools.GetPagedAsync(filter, ct);

            var dtos = items.Select(MapToMarketplaceDto).ToList().AsReadOnly();

            var totalPages = (int)Math.Ceiling(totalCount / (double)filter.PageSize);

            return new PagedToolsResponse(
                Items: dtos,
                Page: filter.Page,
                PageSize: filter.PageSize,
                TotalCount: totalCount,
                TotalPages: totalPages,
                HasPreviousPage: filter.Page > 1,
                HasNextPage: filter.Page < totalPages
            );
        }

        // ── Mapping ───────────────────────────────────────────────────────────────

        internal static ToolMarketplaceDto MapToMarketplaceDto(Domain.Entities.Tool tool) =>
            new(
                Id: tool.Id,
                Name: tool.Name,
                Description: tool.Description,
                CategoryName: tool.Category.Name,
                DifficultyLevel: tool.DifficultyLevel.ToString(),
                UsageCount: tool.UsageCount,
                AverageRating: Math.Round(tool.AverageRating, 2),
                RatingCount: tool.RatingCount,
                Tags: tool.GetTags(),
                GitHubUrl: tool.GitHubUrl,
                CreatedAt: tool.CreatedAt,
                Owner: MapToOwnerDto(tool.Owner)
            );

        internal static OwnerProfileDto MapToOwnerDto(Domain.Entities.User owner) =>
            new(
                UserName: owner.UserName,
                Bio: owner.Bio,
                ProfilePictureUrl: owner.ProfilePictureUrl,
                TotalToolsUploaded: owner.TotalToolsUploaded,
                GitHubUrl: owner.GitHubUrl
            );
    }
}
