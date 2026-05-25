using CyberSphere.Application.Features.Sessions.Commands.EndSession;
using CyberSphere.Application.Features.Sessions.Commands.StartSession;
using CyberSphere.Application.Features.Sessions.DTOs;
using CyberSphere.Application.Features.Sessions.Queries.GetActiveSessions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CyberSphere.Api.Controllers
{
    /// <summary>
    /// VM execution session management.
    /// Session types: Scanning (1), CliTools (3), Sandbox (4).
    /// Each session type routes to a dedicated VM pool.
    /// One active session per type per user is enforced.
    /// </summary>
    [ApiController]
    [Route("api/sessions")]
    [Produces("application/json")]
    [Authorize]
    public sealed class SessionsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public SessionsController(IMediator mediator) => _mediator = mediator;

        /// <summary>
        /// Start a new VM session of the specified type.
        /// Selects the best available VM, performs an SSH health check,
        /// initialises the user's workspace, and returns a connection token.
        /// </summary>
        /// <response code="201">Session started — includes ConnectionToken, VmIpAddress, VmPort.</response>
        /// <response code="400">Session type is invalid.</response>
        /// <response code="401">JWT missing or invalid.</response>
        /// <response code="409">User already has an active session of this type.</response>
        /// <response code="503">No VMs available or VM health check failed.</response>
        [HttpPost]
        [ProducesResponseType(typeof(SessionResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
        public async Task<ActionResult<SessionResponse>> StartSession(
            [FromBody] StartSessionRequest request, CancellationToken ct)
        {
            var result = await _mediator.Send(new StartSessionCommand(request.Type), ct);
            return CreatedAtAction(nameof(GetActiveSessions), null, result);
        }

        /// <summary>
        /// End an active session gracefully.
        /// Triggers SSH teardown, releases the VM slot, and invalidates the connection token.
        /// </summary>
        /// <param name="id">Session GUID.</param>
        /// <response code="200">Session ended.</response>
        /// <response code="400">Session is not active.</response>
        /// <response code="401">JWT missing or invalid.</response>
        /// <response code="403">Session belongs to another user.</response>
        /// <response code="404">Session not found.</response>
        [HttpPost("{id:guid}/end")]
        [ProducesResponseType(typeof(SessionResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<SessionResponse>> EndSession(
            Guid id, CancellationToken ct)
        {
            var result = await _mediator.Send(new EndSessionCommand(id), ct);
            return Ok(result);
        }

        /// <summary>
        /// Get all currently active sessions for the authenticated user.
        /// </summary>
        /// <response code="200">List of active sessions (may be empty).</response>
        [HttpGet("active")]
        [ProducesResponseType(typeof(IReadOnlyList<SessionResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IReadOnlyList<SessionResponse>>> GetActiveSessions(
            CancellationToken ct)
        {
            var result = await _mediator.Send(new GetSessionsQuery(), ct);
            return Ok(result);
        }

        /// <summary>
        /// Get the authenticated user's full session history (all statuses), paged.
        /// Connection tokens are NOT included in history — only active session responses carry them.
        /// </summary>
        /// <response code="200">Paged session history.</response>
        /// <response code="422">Validation errors (page out of range).</response>
        [HttpGet("history")]
        [ProducesResponseType(typeof(PagedSessionsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
        public async Task<ActionResult<PagedSessionsResponse>> GetSessionHistory(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken ct = default)
        {
            var result = await _mediator.Send(new GetSessionHistoryQuery(page, pageSize), ct);
            return Ok(result);
        }
    }
}
