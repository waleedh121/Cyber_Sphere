using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CyberSphere.Application.Features.Auth.DTOs;
using CyberSphere.Application.Exceptions;
using CyberSphere.Domain.Interfaces.Repositories;
using CyberSphere.Domain.Interfaces.Services;
using FluentValidation;
using MediatR;

namespace CyberSphere.Application.Features.Auth.Commands.RefreshToken
{
// ─── Command ─────────────────────────────────────────────────────────────────
 
public sealed record RefreshTokenCommand(string RefreshToken) : IRequest<AuthResponse>;

    // ─── Validator ───────────────────────────────────────────────────────────────

    public sealed class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
    {
        public RefreshTokenCommandValidator()
        {
            RuleFor(x => x.RefreshToken)
                .NotEmpty().WithMessage("Refresh token is required.");
        }
    }

    // ─── Handler ─────────────────────────────────────────────────────────────────

    public sealed class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, AuthResponse>
    {
        private readonly IUserRepository _users;
        private readonly ITokenService _tokens;

        public RefreshTokenCommandHandler(IUserRepository users, ITokenService tokens)
        {
            _users = users;
            _tokens = tokens;
        }

        public async Task<AuthResponse> Handle(RefreshTokenCommand cmd, CancellationToken ct)
        {
            var user = await _users.GetByRefreshTokenAsync(cmd.RefreshToken, ct)
                ?? throw new UnauthorizedException("Invalid refresh token.");

            if (!user.IsRefreshTokenValid(cmd.RefreshToken))
                throw new UnauthorizedException("Refresh token has expired. Please log in again.");

            // Rotate both tokens on every refresh (token rotation security pattern)
            var newAccessToken = _tokens.GenerateAccessToken(user);
            var newRefreshToken = _tokens.GenerateRefreshToken();
            user.SetRefreshToken(newRefreshToken, _tokens.RefreshTokenExpiresAt());

            await _users.SaveChangesAsync(ct);

            return new AuthResponse(
                UserId: user.Id,
                UserName: user.UserName,
                Email: user.Email,
                Role: user.Role.ToString(),
                AccessToken: newAccessToken,
                RefreshToken: newRefreshToken,
                AccessTokenExpiresAt: DateTime.UtcNow.AddMinutes(15)
            );
        }
    }
}
