using CyberSphere.Api.Extensions;
using CyberSphere.Application.Features.Ai.Commands.EndSession;
using CyberSphere.Application.Features.Ai.Commands.SendMessage;
using CyberSphere.Application.Features.Ai.Commands.StartSession;
using CyberSphere.Application.Features.Ai.DTOs;
using CyberSphere.Application.Features.Ai.Queries.GetHistory;
using CyberSphere.Application.Features.Ai.Queries.GetStats;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CyberSphere.Api.Controllers
{
    ///// <summary>
    ///// AI Assistant session endpoints. All routes require authentication.
    ///// The .NET backend acts purely as an API Gateway — it handles auth, validation,
    ///// history persistence, analytics tracking, and forwards requests to the
    ///// external Python AI microservice. No AI logic runs in this process.
    ///// </summary>
    //[ApiController]
    //[Route("api/ai")]
    //[Produces("application/json")]
    //[Authorize]
    //public sealed class AiController : ControllerBase
    //{
    //    private readonly IMediator _mediator;

    //    public AiController(IMediator mediator) => _mediator = mediator;

    //    // ── Session lifecycle ─────────────────────────────────────────────────────

    //    /// <summary>
    //    /// Start a new AI conversation session.
    //    /// Returns a session ID used for all subsequent messages.
    //    /// </summary>
    //    /// <response code="201">Session created — returns session metadata.</response>
    //    /// <response code="401">JWT missing or invalid.</response>
    //    /// <response code="422">Validation errors (title too long).</response>
    //    [HttpPost("sessions")]
    //    [ProducesResponseType(typeof(AiSessionResponse), StatusCodes.Status201Created)]
    //    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    //    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    //    public async Task<ActionResult<AiSessionResponse>> StartSession(
    //        [FromBody] StartAiSessionRequest request, CancellationToken ct)
    //    {
    //        var result = await _mediator.Send(new StartAiSessionCommand(request.Title), ct);
    //        return CreatedAtAction(nameof(GetSessionHistory), new { sessionId = result.Id }, result);
    //    }

    //    /// <summary>
    //    /// End an active AI session. Idempotent — calling on an already-ended session is safe.
    //    /// </summary>
    //    /// <param name="sessionId">AI session GUID.</param>
    //    /// <response code="200">Session ended.</response>
    //    /// <response code="401">JWT missing or invalid.</response>
    //    /// <response code="403">Session belongs to another user.</response>
    //    /// <response code="404">Session not found.</response>
    //    [HttpPost("sessions/{sessionId:guid}/end")]
    //    [ProducesResponseType(typeof(AiSessionResponse), StatusCodes.Status200OK)]
    //    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    //    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    //    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    //    public async Task<ActionResult<AiSessionResponse>> EndSession(
    //        Guid sessionId, CancellationToken ct)
    //    {
    //        var result = await _mediator.Send(new EndAiSessionCommand(sessionId), ct);
    //        return Ok(result);
    //    }

    //    // ── Messaging ─────────────────────────────────────────────────────────────

    //    /// <summary>
    //    /// Send a message to the AI assistant.
    //    /// The backend validates the request, builds conversation context from stored history,
    //    /// forwards to the Python AI microservice, persists both messages, and updates analytics.
    //    /// </summary>
    //    /// <param name="sessionId">Active AI session GUID.</param>
    //    /// <response code="200">Returns both the user message and the AI assistant reply.</response>
    //    /// <response code="400">Session has ended.</response>
    //    /// <response code="401">JWT missing or invalid.</response>
    //    /// <response code="403">Session belongs to another user.</response>
    //    /// <response code="404">Session not found.</response>
    //    /// <response code="422">Validation errors (empty message, message too long).</response>
    //    [HttpPost("sessions/{sessionId:guid}/messages")]
    //    [ProducesResponseType(typeof(AiChatResponse), StatusCodes.Status200OK)]
    //    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    //    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    //    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    //    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    //    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    //    public async Task<ActionResult<AiChatResponse>> SendMessage(
    //        Guid sessionId,
    //        [FromBody] SendAiMessageRequest request,
    //        CancellationToken ct)
    //    {
    //        var result = await _mediator.Send(new SendAiMessageCommand(sessionId, request.Message), ct);
    //        return Ok(result);
    //    }

    //    // ── History ───────────────────────────────────────────────────────────────

    //    /// <summary>
    //    /// Get all AI sessions for the current user (list view — no message content).
    //    /// </summary>
    //    /// <response code="200">List of sessions ordered by most recent first.</response>
    //    [HttpGet("sessions")]
    //    [ProducesResponseType(typeof(IReadOnlyList<AiSessionResponse>), StatusCodes.Status200OK)]
    //    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    //    public async Task<ActionResult<IReadOnlyList<AiSessionResponse>>> GetSessions(CancellationToken ct)
    //    {
    //        var result = await _mediator.Send(new GetAiSessionsQuery(), ct);
    //        return Ok(result);
    //    }

    //    /// <summary>
    //    /// Get the full message history for a specific AI session.
    //    /// Messages are returned in chronological order.
    //    /// </summary>
    //    /// <param name="sessionId">AI session GUID.</param>
    //    /// <response code="200">Session with full ordered message history.</response>
    //    /// <response code="403">Session belongs to another user.</response>
    //    /// <response code="404">Session not found.</response>
    //    [HttpGet("sessions/{sessionId:guid}", Name = "GetAiSessionHistory")]
    //    [ProducesResponseType(typeof(AiSessionWithHistoryResponse), StatusCodes.Status200OK)]
    //    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    //    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    //    public async Task<ActionResult<AiSessionWithHistoryResponse>> GetSessionHistory(
    //        Guid sessionId, CancellationToken ct)
    //    {
    //        var result = await _mediator.Send(new GetAiSessionHistoryQuery(sessionId), ct);
    //        return Ok(result);
    //    }

    //    // ── Analytics ─────────────────────────────────────────────────────────────

    //    /// <summary>
    //    /// Get the current user's AI usage statistics.
    //    /// Returns zeroed response if the user has never used AI.
    //    /// </summary>
    //    /// <response code="200">Total requests, tokens used, sessions, and last activity timestamp.</response>
    //    [HttpGet("stats")]
    //    [ProducesResponseType(typeof(UserAiStatsResponse), StatusCodes.Status200OK)]
    //    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    //    public async Task<ActionResult<UserAiStatsResponse>> GetMyStats(CancellationToken ct)
    //    {
    //        var result = await _mediator.Send(new GetUserAiStatsQuery(), ct);
    //        return Ok(result);
    //    }
    //}

    /// <summary>
    /// AI Assistant session endpoints. All routes require authentication.
    /// The .NET backend acts purely as an API Gateway — it handles auth, validation,
    /// history persistence, analytics tracking, and forwards requests to the
    /// external Python AI microservice. No AI logic runs in this process.
    /// </summary>
    [ApiController]
    [Route("api/ai")]
    [EnableRateLimiting(RateLimitingExtensions.AiPolicy)]
    [Produces("application/json")]
    [Authorize]
    public sealed class AiController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AiController(IMediator mediator) => _mediator = mediator;

        // ── Session lifecycle ─────────────────────────────────────────────────────

        /// <summary>
        /// Start a new AI conversation session.
        /// Returns a session ID used for all subsequent messages.
        /// </summary>
        /// <response code="201">Session created — returns session metadata.</response>
        /// <response code="401">JWT missing or invalid.</response>
        /// <response code="422">Validation errors (title too long).</response>
        [HttpPost("sessions")]
        [ProducesResponseType(typeof(AiSessionResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
        public async Task<ActionResult<AiSessionResponse>> StartSession(
            [FromBody] StartAiSessionRequest request, CancellationToken ct)
        {
            var result = await _mediator.Send(new StartAiSessionCommand(request.Title), ct);
            return CreatedAtAction(nameof(GetSessionHistory), new { sessionId = result.Id }, result);
        }

        /// <summary>
        /// End an active AI session. Idempotent — calling on an already-ended session is safe.
        /// </summary>
        /// <param name="sessionId">AI session GUID.</param>
        /// <response code="200">Session ended.</response>
        /// <response code="401">JWT missing or invalid.</response>
        /// <response code="403">Session belongs to another user.</response>
        /// <response code="404">Session not found.</response>
        [HttpPost("sessions/{sessionId:guid}/end")]
        [ProducesResponseType(typeof(AiSessionResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<AiSessionResponse>> EndSession(
            Guid sessionId, CancellationToken ct)
        {
            var result = await _mediator.Send(new EndAiSessionCommand(sessionId), ct);
            return Ok(result);
        }

        // ── Messaging ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Send a message to the AI assistant.
        /// The backend validates the request, builds conversation context from stored history,
        /// forwards to the Python AI microservice, persists both messages, and updates analytics.
        /// </summary>
        /// <param name="sessionId">Active AI session GUID.</param>
        /// <response code="200">Returns both the user message and the AI assistant reply.</response>
        /// <response code="400">Session has ended.</response>
        /// <response code="401">JWT missing or invalid.</response>
        /// <response code="403">Session belongs to another user.</response>
        /// <response code="404">Session not found.</response>
        /// <response code="422">Validation errors (empty message, message too long).</response>
        [HttpPost("sessions/{sessionId:guid}/messages")]
        [ProducesResponseType(typeof(AiChatResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
        public async Task<ActionResult<AiChatResponse>> SendMessage(
            Guid sessionId,
            [FromBody] SendAiMessageRequest request,
            CancellationToken ct)
        {
            var result = await _mediator.Send(new SendAiMessageCommand(sessionId, request.Message), ct);
            return Ok(result);
        }

        // ── History ───────────────────────────────────────────────────────────────

        /// <summary>
        /// Get all AI sessions for the current user (list view — no message content).
        /// </summary>
        /// <response code="200">List of sessions ordered by most recent first.</response>
        [HttpGet("sessions")]
        [ProducesResponseType(typeof(IReadOnlyList<AiSessionResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<IReadOnlyList<AiSessionResponse>>> GetSessions(CancellationToken ct)
        {
            var result = await _mediator.Send(new GetAiSessionsQuery(), ct);
            return Ok(result);
        }

        /// <summary>
        /// Get the full message history for a specific AI session.
        /// Messages are returned in chronological order.
        /// </summary>
        /// <param name="sessionId">AI session GUID.</param>
        /// <response code="200">Session with full ordered message history.</response>
        /// <response code="403">Session belongs to another user.</response>
        /// <response code="404">Session not found.</response>
        [HttpGet("sessions/{sessionId:guid}", Name = "GetAiSessionHistory")]
        [ProducesResponseType(typeof(AiSessionWithHistoryResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<AiSessionWithHistoryResponse>> GetSessionHistory(
            Guid sessionId, CancellationToken ct)
        {
            var result = await _mediator.Send(new GetAiSessionHistoryQuery(sessionId), ct);
            return Ok(result);
        }

        // ── Analytics ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Get the current user's AI usage statistics.
        /// Returns zeroed response if the user has never used AI.
        /// </summary>
        /// <response code="200">Total requests, tokens used, sessions, and last activity timestamp.</response>
        [HttpGet("stats")]
        [ProducesResponseType(typeof(UserAiStatsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<UserAiStatsResponse>> GetMyStats(CancellationToken ct)
        {
            var result = await _mediator.Send(new GetUserAiStatsQuery(), ct);
            return Ok(result);
        }
    }


}
