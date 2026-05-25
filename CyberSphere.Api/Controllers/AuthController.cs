using CyberSphere.Application.Features.Auth.Commands.ForgotPassword;
using CyberSphere.Application.Features.Auth.Commands.Login;
using CyberSphere.Application.Features.Auth.Commands.RefreshToken;
using CyberSphere.Application.Features.Auth.Commands.Register;
using CyberSphere.Application.Features.Auth.Commands.ResetPassword;
using CyberSphere.Application.Features.Auth.DTOs;
using CyberSphere.Domain.Entities;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;

namespace CyberSphere.Api.Controllers
{

    [ApiController]
    [Route("api/auth")]
    [Produces("application/json")]
    public sealed class AuthController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AuthController(IMediator mediator) => _mediator = mediator;

        /// <summary>Register a new user account.</summary>
        /// <response code="200">Registration successful — returns JWT access token.</response>
        /// <response code="409">Email or username already in use.</response>
        /// <response code="422">Validation errors (weak password, invalid email, etc.).</response>
        [HttpPost("register")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
        public async Task<ActionResult<AuthResponse>> Register(
            [FromBody] RegisterRequest request, CancellationToken ct)
        {
            var command = new RegisterCommand(
                request.UserName, request.Email, request.Password, request.ConfirmPassword);

            var result = await _mediator.Send(command, ct);
            return Ok(result);
        }

        /// <summary>Authenticate with email and password.</summary>
        /// <response code="200">Login successful — returns JWT access token and refresh token.</response>
        /// <response code="401">Invalid email or password.</response>
        [HttpPost("login")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<AuthResponse>> Login(
            [FromBody] LoginRequest request, CancellationToken ct)
        {
            var command = new LoginCommand(request.Email, request.Password);
            var result = await _mediator.Send(command, ct);
            return Ok(result);
        }

        /// <summary>Issue a new access token using a valid refresh token.</summary>
        /// <response code="200">Token refreshed successfully.</response>
        /// <response code="401">Refresh token is invalid or expired.</response>
        [HttpPost("refresh-token")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<AuthResponse>> RefreshToken(
            [FromBody] RefreshTokenRequest request, CancellationToken ct)
        {
            var command = new RefreshTokenCommand(request.RefreshToken);
            var result = await _mediator.Send(command, ct);
            return Ok(result);
        }

        /// <summary>Request a password reset email. Always returns 200 to prevent user enumeration.</summary>
        /// <response code="200">If the email is registered, a reset link has been sent.</response>
        [HttpPost("forgot-password")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<MessageResponse>> ForgotPassword(
            [FromBody] ForgotPasswordRequest request, CancellationToken ct)
        {
            var command = new ForgotPasswordCommand(request.Email);
            var result = await _mediator.Send(command, ct);
            return Ok(result);
        }

        /// <summary>Reset the password using the token sent to the user's email.</summary>
        /// <response code="200">Password reset successfully.</response>
        /// <response code="401">Invalid or expired reset token.</response>
        /// <response code="422">Validation errors (weak password, mismatch, etc.).</response>
        [HttpPost("reset-password")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
        public async Task<ActionResult<MessageResponse>> ResetPassword(
            [FromBody] ResetPasswordRequest request, CancellationToken ct)
        {
            var command = new ResetPasswordCommand(
                request.Email, request.Token, request.NewPassword, request.ConfirmNewPassword);

            var result = await _mediator.Send(command, ct);
            return Ok(result);
        }

        /// <summary>Verify the currently authenticated user's identity. Requires a valid JWT.</summary>
        /// <response code="200">Token is valid — returns basic user info from claims.</response>
        /// <response code="401">Token is missing, invalid, or expired.</response>
        [HttpGet("me")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public IActionResult Me()
        {
            return Ok(new
            {
                UserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                UserName = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value
                         ?? User.Identity?.Name,
                Role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value,
            });
        }
    }
}