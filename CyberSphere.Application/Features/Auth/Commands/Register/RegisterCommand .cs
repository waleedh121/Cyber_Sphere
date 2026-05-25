using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CyberSphere.Application.Features.Auth.DTOs;
using CyberSphere.Domain.Entities;
using CyberSphere.Domain.Enums;
using CyberSphere.Application.Exceptions;
using CyberSphere.Domain.Interfaces.Repositories;
using CyberSphere.Domain.Interfaces.Services;
using FluentValidation;
using MediatR;

namespace CyberSphere.Application.Features.Auth.Commands.Register
{

    // ─── Command ─────────────────────────────────────────────────────────────────

    public sealed record RegisterCommand(
        string UserName,
        string Email,
        string Password,
        string ConfirmPassword
    ) : IRequest<AuthResponse>;

    // ─── Validator ───────────────────────────────────────────────────────────────

    public sealed class RegisterCommandValidator : AbstractValidator<RegisterCommand>
    {
        public RegisterCommandValidator()
        {
            RuleFor(x => x.UserName)
                .NotEmpty().WithMessage("Username is required.")
                .MinimumLength(3).WithMessage("Username must be at least 3 characters.")
                .MaximumLength(50).WithMessage("Username cannot exceed 50 characters.")
                .Matches(@"^[a-zA-Z0-9_\-]+$").WithMessage("Username can only contain letters, numbers, underscores, and hyphens.");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("A valid email address is required.")
                .MaximumLength(200).WithMessage("Email cannot exceed 200 characters.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required.")
                .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
                .MaximumLength(100).WithMessage("Password cannot exceed 100 characters.")
                .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
                .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter.")
                .Matches(@"[0-9]").WithMessage("Password must contain at least one digit.")
                .Matches(@"[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character.");

            RuleFor(x => x.ConfirmPassword)
                .NotEmpty().WithMessage("Please confirm your password.")
                .Equal(x => x.Password).WithMessage("Passwords do not match.");
        }
    }

    // ─── Handler ─────────────────────────────────────────────────────────────────

    public sealed class RegisterCommandHandler : IRequestHandler<RegisterCommand, AuthResponse>
    {
        private readonly IUserRepository _users;
        private readonly IPasswordHasher _hasher;
        private readonly ITokenService _tokens;
        private readonly IEmailService _email;

        public RegisterCommandHandler(
            IUserRepository users,
            IPasswordHasher hasher,
            ITokenService tokens,
            IEmailService email)
        {
            _users = users;
            _hasher = hasher;
            _tokens = tokens;
            _email = email;
        }

        public async Task<AuthResponse> Handle(RegisterCommand cmd, CancellationToken ct)
        {
            // Uniqueness checks
            if (await _users.ExistsByEmailAsync(cmd.Email, ct))
                throw new ConflictException($"The email '{cmd.Email}' is already registered.");

            if (await _users.ExistsByUserNameAsync(cmd.UserName, ct))
                throw new ConflictException($"The username '{cmd.UserName}' is already taken.");

            // Create user via factory method
            var passwordHash = _hasher.Hash(cmd.Password);
            var user = User.Create(cmd.UserName, cmd.Email, passwordHash, Role.User);

            // Issue tokens
            var accessToken = _tokens.GenerateAccessToken(user);
            var refreshToken = _tokens.GenerateRefreshToken();
            user.SetRefreshToken(refreshToken, _tokens.RefreshTokenExpiresAt());

            await _users.AddAsync(user, ct);
            await _users.SaveChangesAsync(ct);

            // Fire-and-forget welcome email (non-blocking)
            _ = _email.SendWelcomeEmailAsync(user.Email, user.UserName, ct);

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
