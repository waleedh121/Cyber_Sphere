using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CyberSphere.Application.Features.Profile.DTOs
{

    // ── Responses ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Private profile returned to the authenticated user only.
    /// Includes email and role — never exposed on the public endpoint.
    /// </summary>
    public sealed record MyProfileResponse(
        Guid Id,
        string UserName,
        string Email,
        string Role,
        string? Bio,
        string? ProfilePictureUrl,
        string? GitHubUrl,
        int TotalToolsUploaded,
        DateTime CreatedAt
    );

    /// <summary>
    /// Public profile visible to any visitor.
    /// Email and role are intentionally excluded.
    /// ApprovedTools contains only Approved tools — Pending and Rejected are hidden.
    /// </summary>
    public sealed record PublicProfileResponse(
        Guid Id,
        string UserName,
        string? Bio,
        string? ProfilePictureUrl,
        string? GitHubUrl,
        int TotalToolsUploaded,
        DateTime CreatedAt,
        IReadOnlyList<PublicToolSummary> ApprovedTools
    );

    /// <summary>
    /// Lightweight tool card shown on a contributor's public profile page.
    /// Category.Name is resolved via the Tool → Category navigation property —
    /// never a raw Guid stub.
    /// </summary>
    public sealed record PublicToolSummary(
        Guid Id,
        string Name,
        string Description,
        string CategoryName,
        string CategorySlug,
        string DifficultyLevel,
        decimal AverageRating,
        int RatingCount,
        int UsageCount,
        string? GitHubUrl,
        IReadOnlyList<string> Tags,
        DateTime CreatedAt
    );

    // ── Requests ──────────────────────────────────────────────────────────────────

    public sealed record UpdateProfileRequest(
        string? Bio,
        string? ProfilePictureUrl,
        string? GitHubUrl
    );

    public sealed record ChangePasswordRequest(
        string CurrentPassword,
        string NewPassword,
        string ConfirmNewPassword
    );


}
