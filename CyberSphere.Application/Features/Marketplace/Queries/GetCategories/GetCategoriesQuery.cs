using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CyberSphere.Application.Features.Marketplace.DTOs;
using CyberSphere.Domain.Interfaces.Repositories;
using MediatR;

namespace CyberSphere.Application.Features.Marketplace.Queries.GetCategories
{

    // ── Query ─────────────────────────────────────────────────────────────────────

    public sealed record GetCategoriesQuery : IRequest<IReadOnlyList<CategoryDto>>;

    // ── Handler ───────────────────────────────────────────────────────────────────

    public sealed class GetCategoriesQueryHandler
        : IRequestHandler<GetCategoriesQuery, IReadOnlyList<CategoryDto>>
    {
        private readonly ICategoryRepository _categories;

        public GetCategoriesQueryHandler(ICategoryRepository categories) => _categories = categories;

        public async Task<IReadOnlyList<CategoryDto>> Handle(GetCategoriesQuery _, CancellationToken ct)
        {
            var categories = await _categories.GetAllAsync(ct);

            return categories
                .Select(c => new CategoryDto(
                    Id: c.Id,
                    Name: c.Name,
                    Slug: c.Slug,
                    Description: c.Description,
                    ToolCount: c.Tools.Count(t => t.Status == Domain.Enums.ToolStatus.Approved)))
                .OrderBy(c => c.Name)
                .ToList()
                .AsReadOnly();
        }
    }
}
