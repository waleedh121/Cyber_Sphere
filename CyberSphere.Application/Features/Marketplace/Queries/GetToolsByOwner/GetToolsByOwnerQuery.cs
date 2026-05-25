using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CyberSphere.Application.Features.Marketplace.DTOs;
using CyberSphere.Application.Features.Marketplace.Queries.GetTools;
using CyberSphere.Domain.Interfaces.Repositories;
using MediatR;

namespace CyberSphere.Application.Features.Marketplace.Queries.GetToolsByOwner
{
    // ── Query ─────────────────────────────────────────────────────────────────────

    public sealed record GetToolsByOwnerQuery(Guid OwnerId) : IRequest<IReadOnlyList<ToolMarketplaceDto>>;

    // ── Handler ───────────────────────────────────────────────────────────────────

    public sealed class GetToolsByOwnerQueryHandler
        : IRequestHandler<GetToolsByOwnerQuery, IReadOnlyList<ToolMarketplaceDto>>
    {
        private readonly IToolRepository _tools;

        public GetToolsByOwnerQueryHandler(IToolRepository tools) => _tools = tools;

        public async Task<IReadOnlyList<ToolMarketplaceDto>> Handle(
            GetToolsByOwnerQuery query, CancellationToken ct)
        {
            var tools = await _tools.GetApprovedByOwnerAsync(query.OwnerId, ct);

            return tools
                .Select(GetToolsQueryHandler.MapToMarketplaceDto)
                .ToList()
                .AsReadOnly();
        }
    }
}
