using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CyberSphere.Application.Features.Marketplace.DTOs
{

    // ── Owner (always embedded — never optional) ──────────────────────────────────

    public sealed record OwnerProfileDto(
        string UserName,
        string? Bio,
        string? ProfilePictureUrl,
        int TotalToolsUploaded,
        string? GitHubUrl
    );

    // ── Marketplace list card ─────────────────────────────────────────────────────

    public sealed record ToolMarketplaceDto(
        Guid Id,
        string Name,
        string Description,
        string CategoryName,
        string DifficultyLevel,
        int UsageCount,
        decimal AverageRating,
        int RatingCount,
        IReadOnlyList<string> Tags,
        string? GitHubUrl,
        DateTime CreatedAt,
        OwnerProfileDto Owner           // mandatory — never null
    );

    // ── Full tool detail page ─────────────────────────────────────────────────────

    public sealed record ToolDetailDto(
        Guid Id,
        string Name,
        string Description,
        string CategoryName,
        Guid CategoryId,
        string DifficultyLevel,
        int UsageCount,
        decimal AverageRating,
        int RatingCount,
        IReadOnlyList<string> Tags,
        IReadOnlyList<ToolCommandDto> Commands,
        string? GitHubUrl,
        DateTime CreatedAt,
        OwnerProfileDto Owner           // mandatory — never null
    );

    public sealed record ToolCommandDto(
        Guid Id,
        string Name,
        string Description,
        string Syntax,
        string? Example
    );

    // ── Paged response wrapper ────────────────────────────────────────────────────

    public sealed record PagedToolsResponse(
        IReadOnlyList<ToolMarketplaceDto> Items,
        int Page,
        int PageSize,
        int TotalCount,
        int TotalPages,
        bool HasPreviousPage,
        bool HasNextPage
    );

    // ── Category ──────────────────────────────────────────────────────────────────

    public sealed record CategoryDto(
        Guid Id,
        string Name,
        string Slug,
        string? Description,
        int ToolCount
    );
}
