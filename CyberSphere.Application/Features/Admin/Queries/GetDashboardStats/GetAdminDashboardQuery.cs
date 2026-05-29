using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CyberSphere.Application.Features.Admin.DTOs;
using CyberSphere.Domain.Enums;
using CyberSphere.Domain.Interfaces.Repositories;
using MediatR;

namespace CyberSphere.Application.Features.Admin.Queries.GetDashboardStats
{

    public sealed record GetAdminDashboardQuery : IRequest<AdminDashboardResponse>;

    // ── Handler ───────────────────────────────────────────────────────────────────

    public sealed class GetAdminDashboardQueryHandler
        : IRequestHandler<GetAdminDashboardQuery, AdminDashboardResponse>
    {
        private readonly IUserRepository _users;
        private readonly IToolRepository _tools;
        private readonly ISessionRepository _sessions;
        private readonly IRatingRepository _ratings;
        private readonly IUserAiStatsRepository _aiStats;

        public GetAdminDashboardQueryHandler(
            IUserRepository users,
            IToolRepository tools,
            ISessionRepository sessions,
            IRatingRepository ratings,
            IUserAiStatsRepository aiStats)
        {
            _users = users;
            _tools = tools;
            _sessions = sessions;
            _ratings = ratings;
            _aiStats = aiStats;
        }

        public async Task<AdminDashboardResponse> Handle(
            GetAdminDashboardQuery _, CancellationToken ct)
        {

            var allUsers = await _users.GetAllForDashboardAsync(ct);

            var allTools = await _tools.GetAllForStatsAsync(ct);

            var allSessions = await _sessions.GetAllForDashboardAsync(ct);

            var allAiStats = await _aiStats.GetAllAsync(ct);

            // ── User stats ────────────────────────────────────────────────────────
            var now = DateTime.UtcNow;
            var ago7 = now.AddDays(-7);
            var ago30 = now.AddDays(-30);

            var userStats = new UserStats(
                TotalUsers: allUsers.Count,
                NewUsersLast7Days: allUsers.Count(u => u.CreatedAt >= ago7),
                NewUsersLast30Days: allUsers.Count(u => u.CreatedAt >= ago30)
            );

            // ── Tool stats ────────────────────────────────────────────────────────
            var approved = allTools.Where(t => t.Status == ToolStatus.Approved).ToList();
            var pending = allTools.Where(t => t.Status == ToolStatus.Pending).ToList();
            var rejected = allTools.Where(t => t.Status == ToolStatus.Rejected).ToList();

            var toolStats = new ToolStats(
                TotalTools: allTools.Count,
                ApprovedTools: approved.Count,
                PendingTools: pending.Count,
                RejectedTools: rejected.Count,
                TotalContributors: allTools.Select(t => t.OwnerId).Distinct().Count()
            );

            // ── Session stats ─────────────────────────────────────────────────────
            var sessionStats = new SessionStats(
                TotalSessions: allSessions.Count,
                ActiveSessions: allSessions.Count(s => s.Status == SessionStatus.Active),
                ScanSessions: allSessions.Count(s => s.Type == SessionType.Scanning),
                CliSessions: allSessions.Count(s => s.Type == SessionType.CliTools),
                SandboxSessions: allSessions.Count(s => s.Type == SessionType.Sandbox)
            );

            // ── Rating stats ──────────────────────────────────────────────────────
            var totalRatings = approved.Sum(t => t.RatingCount);
            var overallAvg = approved.Count > 0 && totalRatings > 0
                ? Math.Round(approved.Where(t => t.RatingCount > 0).Average(t => t.AverageRating), 2)
                : 0m;

            var ratingStats = new RatingStats(
                TotalRatings: totalRatings,
                OverallAverageRating: overallAvg,
                TotalUsageCount: approved.Sum(t => t.UsageCount)
            );

            // ── AI stats ──────────────────────────────────────────────────────────
            var aiStatsResponse = new AiStats(
                TotalAiRequests: allAiStats.Sum(s => s.TotalRequests),
                TotalAiSessions: allAiStats.Sum(s => s.TotalSessions),
                TotalAiUsers: allAiStats.Count
            );

            // ── Recent activity (last 10 of each) ────────────────────────────────
            var recentSubmissions = allTools
                .OrderByDescending(t => t.CreatedAt)
                .Take(10)
                .Select(t => new RecentToolSubmission(
                    t.Id, t.Name, t.Owner.UserName, t.Status.ToString(), t.CreatedAt))
                .ToList()
                .AsReadOnly();

            var recentSessions = allSessions
                .OrderByDescending(s => s.StartedAt)
                .Take(10)
                .Select(s => new RecentSessionActivity(
                    s.Id, s.User.UserName, s.Type.ToString(), s.Status.ToString(), s.StartedAt))
                .ToList()
                .AsReadOnly();

            return new AdminDashboardResponse(
                Users: userStats,
                Tools: toolStats,
                Sessions: sessionStats,
                Ratings: ratingStats,
                Ai: aiStatsResponse,
                RecentActivity: new RecentActivity(recentSubmissions, recentSessions)
            );
        }
    }

}
