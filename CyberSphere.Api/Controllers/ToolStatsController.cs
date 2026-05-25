using CyberSphere.Application.Features.Auth.DTOs;
using CyberSphere.Application.Features.Ratings.DTOs;
using CyberSphere.Application.Features.Tools.Commands.IncrementUsage;
using CyberSphere.Application.Features.Tools.Queries.GetToolStats;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CyberSphere.Api.Controllers
{

    /// <summary>
    /// Tool usage tracking and per-tool statistics.
    /// </summary>
    [ApiController]
    [Route("api/tools")]
    [Produces("application/json")]
    public sealed class ToolStatsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public ToolStatsController(IMediator mediator) => _mediator = mediator;

        /// <summary>
        /// Get statistics for a specific tool:
        /// UsageCount, AverageRating, RatingCount, and a per-star breakdown.
        /// Accessible to the tool owner, admins, and any authenticated user.
        /// </summary>
        /// <param name="id">Tool GUID.</param>
        /// <response code="200">Tool statistics including rating breakdown.</response>
        /// <response code="401">JWT missing or invalid.</response>
        /// <response code="404">Tool not found.</response>
        [HttpGet("{id:guid}/stats")]
        [Authorize]
        [ProducesResponseType(typeof(ToolStatsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ToolStatsResponse>> GetToolStats(Guid id, CancellationToken ct)
        {
            var result = await _mediator.Send(new GetToolStatsQuery(id), ct);
            return Ok(result);
        }

        /// <summary>
        /// Record a usage event for a tool.
        /// Called when a user opens a CLI session or interacts with a tool.
        /// Only tracks Approved tools.
        /// </summary>
        /// <param name="id">Tool GUID.</param>
        /// <response code="200">Usage count incremented.</response>
        /// <response code="400">Tool is not Approved.</response>
        /// <response code="401">JWT missing or invalid.</response>
        /// <response code="404">Tool not found.</response>
        [HttpPost("{id:guid}/usage")]
        [Authorize]
        [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<MessageResponse>> IncrementUsage(Guid id, CancellationToken ct)
        {
            var result = await _mediator.Send(new IncrementToolUsageCommand(id), ct);
            return Ok(result);
        }
    }
}
