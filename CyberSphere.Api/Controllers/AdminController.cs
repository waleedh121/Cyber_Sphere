using CyberSphere.Application.Features.Admin.Commands.ManageServer;
using CyberSphere.Application.Features.Admin.Commands.ReviewTool;
using CyberSphere.Application.Features.Admin.Commands.TerminateSession;
using CyberSphere.Application.Features.Admin.DTOs;
using CyberSphere.Application.Features.Admin.Queries.GetAllSessions;
using CyberSphere.Application.Features.Admin.Queries.GetDashboardStats;
using CyberSphere.Application.Features.Admin.Queries.GetPendingTools;
using CyberSphere.Application.Features.Admin.Queries.GetToolStats;
using CyberSphere.Application.Features.Auth.DTOs;
using CyberSphere.Application.Features.Ratings.DTOs;
using CyberSphere.Application.Features.Sessions.DTOs;
using CyberSphere.Application.Features.Tools.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CyberSphere.Api.Controllers
{

    /// <summary>
    /// Admin-only endpoints. All routes require [Authorize(Roles = "Admin")].
    /// A 401 means no token; a 403 means the token is valid but the role is User.
    /// </summary>
    [ApiController]
    [Route("api/admin")]
    [Produces("application/json")]
    [Authorize(Roles = "Admin")]
    public sealed class AdminController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AdminController(IMediator mediator) => _mediator = mediator;

        // ── Review queue ──────────────────────────────────────────────────────────

        /// <summary>
        /// Get the paginated list of all tools awaiting review, ordered oldest-first (FIFO).
        /// Each card includes the owner's info and the last review decision (if any).
        /// </summary>
        /// <param name="page">1-based page number (default: 1).</param>
        /// <param name="pageSize">Items per page, 1–50 (default: 20).</param>
        /// <response code="200">Paged list of pending tools.</response>
        /// <response code="401">JWT missing or invalid.</response>
        /// <response code="403">Caller is not an admin.</response>
        [HttpGet("tools/pending")]
        [ProducesResponseType(typeof(PagedPendingToolsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<PagedPendingToolsResponse>> GetPendingTools(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken ct = default)
        {
            var result = await _mediator.Send(new GetPendingToolsQuery(page, pageSize), ct);
            return Ok(result);
        }

        // ── Review decision ───────────────────────────────────────────────────────

        /// <summary>
        /// Approve or reject a pending tool.
        /// On Approval: tool status → Approved, owner.TotalToolsUploaded++, approval email sent.
        /// On Rejection: tool status → Rejected, rejection email with notes sent to owner.
        /// Notes are mandatory when rejecting.
        /// </summary>
        /// <param name="id">Tool GUID to review.</param>
        /// <response code="200">Decision recorded — returns decision summary.</response>
        /// <response code="400">Tool is not in Pending status.</response>
        /// <response code="401">JWT missing or invalid.</response>
        /// <response code="403">Caller is not an admin.</response>
        /// <response code="404">Tool not found.</response>
        /// <response code="422">Validation errors (missing rejection notes, etc.).</response>
        [HttpPost("tools/{id:guid}/review")]
        [ProducesResponseType(typeof(ReviewDecisionResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
        public async Task<ActionResult<ReviewDecisionResponse>> ReviewTool(
            Guid id, [FromBody] ReviewToolRequest request, CancellationToken ct)
        {
            var command = new ReviewToolCommand(id, request.Decision, request.Notes);
            var result = await _mediator.Send(command, ct);
            return Ok(result);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // Session Management
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        /// <summary>Get all platform sessions (all users, all statuses), paged.</summary>
        [HttpGet("sessions")]
        [ProducesResponseType(typeof(PagedAdminSessionsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<PagedAdminSessionsResponse>> GetAllSessions(
            [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        {
            var result = await _mediator.Send(new AdminGetAllSessionsQuery(page, pageSize), ct);
            return Ok(result);
        }

        /// <summary>Force-terminate any active session.</summary>
        [HttpPost("sessions/{id:guid}/terminate")]
        [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<MessageResponse>> TerminateSession(
            Guid id, [FromBody] TerminateSessionRequest request, CancellationToken ct)
        {
            var result = await _mediator.Send(new AdminTerminateSessionCommand(id, request.Reason), ct);
            return Ok(result);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // Server (VM) Management
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        /// <summary>Get status dashboard for all VM servers.</summary>
        [HttpGet("servers")]
        [ProducesResponseType(typeof(IReadOnlyList<ServerStatusResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IReadOnlyList<ServerStatusResponse>>> GetServers(CancellationToken ct)
        {
            var result = await _mediator.Send(new GetServerStatusQuery(), ct);
            return Ok(result);
        }

        /// <summary>Register a new VM server in the infrastructure pool.</summary>
        [HttpPost("servers")]
        [ProducesResponseType(typeof(ServerStatusResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
        public async Task<ActionResult<ServerStatusResponse>> RegisterServer(
            [FromBody] RegisterServerCommand command, CancellationToken ct)
        {
            var result = await _mediator.Send(command, ct);
            return CreatedAtAction(nameof(GetServers), null, result);
        }

        /// <summary>Set a server's status (Online / Offline / Maintenance).</summary>
        [HttpPut("servers/{id:guid}/status")]
        [ProducesResponseType(typeof(ServerStatusResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ServerStatusResponse>> SetServerStatus(
            Guid id, [FromBody] SetServerStatusRequest request, CancellationToken ct)
        {
            var result = await _mediator.Send(new SetServerStatusCommand(id, request.Status), ct);
            return Ok(result);
        }

        /// <summary>Trigger an on-demand SSH health check for a specific server.</summary>
        [HttpPost("servers/{id:guid}/health-check")]
        [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<MessageResponse>> HealthCheckServer(
            Guid id, CancellationToken ct)
        {
            var result = await _mediator.Send(new HealthCheckServerCommand(id), ct);
            return Ok(result);
        }
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // Platform Analytics
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        /// <summary>
        /// Platform-wide tool analytics: totals by status, top-rated tools,
        /// most-used tools, total ratings, and overall average rating.
        /// </summary>
        /// <response code="200">Platform statistics snapshot.</response>
        [HttpGet("stats/tools")]
        [ProducesResponseType(typeof(PlatformToolStatsResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<PlatformToolStatsResponse>> GetPlatformToolStats(
            CancellationToken ct)
        {
            var result = await _mediator.Send(new GetPlatformToolStatsQuery(), ct);
            return Ok(result);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // Admin Dashboard
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        /// <summary>
        /// Aggregated platform dashboard: users, tools, sessions, ratings, AI usage,
        /// and the 10 most-recent tool submissions and sessions.
        /// All queries run concurrently.
        /// </summary>
        /// <response code="200">Full platform dashboard snapshot.</response>
        [HttpGet("dashboard")]
        [ProducesResponseType(typeof(AdminDashboardResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<AdminDashboardResponse>> GetDashboard(CancellationToken ct)
        {
            var result = await _mediator.Send(new GetAdminDashboardQuery(), ct);
            return Ok(result);
        }
    }
}