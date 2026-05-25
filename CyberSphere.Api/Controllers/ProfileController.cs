using CyberSphere.Application.Features.Auth.DTOs;
using CyberSphere.Application.Features.Profile.Commands.ChangePassword;
using CyberSphere.Application.Features.Profile.Commands.UpdateProfile;
using CyberSphere.Application.Features.Profile.DTOs;
using CyberSphere.Application.Features.Profile.Queries.GetMyProfile;
using CyberSphere.Application.Features.Profile.Queries.GetPublicProfile;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CyberSphere.Api.Controllers
{

    /// <summary>
    /// User profile endpoints.
    ///
    /// Private routes (require JWT):
    ///   GET  /api/users/me          — own full profile (email + role included)
    ///   PUT  /api/users/me          — update bio, ProfilePictureUrl, GitHubUrl
    ///   PUT  /api/users/me/password — change password (revokes refresh token)
    ///
    /// Public routes (no auth required):
    ///   GET  /api/users/{username}  — contributor's public profile + approved tools
    /// </summary>
    [ApiController]
    [Route("api/users")]
    [Produces("application/json")]
    public sealed class ProfileController : ControllerBase
    {
        private readonly IMediator _mediator;

        public ProfileController(IMediator mediator) => _mediator = mediator;

        // ── Private: authenticated user ───────────────────────────────────────────

        /// <summary>
        /// Get the authenticated user's full profile.
        /// Includes email and role — not exposed on the public endpoint.
        /// </summary>
        /// <response code="200">Authenticated user's profile.</response>
        /// <response code="401">JWT missing or invalid.</response>
        [HttpGet("me")]
        [Authorize]
        [ProducesResponseType(typeof(MyProfileResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<MyProfileResponse>> GetMyProfile(CancellationToken ct)
        {
            var result = await _mediator.Send(new GetMyProfileQuery(), ct);
            return Ok(result);
        }

        /// <summary>
        /// Update the authenticated user's community profile.
        /// All fields are optional — send only the fields you want to change.
        /// Pass null to clear a field.
        /// </summary>
        /// <response code="200">Profile updated — returns the full updated profile.</response>
        /// <response code="401">JWT missing or invalid.</response>
        /// <response code="422">Validation errors (invalid URL, field too long, non-github.com URL).</response>
        [HttpPut("me")]
        [Authorize]
        [ProducesResponseType(typeof(MyProfileResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
        public async Task<ActionResult<MyProfileResponse>> UpdateProfile(
            [FromBody] UpdateProfileRequest request, CancellationToken ct)
        {
            var command = new UpdateProfileCommand(
                request.Bio,
                request.ProfilePictureUrl,
                request.GitHubUrl);

            var result = await _mediator.Send(command, ct);
            return Ok(result);
        }

        /// <summary>
        /// Change the authenticated user's password.
        /// On success the refresh token is revoked — all active sessions on all devices
        /// are invalidated and the user must log in again.
        /// </summary>
        /// <response code="200">Password changed — re-authentication required.</response>
        /// <response code="401">JWT missing/invalid, or current password is incorrect.</response>
        /// <response code="422">Validation errors (weak password, mismatch, same as current).</response>
        [HttpPut("me/password")]
        [Authorize]
        [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
        public async Task<ActionResult<MessageResponse>> ChangePassword(
            [FromBody] ChangePasswordRequest request, CancellationToken ct)
        {
            var command = new ChangePasswordCommand(
                request.CurrentPassword,
                request.NewPassword,
                request.ConfirmNewPassword);

            var result = await _mediator.Send(command, ct);
            return Ok(result);
        }

        // ── Public: contributor profile ───────────────────────────────────────────

        /// <summary>
        /// Get a contributor's public profile by username.
        ///
        /// Returns the contributor's community info and a list of their Approved tools.
        /// Pending and Rejected tools are never included.
        /// Each tool includes CategoryName, CategorySlug, DifficultyLevel, AverageRating,
        /// RatingCount, UsageCount, Tags, GitHubUrl, and CreatedAt.
        ///
        /// This endpoint requires no authentication — visible to all visitors.
        /// Email and role are intentionally excluded from the response.
        /// </summary>
        /// <param name="username">Case-sensitive username of the contributor.</param>
        /// <response code="200">Contributor's public profile with approved tools list.</response>
        /// <response code="404">No user found with that username.</response>
        [HttpGet("{username}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(PublicProfileResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<PublicProfileResponse>> GetPublicProfile(
            string username, CancellationToken ct)
        {
            var result = await _mediator.Send(new GetPublicProfileQuery(username), ct);
            return Ok(result);
        }
    }
}
