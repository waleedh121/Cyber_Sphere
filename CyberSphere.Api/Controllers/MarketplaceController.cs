using CyberSphere.Application.Features.Marketplace.DTOs;
using CyberSphere.Application.Features.Marketplace.Queries.GetCategories;
using CyberSphere.Application.Features.Marketplace.Queries.GetToolById;
using CyberSphere.Application.Features.Marketplace.Queries.GetTools;
using CyberSphere.Application.Features.Marketplace.Queries.GetToolsByOwner;
using CyberSphere.Domain.Enums;
using CyberSphere.Domain.Interfaces.Repositories;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CyberSphere.Api.Controllers
{

    [ApiController]
    [Route("api/marketplace")]
    [Produces("application/json")]
    public sealed class MarketplaceController : ControllerBase
    {
        private readonly IMediator _mediator;

        public MarketplaceController(IMediator mediator) => _mediator = mediator;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // Tools
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        /// <summary>
        /// Browse approved tools with optional search, category, difficulty filter, and sorting.
        /// Always returns the tool owner's public profile embedded in each result card.
        /// </summary>
        /// <param name="search">Full-text search across name, description, and tags.</param>
        /// <param name="categoryId">Filter by category ID.</param>
        /// <param name="difficultyLevel">0=Beginner, 1=Intermediate, 2=Advanced, 3=Expert.</param>
        /// <param name="sortBy">0=Newest, 1=MostUsed, 2=HighestRated, 3=Alphabetical.</param>
        /// <param name="page">1-based page number (default: 1).</param>
        /// <param name="pageSize">Items per page, 1–50 (default: 12).</param>
        /// <response code="200">Paged tool list with embedded owner profiles.</response>
        /// <response code="422">Validation errors (page out of range, pageSize too large, etc.).</response>
        [HttpGet("tools")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(PagedToolsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
        public async Task<ActionResult<PagedToolsResponse>> GetTools(
            [FromQuery] string? search = null,
            [FromQuery] Guid? categoryId = null,
            [FromQuery] DifficultyLevel? difficultyLevel = null,
            [FromQuery] ToolSortBy sortBy = ToolSortBy.Newest,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 12,
            CancellationToken ct = default)
        {
            var query = new GetToolsQuery(search, categoryId, difficultyLevel, sortBy, page, pageSize);
            var result = await _mediator.Send(query, ct);
            return Ok(result);
        }

        /// <summary>
        /// Get full details for a single approved tool — includes commands list and owner profile.
        /// Also usable by unauthenticated visitors (public tool cards).
        /// </summary>
        /// <param name="id">Tool GUID.</param>
        /// <response code="200">Full tool detail with commands and owner profile.</response>
        /// <response code="404">Tool not found.</response>
        [HttpGet("tools/{id:guid}", Name = "GetToolById_Route")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ToolDetailDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ToolDetailDto>> GetToolById(Guid id, CancellationToken ct)
        {
            var result = await _mediator.Send(new GetToolByIdQuery(id), ct);
            return Ok(result);
        }

        /// <summary>
        /// Get all approved tools by a specific owner (used to build contributor pages).
        /// </summary>
        /// <param name="ownerId">Owner's user GUID.</param>
        /// <response code="200">List of approved tools for the specified owner.</response>
        [HttpGet("tools/by-owner/{ownerId:guid}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(IReadOnlyList<ToolMarketplaceDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IReadOnlyList<ToolMarketplaceDto>>> GetToolsByOwner(
            Guid ownerId, CancellationToken ct)
        {
            var result = await _mediator.Send(new GetToolsByOwnerQuery(ownerId), ct);
            return Ok(result);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // Categories
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        /// <summary>
        /// Get all categories with their approved tool count.
        /// Used to populate the marketplace filter sidebar.
        /// </summary>
        /// <response code="200">Alphabetically sorted category list.</response>
        [HttpGet("categories")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(IReadOnlyList<CategoryDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IReadOnlyList<CategoryDto>>> GetCategories(CancellationToken ct)
        {
            var result = await _mediator.Send(new GetCategoriesQuery(), ct);
            return Ok(result);
        }
    }
}
