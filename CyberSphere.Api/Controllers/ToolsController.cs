using CyberSphere.Application.Features.Auth.DTOs;
using CyberSphere.Application.Features.Tools.Commands.DeleteTool;
using CyberSphere.Application.Features.Tools.Commands.SubmitTool;
using CyberSphere.Application.Features.Tools.Commands.UpdateTool;
using CyberSphere.Application.Features.Tools.DTOs;
using CyberSphere.Application.Features.Tools.Queries.GetMyTools;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CyberSphere.Api.Controllers
{
    [ApiController]
    [Route("api/tools")]
    [Produces("application/json")]
    [Authorize]
    public sealed class ToolsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public ToolsController(IMediator mediator) => _mediator = mediator;

        // ── Owner: view own tools ─────────────────────────────────────────────────

        /// <summary>
        /// Get all tools submitted by the authenticated user (all statuses).
        /// Used to build the contributor dashboard.
        /// </summary>
        /// <response code="200">List of all tools belonging to the current user.</response>
        /// <response code="401">JWT missing or invalid.</response>
        [HttpGet("mine")]
        [ProducesResponseType(typeof(IReadOnlyList<ToolSubmissionResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<IReadOnlyList<ToolSubmissionResponse>>> GetMyTools(CancellationToken ct)
        {
            var result = await _mediator.Send(new GetMyToolsQuery(), ct);
            return Ok(result);
        }

        // ── Owner: submit ─────────────────────────────────────────────────────────

        /// <summary>
        /// Submit a new tool to the platform. The tool enters Pending status
        /// and is queued for admin review.
        /// </summary>
        /// <response code="201">Tool submitted — returns the created tool with Pending status.</response>
        /// <response code="401">JWT missing or invalid.</response>
        /// <response code="404">Specified category does not exist.</response>
        /// <response code="409">A tool with this name already exists.</response>
        /// <response code="422">Validation errors.</response>
        [HttpPost]
        [ProducesResponseType(typeof(ToolSubmissionResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
        public async Task<ActionResult<ToolSubmissionResponse>> SubmitTool(
            [FromBody] SubmitToolRequest request, CancellationToken ct)
        {
            var command = new SubmitToolCommand(
                request.Name,
                request.Description,
                request.CategoryId,
                request.DifficultyLevel,
                request.GitHubUrl,
                request.Tags,
                request.Commands);

            var result = await _mediator.Send(command, ct);
            return CreatedAtRoute(
                routeName: "GetToolById_Route",
                routeValues: new { id = result.Id },
                value: result);
        }

        // ── Owner: update ─────────────────────────────────────────────────────────

        /// <summary>
        /// Update an existing Pending or Rejected tool.
        /// Editing a Rejected tool automatically resets its status to Pending
        /// and requeues it for admin review.
        /// Approved tools cannot be edited.
        /// </summary>
        /// <param name="id">Tool GUID.</param>
        /// <response code="200">Tool updated — returns updated tool.</response>
        /// <response code="400">Tool is Approved and cannot be edited.</response>
        /// <response code="401">JWT missing or invalid.</response>
        /// <response code="403">Caller does not own this tool.</response>
        /// <response code="404">Tool or category not found.</response>
        /// <response code="422">Validation errors.</response>
        [HttpPut("{id:guid}")]
        [ProducesResponseType(typeof(ToolSubmissionResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
        public async Task<ActionResult<ToolSubmissionResponse>> UpdateTool(
            Guid id, [FromBody] UpdateToolRequest request, CancellationToken ct)
        {
            var command = new UpdateToolCommand(
                id,
                request.Name,
                request.Description,
                request.CategoryId,
                request.DifficultyLevel,
                request.GitHubUrl,
                request.Tags);

            var result = await _mediator.Send(command, ct);
            return Ok(result);
        }

        // ── Owner / Admin: delete ─────────────────────────────────────────────────

        /// <summary>
        /// Delete a tool. Owners may delete their own Pending or Rejected tools.
        /// Only admins may delete Approved tools.
        /// </summary>
        /// <param name="id">Tool GUID.</param>
        /// <response code="200">Tool deleted successfully.</response>
        /// <response code="400">Owner tried to delete an Approved tool.</response>
        /// <response code="401">JWT missing or invalid.</response>
        /// <response code="403">Caller does not own this tool and is not an admin.</response>
        /// <response code="404">Tool not found.</response>
        [HttpDelete("{id:guid}")]
        [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<MessageResponse>> DeleteTool(Guid id, CancellationToken ct)
        {
            var result = await _mediator.Send(new DeleteToolCommand(id), ct);
            return Ok(result);
        }
    }
}
