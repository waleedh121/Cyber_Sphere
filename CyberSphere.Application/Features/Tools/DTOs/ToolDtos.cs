using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CyberSphere.Domain.Enums;

namespace CyberSphere.Application.Features.Tools.DTOs
{
    // ── Requests ──────────────────────────────────────────────────────────────────

    public sealed record SubmitToolRequest(
        string Name,
        string Description,
        Guid CategoryId,
        DifficultyLevel DifficultyLevel,
        string? GitHubUrl,
        List<string>? Tags,
        List<SubmitCommandRequest>? Commands
    );

    public sealed record SubmitCommandRequest(
        string Name,
        string Description,
        string Syntax,
        string? Example
    );

    public sealed record UpdateToolRequest(
        string Name,
        string Description,
        Guid CategoryId,
        DifficultyLevel DifficultyLevel,
        string? GitHubUrl,
        List<string>? Tags
    );

    public sealed record ReviewToolRequest(
        ReviewDecision Decision,
        string? Notes
    );

    // ── Responses ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Returned to the owner after submit / update.
    /// Includes full status so the owner can track where they are in the review cycle.
    /// </summary>
    public sealed record ToolSubmissionResponse(
        Guid Id,
        string Name,
        string Description,
        string CategoryName,
        string DifficultyLevel,
        string Status,
        string? GitHubUrl,
        IReadOnlyList<string> Tags,
        IReadOnlyList<ToolCommandResponse> Commands,
        DateTime CreatedAt
    );

    public sealed record ToolCommandResponse(
        Guid Id,
        string Name,
        string Description,
        string Syntax,
        string? Example
    );

    /// <summary>Admin-facing pending tool card — includes owner info for context.</summary>
    public sealed record PendingToolResponse(
        Guid Id,
        string Name,
        string Description,
        string CategoryName,
        string DifficultyLevel,
        string? GitHubUrl,
        IReadOnlyList<string> Tags,
        DateTime CreatedAt,
        PendingToolOwnerInfo Owner,
        ToolReviewHistoryEntry? LastReview
    );

    public sealed record PendingToolOwnerInfo(
        Guid Id,
        string UserName,
        string Email,
        int TotalToolsUploaded
    );

    public sealed record ToolReviewHistoryEntry(
        string Decision,
        string? Notes,
        string AdminUserName,
        DateTime ReviewedAt
    );

    public sealed record PagedPendingToolsResponse(
        IReadOnlyList<PendingToolResponse> Items,
        int Page,
        int PageSize,
        int TotalCount,
        int TotalPages,
        bool HasNextPage,
        bool HasPreviousPage
    );

    /// <summary>Returned to the admin after a review decision is saved.</summary>
    public sealed record ReviewDecisionResponse(
        Guid ToolId,
        string ToolName,
        string Decision,
        string? Notes,
        DateTime ReviewedAt
    );
}
