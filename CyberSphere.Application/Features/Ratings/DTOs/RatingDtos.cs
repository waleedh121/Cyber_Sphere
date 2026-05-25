using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CyberSphere.Application.Features.Ratings.DTOs
{
    // ── Requests ──────────────────────────────────────────────────────────────────

    public sealed record RateToolRequest(int Stars, string? Comment);

    public sealed record UpdateRatingRequest(int Stars, string? Comment);

    // ── Responses ─────────────────────────────────────────────────────────────────

    public sealed record RatingResponse(
        Guid Id,
        Guid ToolId,
        string ToolName,
        int Stars,
        string? Comment,
        string UserName,
        DateTime CreatedAt,
        DateTime UpdatedAt
    );

    public sealed record PagedRatingsResponse(
        IReadOnlyList<RatingResponse> Items,
        int Page,
        int PageSize,
        int TotalCount,
        int TotalPages,
        bool HasNextPage,
        bool HasPreviousPage
    );

    // ── Tool statistics (owner + admin dashboard) ─────────────────────────────────

    public sealed record ToolStatsResponse(
        Guid ToolId,
        string ToolName,
        string Status,
        int UsageCount,
        decimal AverageRating,
        int RatingCount,
        RatingBreakdown RatingBreakdown
    );

    /// <summary>Count of ratings per star value for the histogram bar chart.</summary>
    public sealed record RatingBreakdown(
        int OneStar,
        int TwoStars,
        int ThreeStars,
        int FourStars,
        int FiveStars
    );

    // ── Admin platform-wide stats ─────────────────────────────────────────────────

    public sealed record PlatformToolStatsResponse(
        int TotalTools,
        int ApprovedTools,
        int PendingTools,
        int RejectedTools,
        int TotalRatings,
        int TotalUsageCount,
        decimal OverallAverageRating,
        IReadOnlyList<TopRatedToolResponse> TopRatedTools,
        IReadOnlyList<MostUsedToolResponse> MostUsedTools
    );

    public sealed record TopRatedToolResponse(
        Guid Id,
        string Name,
        string CategoryName,
        decimal AverageRating,
        int RatingCount
    );

    public sealed record MostUsedToolResponse(
        Guid Id,
        string Name,
        string CategoryName,
        int UsageCount
    );
}
