using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;
using MediatR;
using CyberSphere.Domain.Interfaces.Repositories;
using CyberSphere.Application.Features.Sessions.DTOs;


namespace CyberSphere.Application.Features.Admin.Queries.GetAllSessions
{
    // ═══════════════════════════════════════════════════════════════════════════════
    // Admin: all platform sessions (paged)
    // ═══════════════════════════════════════════════════════════════════════════════

    public sealed record AdminGetAllSessionsQuery(
        int Page = 1,
        int PageSize = 20
    ) : IRequest<PagedAdminSessionsResponse>;

    public sealed record PagedAdminSessionsResponse(
        IReadOnlyList<AdminSessionResponse> Items,
        int Page, int PageSize, int TotalCount, int TotalPages,
        bool HasNextPage, bool HasPreviousPage
    );

    public sealed class AdminGetAllSessionsQueryValidator : AbstractValidator<AdminGetAllSessionsQuery>
    {
        public AdminGetAllSessionsQueryValidator()
        {
            RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        }
    }

    public sealed class AdminGetAllSessionsQueryHandler
        : IRequestHandler<AdminGetAllSessionsQuery, PagedAdminSessionsResponse>
    {
        private readonly ISessionRepository _sessions;

        public AdminGetAllSessionsQueryHandler(ISessionRepository sessions) => _sessions = sessions;

        public async Task<PagedAdminSessionsResponse> Handle(
            AdminGetAllSessionsQuery query, CancellationToken ct)
        {
            var (items, totalCount) =
                await _sessions.GetAllPagedAsync(query.Page, query.PageSize, ct);

            var dtos = items.Select(s => new AdminSessionResponse(
                Id: s.Id,
                Type: s.Type.ToString(),
                Status: s.Status.ToString(),
                StartedAt: s.StartedAt,
                EndedAt: s.EndedAt,
                TerminationReason: s.TerminationReason,
                ServerName: s.Server.Name,
                ServerIpAddress: s.Server.IpAddress,
                User: new UserSessionInfo(s.User.Id, s.User.UserName, s.User.Email)
            )).ToList().AsReadOnly();

            var totalPages = (int)Math.Ceiling(totalCount / (double)query.PageSize);

            return new PagedAdminSessionsResponse(
                Items: dtos,
                Page: query.Page,
                PageSize: query.PageSize,
                TotalCount: totalCount,
                TotalPages: totalPages,
                HasNextPage: query.Page < totalPages,
                HasPreviousPage: query.Page > 1);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    // Admin: server status dashboard
    // ═══════════════════════════════════════════════════════════════════════════════

    public sealed record GetServerStatusQuery : IRequest<IReadOnlyList<ServerStatusResponse>>;

    public sealed class GetServerStatusQueryHandler
        : IRequestHandler<GetServerStatusQuery, IReadOnlyList<ServerStatusResponse>>
    {
        private readonly IServerRepository _servers;

        public GetServerStatusQueryHandler(IServerRepository servers) => _servers = servers;

        public async Task<IReadOnlyList<ServerStatusResponse>> Handle(
            GetServerStatusQuery _, CancellationToken ct)
        {
            var servers = await _servers.GetAllAsync(ct);

            return servers
                .Select(s => new ServerStatusResponse(
                    Id: s.Id,
                    Name: s.Name,
                    IpAddress: s.IpAddress,
                    SshPort: s.SshPort,
                    Type: s.Type.ToString(),
                    Status: s.Status.ToString(),
                    ActiveSessions: s.ActiveSessions,
                    MaxSessions: s.MaxSessions,
                    AvailableSlots: Math.Max(0, s.MaxSessions - s.ActiveSessions),
                    LastHealthCheckAt: s.LastHealthCheckAt))
                .ToList()
                .AsReadOnly();
        }
    }
}
