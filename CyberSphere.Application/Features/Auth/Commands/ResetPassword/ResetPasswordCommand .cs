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

namespace CyberSphere.Application.Features.Auth.Commands.ResetPassword
{

    // ─── Command ─────────────────────────────────────────────────────────────────

    public sealed record ResetPasswordCommand(
        string Email,
        string Token,
        string NewPassword,
        string ConfirmNewPassword
    ) : IRequest<MessageResponse>;

    // ─── Validator ───────────────────────────────────────────────────────────────

    public sealed class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
    {
        public ResetPasswordCommandValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("A valid email address is required.");

            RuleFor(x => x.Token)
                .NotEmpty().WithMessage("Reset token is required.");

            RuleFor(x => x.NewPassword)
                .NotEmpty().WithMessage("New password is required.")
                .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
                .MaximumLength(100).WithMessage("Password cannot exceed 100 characters.")
                .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
                .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter.")
                .Matches(@"[0-9]").WithMessage("Password must contain at least one digit.")
                .Matches(@"[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character.");

            RuleFor(x => x.ConfirmNewPassword)
                .NotEmpty().WithMessage("Please confirm your new password.")
                .Equal(x => x.NewPassword).WithMessage("Passwords do not match.");
        }
    }

    // ─── Handler ─────────────────────────────────────────────────────────────────

    public sealed class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, MessageResponse>
    {
        private readonly IUserRepository _users;
        private readonly IPasswordHasher _hasher;

        public ResetPasswordCommandHandler(IUserRepository users, IPasswordHasher hasher)
        {
            _users = users;
            _hasher = hasher;
        }

        public async Task<MessageResponse> Handle(ResetPasswordCommand cmd, CancellationToken ct)
        {
            var user = await _users.GetByEmailAsync(cmd.Email, ct)
                ?? throw new UnauthorizedException("Invalid or expired reset token.");

            if (!user.IsPasswordResetTokenValid(cmd.Token))
                throw new UnauthorizedException("Invalid or expired reset token.");

            var newHash = _hasher.Hash(cmd.NewPassword);
            user.UpdatePasswordHash(newHash);
            user.ClearPasswordResetToken();
            user.RevokeRefreshToken(); // invalidate all sessions on password change

            await _users.SaveChangesAsync(ct);

            return new MessageResponse("Password reset successfully. Please log in with your new password.");
        }
    }
}
