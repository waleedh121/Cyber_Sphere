using CyberSphere.Application.Features.Auth.DTOs;
using CyberSphere.Application.Features.Ratings.Commands.DeleteRating;
using CyberSphere.Application.Features.Ratings.Commands.RateTool;
using CyberSphere.Application.Features.Ratings.Commands.UpdateRating;
using CyberSphere.Application.Features.Ratings.DTOs;
using CyberSphere.Application.Features.Ratings.Queries.GetToolRatings;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CyberSphere.Api.Controllers
{
    /// <summary>
    /// Tool ratings. One rating per user per tool.
    /// Owners cannot rate their own tools.
    /// Only Approved tools can be rated.
    /// </summary>
    [ApiController]
    [Route("api/tools/{toolId:guid}/ratings")]
    [Produces("application/json")]
    public sealed class RatingsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public RatingsController(IMediator mediator) => _mediator = mediator;

        // ── Public: read ratings ──────────────────────────────────────────────────

        /// <summary>
        /// Get paged ratings for a tool. Includes commenter username.
        /// No authentication required.
        /// </summary>
        /// <param name="toolId">Tool GUID.</param>
        /// <param name="page">1-based page number (default: 1).</param>
        /// <param name="pageSize">Items per page, 1–50 (default: 10).</param>
        /// <response code="200">Paged ratings list, newest first.</response>
        /// <response code="404">Tool not found.</response>
        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType(typeof(PagedRatingsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<PagedRatingsResponse>> GetRatings(
            Guid toolId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            CancellationToken ct = default)
        {
            var result = await _mediator.Send(new GetToolRatingsQuery(toolId, page, pageSize), ct);
            return Ok(result);
        }

        /// <summary>
        /// Get the authenticated user's rating for this tool.
        /// Returns 204 No Content if the user hasn't rated this tool yet.
        /// </summary>
        /// <param name="toolId">Tool GUID.</param>
        /// <response code="200">The user's existing rating.</response>
        /// <response code="204">User has not rated this tool.</response>
        /// <response code="401">JWT missing or invalid.</response>
        [HttpGet("mine")]
        [Authorize]
        [ProducesResponseType(typeof(RatingResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<RatingResponse>> GetMyRating(
            Guid toolId, CancellationToken ct)
        {
            var result = await _mediator.Send(new GetMyRatingQuery(toolId), ct);
            return result is null ? NoContent() : Ok(result);
        }

        // ── Authenticated: write ratings ──────────────────────────────────────────

        /// <summary>
        /// Submit a new rating for a tool.
        /// One rating per user per tool. Owners cannot rate their own tools.
        /// </summary>
        /// <param name="toolId">Tool GUID.</param>
        /// <response code="201">Rating created.</response>
        /// <response code="400">Tool not Approved, or user is the owner.</response>
        /// <response code="401">JWT missing or invalid.</response>
        /// <response code="404">Tool not found.</response>
        /// <response code="409">User has already rated this tool.</response>
        /// <response code="422">Validation errors (stars out of range, comment too long).</response>
        [HttpPost]
        [Authorize]
        [ProducesResponseType(typeof(RatingResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
        public async Task<ActionResult<RatingResponse>> RateTool(
            Guid toolId,
            [FromBody] RateToolRequest request,
            CancellationToken ct)
        {
            var result = await _mediator.Send(
                new RateToolCommand(toolId, request.Stars, request.Comment), ct);

            return CreatedAtAction(nameof(GetMyRating), new { toolId }, result);
        }

        /// <summary>
        /// Update an existing rating. Only the rating author can update it.
        /// Re-syncs the tool's AverageRating.
        /// </summary>
        /// <param name="toolId">Tool GUID.</param>
        /// <param name="ratingId">Rating GUID.</param>
        /// <response code="200">Rating updated.</response>
        /// <response code="401">JWT missing or invalid.</response>
        /// <response code="403">Caller is not the rating author.</response>
        /// <response code="404">Rating not found.</response>
        /// <response code="422">Validation errors.</response>
        [HttpPut("{ratingId:guid}")]
        [Authorize]
        [ProducesResponseType(typeof(RatingResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
        public async Task<ActionResult<RatingResponse>> UpdateRating(
            Guid toolId,
            Guid ratingId,
            [FromBody] UpdateRatingRequest request,
            CancellationToken ct)
        {
            var result = await _mediator.Send(
                new UpdateRatingCommand(ratingId, request.Stars, request.Comment), ct);
            return Ok(result);
        }

        /// <summary>
        /// Delete a rating. Authors can delete their own; admins can delete any.
        /// Corrects the tool's AverageRating on deletion.
        /// </summary>
        /// <param name="toolId">Tool GUID.</param>
        /// <param name="ratingId">Rating GUID.</param>
        /// <response code="200">Rating deleted.</response>
        /// <response code="401">JWT missing or invalid.</response>
        /// <response code="403">Caller is not the author and not an admin.</response>
        /// <response code="404">Rating not found.</response>
        [HttpDelete("{ratingId:guid}")]
        [Authorize]
        [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<MessageResponse>> DeleteRating(
            Guid toolId,
            Guid ratingId,
            CancellationToken ct)
        {
            var result = await _mediator.Send(new DeleteRatingCommand(ratingId), ct);
            return Ok(result);
        }
    }
}
