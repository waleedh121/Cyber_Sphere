using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CyberSphere.Application.Features.Auth.DTOs;
using CyberSphere.Domain.Interfaces.Repositories;
using CyberSphere.Domain.Interfaces.Services;
using MediatR;
using FluentValidation;

namespace CyberSphere.Application.Features.Auth.Commands.ForgotPassword
{

    // ─── Command ─────────────────────────────────────────────────────────────────

    public sealed record ForgotPasswordCommand(string Email) : IRequest<MessageResponse>;

    // ─── Validator ───────────────────────────────────────────────────────────────

    public sealed class ForgotPasswordCommandValidator : AbstractValidator<ForgotPasswordCommand>
    {
        public ForgotPasswordCommandValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("A valid email address is required.");
        }
    }

    // ─── Handler ─────────────────────────────────────────────────────────────────

    public sealed class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, MessageResponse>
    {
        private readonly IUserRepository _users;
        private readonly IEmailService _email;

        public ForgotPasswordCommandHandler(IUserRepository users, IEmailService email)
        {
            _users = users;
            _email = email;
        }

        public async Task<MessageResponse> Handle(ForgotPasswordCommand cmd, CancellationToken ct)
        {
            // Always return the same message regardless of whether the email exists.
            // This prevents user enumeration attacks.
            const string safeResponse = "If that email is registered, you will receive a reset link shortly.";

            var user = await _users.GetByEmailAsync(cmd.Email, ct);
            if (user is null)
                return new MessageResponse(safeResponse);

            // Generate a short-lived token (15 minutes)
            var token = GenerateSecureToken();
            user.SetPasswordResetToken(token, DateTime.UtcNow.AddMinutes(15));
            await _users.SaveChangesAsync(ct);

            await _email.SendPasswordResetEmailAsync(user.Email, user.UserName, token, ct);

            return new MessageResponse(safeResponse);
        }

        // Cryptographically random, URL-safe token
        private static string GenerateSecureToken()
        {
            var bytes = new byte[32];
            System.Security.Cryptography.RandomNumberGenerator.Fill(bytes);
            return Convert.ToBase64String(bytes)
                .Replace('+', '-')
                .Replace('/', '_')
                .TrimEnd('=');
        }
    }
}
