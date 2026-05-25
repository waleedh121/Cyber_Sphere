using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CyberSphere.Application.Exceptions;
using CyberSphere.Application.Features.Profile.DTOs;
using CyberSphere.Domain.Interfaces.Repositories;
using FluentValidation;
using MediatR;

namespace CyberSphere.Application.Features.Profile.Commands.UpdateProfile
{

    // ── Command ───────────────────────────────────────────────────────────────────

    public sealed record UpdateProfileCommand(
        string? Bio,
        string? ProfilePictureUrl,
        string? GitHubUrl
    ) : IRequest<MyProfileResponse>;

    // ── Validator ─────────────────────────────────────────────────────────────────

    public sealed class UpdateProfileCommandValidator : AbstractValidator<UpdateProfileCommand>
    {
        public UpdateProfileCommandValidator()
        {
            RuleFor(x => x.Bio)
                .MaximumLength(500).WithMessage("Bio must not exceed 500 characters.")
                .When(x => x.Bio is not null);

            RuleFor(x => x.ProfilePictureUrl)
                .MaximumLength(1000).WithMessage("Profile picture URL must not exceed 1000 characters.")
                .Must(url => Uri.TryCreate(url, UriKind.Absolute, out _))
                .WithMessage("Profile picture URL must be a valid absolute URL.")
                .When(x => !string.IsNullOrWhiteSpace(x.ProfilePictureUrl));

            RuleFor(x => x.GitHubUrl)
                .MaximumLength(500).WithMessage("GitHub URL must not exceed 500 characters.")
                .Must(url => Uri.TryCreate(url, UriKind.Absolute, out var u)
                             && (u.Host == "github.com" || u.Host == "www.github.com"))
                .WithMessage("GitHub URL must be a valid github.com URL.")
                .When(x => !string.IsNullOrWhiteSpace(x.GitHubUrl));
        }
    }

    // ── Handler ───────────────────────────────────────────────────────────────────

    public sealed class UpdateProfileCommandHandler
        : IRequestHandler<UpdateProfileCommand, MyProfileResponse>
    {
        private readonly IUserRepository _users;
        private readonly ICurrentUserService _currentUser;

        public UpdateProfileCommandHandler(IUserRepository users, ICurrentUserService currentUser)
        {
            _users = users;
            _currentUser = currentUser;
        }

        public async Task<MyProfileResponse> Handle(UpdateProfileCommand command, CancellationToken ct)
        {
            var user = await _users.GetByIdAsync(_currentUser.UserId, ct)
                ?? throw new NotFoundException(nameof(Domain.Entities.User), _currentUser.UserId);

            user.UpdateProfile(command.Bio, command.ProfilePictureUrl, command.GitHubUrl);

            await _users.SaveChangesAsync(ct);

            return new MyProfileResponse(
                Id: user.Id,
                UserName: user.UserName,
                Email: user.Email,
                Role: user.Role.ToString(),
                Bio: user.Bio,
                ProfilePictureUrl: user.ProfilePictureUrl,
                GitHubUrl: user.GitHubUrl,
                TotalToolsUploaded: user.TotalToolsUploaded,
                CreatedAt: user.CreatedAt
            );
        }
    }
}
