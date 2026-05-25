using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CyberSphere.Application.Features.Auth.DTOs
{

    // ─── Requests ────────────────────────────────────────────────────────────────

    public sealed record RegisterRequest(
        string UserName,
        string Email,
        string Password,
        string ConfirmPassword
    );

    public sealed record LoginRequest(
        string Email,
        string Password
    );

    public sealed record ForgotPasswordRequest(
        string Email
    );

    public sealed record ResetPasswordRequest(
        string Email,
        string Token,
        string NewPassword,
        string ConfirmNewPassword
    );

    public sealed record RefreshTokenRequest(
        string RefreshToken
    );

    // ─── Responses ───────────────────────────────────────────────────────────────

    public sealed record AuthResponse(
        Guid UserId,
        string UserName,
        string Email,
        string Role,
        string AccessToken,
        string RefreshToken,
        DateTime AccessTokenExpiresAt
    );

    public sealed record MessageResponse(
        string Message
    );
}
