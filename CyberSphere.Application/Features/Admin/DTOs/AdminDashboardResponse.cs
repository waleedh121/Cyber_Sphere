using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CyberSphere.Application.Features.Admin.DTOs
{

    // ── Admin dashboard ───────────────────────────────────────────────────────────

    public sealed record AdminDashboardResponse(
        UserStats Users,
        ToolStats Tools,
        SessionStats Sessions,
        RatingStats Ratings,
        AiStats Ai,
        RecentActivity RecentActivity
    );

    public sealed record UserStats(
        int TotalUsers,
        int NewUsersLast7Days,
        int NewUsersLast30Days
    );

    public sealed record ToolStats(
        int TotalTools,
        int ApprovedTools,
        int PendingTools,
        int RejectedTools,
        int TotalContributors
    );

    public sealed record SessionStats(
        int TotalSessions,
        int ActiveSessions,
        int ScanSessions,
        int CliSessions,
        int SandboxSessions
    );

    public sealed record RatingStats(
        int TotalRatings,
        decimal OverallAverageRating,
        int TotalUsageCount
    );

    public sealed record AiStats(
        int TotalAiRequests,
        int TotalAiSessions,
        int TotalAiUsers
    );

    public sealed record RecentActivity(
        IReadOnlyList<RecentToolSubmission> RecentSubmissions,
        IReadOnlyList<RecentSessionActivity> RecentSessions
    );

    public sealed record RecentToolSubmission(
        Guid ToolId,
        string ToolName,
        string OwnerUserName,
        string Status,
        DateTime SubmittedAt
    );

    public sealed record RecentSessionActivity(
        Guid SessionId,
        string UserName,
        string SessionType,
        string Status,
        DateTime StartedAt
    );

}
