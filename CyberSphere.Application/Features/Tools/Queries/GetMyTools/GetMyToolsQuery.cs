using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CyberSphere.Application.Features.Tools.Commands.SubmitTool;
using CyberSphere.Application.Features.Tools.DTOs;
using CyberSphere.Domain.Interfaces.Repositories;
using MediatR;

namespace CyberSphere.Application.Features.Tools.Queries.GetMyTools
{
    // ── Query ─────────────────────────────────────────────────────────────────────

    public sealed record GetMyToolsQuery : IRequest<IReadOnlyList<ToolSubmissionResponse>>;

    // ── Handler ───────────────────────────────────────────────────────────────────

    public sealed class GetMyToolsQueryHandler
        : IRequestHandler<GetMyToolsQuery, IReadOnlyList<ToolSubmissionResponse>>
    {
        private readonly IToolRepository _tools;
        private readonly ICategoryRepository _categories;
        private readonly ICurrentUserService _currentUser;

        public GetMyToolsQueryHandler(
            IToolRepository tools,
            ICategoryRepository categories,
            ICurrentUserService currentUser)
        {
            _tools = tools;
            _categories = categories;
            _currentUser = currentUser;
        }

        public async Task<IReadOnlyList<ToolSubmissionResponse>> Handle(
            GetMyToolsQuery _, CancellationToken ct)
        {
            // Returns all tools (all statuses) owned by the current user
            var tools = await _tools.GetAllByOwnerAsync(_currentUser.UserId, ct);

            // Load category names for the response
            var categories = await _categories.GetAllAsync(ct);
            var categoryMap = categories.ToDictionary(c => c.Id, c => c.Name);

            return tools
                .Select(t => SubmitToolCommandHandler.MapToResponse(
                    t,
                    categoryMap.GetValueOrDefault(t.CategoryId, "Unknown")))
                .ToList()
                .AsReadOnly();
        }
    }

}
