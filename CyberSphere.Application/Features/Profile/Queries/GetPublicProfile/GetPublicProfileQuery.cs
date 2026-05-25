using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CyberSphere.Application.Exceptions;
using CyberSphere.Application.Features.Profile.DTOs;
using CyberSphere.Domain.Enums;
using CyberSphere.Domain.Interfaces.Repositories;
using MediatR;

namespace CyberSphere.Application.Features.Profile.Queries.GetPublicProfile
{
    // ── Query ─────────────────────────────────────────────────────────────────────

    public sealed record GetPublicProfileQuery(string UserName) : IRequest<PublicProfileResponse>;

    // ── Handler ───────────────────────────────────────────────────────────────────

    public sealed class GetPublicProfileQueryHandler
        : IRequestHandler<GetPublicProfileQuery, PublicProfileResponse>
    {
        private readonly IUserRepository _users;

        public GetPublicProfileQueryHandler(IUserRepository users) => _users = users;

        public async Task<PublicProfileResponse> Handle(
            GetPublicProfileQuery query, CancellationToken ct)
        {
            // GetByUserNameWithToolsAsync includes Tools + Category navigation
            // so Category.Name is available without a second query
            var user = await _users.GetByUserNameWithToolsAsync(query.UserName, ct)
                ?? throw new NotFoundException(nameof(Domain.Entities.User), query.UserName);

            var approvedTools = user.Tools
                .Where(t => t.Status == ToolStatus.Approved)
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => new PublicToolSummary(
                    Id: t.Id,
                    Name: t.Name,
                    Description: t.Description,
                    CategoryName: t.Category.Name,     // resolved via ThenInclude — never a stub
                    CategorySlug: t.Category.Slug,
                    DifficultyLevel: t.DifficultyLevel.ToString(),
                    AverageRating: Math.Round(t.AverageRating, 2),
                    RatingCount: t.RatingCount,
                    UsageCount: t.UsageCount,
                    GitHubUrl: t.GitHubUrl,
                    Tags: t.GetTags(),
                    CreatedAt: t.CreatedAt
                ))
                .ToList()
                .AsReadOnly();

            return new PublicProfileResponse(
                Id: user.Id,
                UserName: user.UserName,
                Bio: user.Bio,
                ProfilePictureUrl: user.ProfilePictureUrl,
                GitHubUrl: user.GitHubUrl,
                TotalToolsUploaded: user.TotalToolsUploaded,
                CreatedAt: user.CreatedAt,
                ApprovedTools: approvedTools
            );
        }
    }

}
