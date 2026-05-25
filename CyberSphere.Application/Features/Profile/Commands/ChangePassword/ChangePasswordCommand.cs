using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CyberSphere.Application.Features.Auth.DTOs;
using CyberSphere.Domain.Interfaces.Repositories;
using CyberSphere.Domain.Interfaces.Services;
using FluentValidation;
using CyberSphere.Application.Exceptions;
using MediatR;

namespace CyberSphere.Application.Features.Profile.Commands.ChangePassword
{
    // ── Command ───────────────────────────────────────────────────────────────────

    public sealed record ChangePasswordCommand(
        string CurrentPassword,
        string NewPassword,
        string ConfirmNewPassword
    ) : IRequest<MessageResponse>;

    // ── Validator ─────────────────────────────────────────────────────────────────

    public sealed class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
    {
        public ChangePasswordCommandValidator()
        {
            RuleFor(x => x.CurrentPassword)
                .NotEmpty().WithMessage("Current password is required.");

            RuleFor(x => x.NewPassword)
                .NotEmpty().WithMessage("New password is required.")
                .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
                .MaximumLength(100).WithMessage("Password must not exceed 100 characters.")
                .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
                .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter.")
                .Matches(@"[0-9]").WithMessage("Password must contain at least one digit.")
                .Matches(@"[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character.")
                .NotEqual(x => x.CurrentPassword).WithMessage("New password must differ from the current password.");

            RuleFor(x => x.ConfirmNewPassword)
                .NotEmpty().WithMessage("Please confirm your new password.")
                .Equal(x => x.NewPassword).WithMessage("Passwords do not match.");
        }
    }

    // ── Handler ───────────────────────────────────────────────────────────────────

    public sealed class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, MessageResponse>
    {
        private readonly IUserRepository _users;
        private readonly IPasswordHasher _hasher;
        private readonly ICurrentUserService _currentUser;

        public ChangePasswordCommandHandler(
            IUserRepository users,
            IPasswordHasher hasher,
            ICurrentUserService currentUser)
        {
            _users = users;
            _hasher = hasher;
            _currentUser = currentUser;
        }

        public async Task<MessageResponse> Handle(ChangePasswordCommand command, CancellationToken ct)
        {
            var user = await _users.GetByIdAsync(_currentUser.UserId, ct)
                ?? throw new NotFoundException(nameof(Domain.Entities.User), _currentUser.UserId);

            // Verify current password before allowing change
            if (!_hasher.Verify(command.CurrentPassword, user.PasswordHash))
                throw new UnauthorizedException("Current password is incorrect.");

            user.UpdatePasswordHash(_hasher.Hash(command.NewPassword));

            // Invalidate all existing sessions — force re-login on all devices
            user.RevokeRefreshToken();

            await _users.SaveChangesAsync(ct);

            return new MessageResponse("Password changed successfully. Please log in again.");
        }
    }
}
