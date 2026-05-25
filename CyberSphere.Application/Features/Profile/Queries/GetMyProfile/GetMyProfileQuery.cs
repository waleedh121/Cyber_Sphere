using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CyberSphere.Application.Exceptions;
using CyberSphere.Application.Features.Profile.DTOs;
using CyberSphere.Domain.Interfaces.Repositories;
using MediatR;

namespace CyberSphere.Application.Features.Profile.Queries.GetMyProfile;


    // ── Query ─────────────────────────────────────────────────────────────────────

    public sealed record GetMyProfileQuery : IRequest<MyProfileResponse>;

    // ── Handler ───────────────────────────────────────────────────────────────────

    public sealed class GetMyProfileQueryHandler : IRequestHandler<GetMyProfileQuery, MyProfileResponse>
    {
        private readonly IUserRepository _users;
        private readonly ICurrentUserService _currentUser;

        public GetMyProfileQueryHandler(IUserRepository users, ICurrentUserService currentUser)
        {
            _users = users;
            _currentUser = currentUser;
        }

        public async Task<MyProfileResponse> Handle(GetMyProfileQuery _, CancellationToken ct)
        {
            var user = await _users.GetByIdAsync(_currentUser.UserId, ct)
                ?? throw new NotFoundException(nameof(Domain.Entities.User), _currentUser.UserId);

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

