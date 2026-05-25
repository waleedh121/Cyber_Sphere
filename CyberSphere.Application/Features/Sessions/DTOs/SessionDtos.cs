using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CyberSphere.Domain.Enums;

namespace CyberSphere.Application.Features.Sessions.DTOs
{


    // ── Requests ──────────────────────────────────────────────────────────────────

    public sealed record StartSessionRequest(SessionType Type);

    public sealed record EndSessionRequest(string? Reason);

    // ── Responses ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Returned to the user after a session starts.
    /// Contains everything the frontend needs to open a VM terminal.
    /// </summary>
    public sealed record SessionResponse(
        Guid Id,
        string Type,
        string Status,
        string? ConnectionToken,
        string? VmIpAddress,
        int? VmPort,
        DateTime StartedAt,
        DateTime? EndedAt,
        ServerInfo Server
    );

    public sealed record ServerInfo(
        Guid Id,
        string Name,
        string Type
    );

    /// <summary>Summary card — no connection token exposed.</summary>
    public sealed record SessionSummaryResponse(
        Guid Id,
        string Type,
        string Status,
        DateTime StartedAt,
        DateTime? EndedAt,
        string? TerminationReason,
        string ServerName
    );

    public sealed record PagedSessionsResponse(
        IReadOnlyList<SessionSummaryResponse> Items,
        int Page,
        int PageSize,
        int TotalCount,
        int TotalPages,
        bool HasNextPage,
        bool HasPreviousPage
    );

    // ── Admin responses ───────────────────────────────────────────────────────────

    public sealed record AdminSessionResponse(
        Guid Id,
        string Type,
        string Status,
        DateTime StartedAt,
        DateTime? EndedAt,
        string? TerminationReason,
        string ServerName,
        string ServerIpAddress,
        UserSessionInfo User
    );

    public sealed record UserSessionInfo(
        Guid Id,
        string UserName,
        string Email
    );

    public sealed record ServerStatusResponse(
        Guid Id,
        string Name,
        string IpAddress,
        int SshPort,
        string Type,
        string Status,
        int ActiveSessions,
        int MaxSessions,
        int AvailableSlots,
        DateTime? LastHealthCheckAt
    );

    // ── Admin requests ────────────────────────────────────────────────────────────

    public sealed record TerminateSessionRequest(string? Reason);

    public sealed record SetServerStatusRequest(ServerStatus Status);
}
