using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CyberSphere.Application.Features.Marketplace.DTOs;
using CyberSphere.Application.Features.Marketplace.Queries.GetTools;
using CyberSphere.Domain.Interfaces.Repositories;
using CyberSphere.Application.Exceptions;
using MediatR;

namespace CyberSphere.Application.Features.Marketplace.Queries.GetToolById
{
    // ── Query ─────────────────────────────────────────────────────────────────────

    public sealed record GetToolByIdQuery(Guid ToolId) : IRequest<ToolDetailDto>;

    // ── Handler ───────────────────────────────────────────────────────────────────

    public sealed class GetToolByIdQueryHandler : IRequestHandler<GetToolByIdQuery, ToolDetailDto>
    {
        private readonly IToolRepository _tools;

        public GetToolByIdQueryHandler(IToolRepository tools) => _tools = tools;

        public async Task<ToolDetailDto> Handle(GetToolByIdQuery query, CancellationToken ct)
        {
            // GetByIdWithDetailsAsync eagerly loads Owner + Category + Commands
            var tool = await _tools.GetByIdWithDetailsAsync(query.ToolId, ct)
                ?? throw new NotFoundException(nameof(Domain.Entities.Tool), query.ToolId);

            var commands = tool.Commands
                .Select(c => new ToolCommandDto(
                    Id: c.Id,
                    Name: c.Name,
                    Description: c.Description,
                    Syntax: c.Syntax,
                    Example: c.Example))
                .ToList()
                .AsReadOnly();

            return new ToolDetailDto(
                Id: tool.Id,
                Name: tool.Name,
                Description: tool.Description,
                CategoryName: tool.Category.Name,
                CategoryId: tool.CategoryId,
                DifficultyLevel: tool.DifficultyLevel.ToString(),
                UsageCount: tool.UsageCount,
                AverageRating: Math.Round(tool.AverageRating, 2),
                RatingCount: tool.RatingCount,
                Tags: tool.GetTags(),
                Commands: commands,
                GitHubUrl: tool.GitHubUrl,
                CreatedAt: tool.CreatedAt,
                Owner: GetToolsQueryHandler.MapToOwnerDto(tool.Owner)
            );
        }
    }

}
