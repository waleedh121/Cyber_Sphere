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


namespace CyberSphere.Application.Features.Auth.Commands.Login
{

    // ─── Command ─────────────────────────────────────────────────────────────────

    public sealed record LoginCommand(
        string Email,
        string Password
    ) : IRequest<AuthResponse>;

    // ─── Validator ───────────────────────────────────────────────────────────────

    public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
    {
        public LoginCommandValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("A valid email address is required.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required.");
        }
    }

    // ─── Handler ─────────────────────────────────────────────────────────────────

    public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResponse>
    {
        private readonly IUserRepository _users;
        private readonly IPasswordHasher _hasher;
        private readonly ITokenService _tokens;

        public LoginCommandHandler(
            IUserRepository users,
            IPasswordHasher hasher,
            ITokenService tokens)
        {
            _users = users;
            _hasher = hasher;
            _tokens = tokens;
        }

        public async Task<AuthResponse> Handle(LoginCommand cmd, CancellationToken ct)
        {
            // Use a generic error message to avoid user enumeration
            const string invalidCredentials = "Invalid email or password.";

            var user = await _users.GetByEmailAsync(cmd.Email, ct)
                ?? throw new UnauthorizedException(invalidCredentials);

            if (!_hasher.Verify(cmd.Password, user.PasswordHash))
                throw new UnauthorizedException(invalidCredentials);

            var accessToken = _tokens.GenerateAccessToken(user);
            var refreshToken = _tokens.GenerateRefreshToken();
            user.SetRefreshToken(refreshToken, _tokens.RefreshTokenExpiresAt());

            await _users.SaveChangesAsync(ct);

            return new AuthResponse(
                UserId: user.Id,
                UserName: user.UserName,
                Email: user.Email,
                Role: user.Role.ToString(),
                AccessToken: accessToken,
                RefreshToken: refreshToken,
                AccessTokenExpiresAt: DateTime.UtcNow.AddMinutes(15)
            );
        }
    }
}
