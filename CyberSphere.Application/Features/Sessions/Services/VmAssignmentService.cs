using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CyberSphere.Application.Exceptions;
using CyberSphere.Domain.Enums;
using CyberSphere.Domain.Entities;
using CyberSphere.Domain.Interfaces.Repositories;

namespace CyberSphere.Application.Features.Sessions.Services
{

    /// <summary>
    /// Selects the best available VM server for a given session type.
    /// Sits in the Application layer because the routing decision is business logic —
    /// it only reads from IServerRepository (no SSH, no infrastructure concerns).
    ///
    /// Strategy: pick the Online server of the matching type with the most available slots.
    /// If no server is available, throws <see cref="ServiceUnavailableException"/>.
    /// </summary>
    public sealed class VmAssignmentService
    {
        private readonly IServerRepository _servers;

        public VmAssignmentService(IServerRepository servers) => _servers = servers;

        /// <summary>
        /// Maps <see cref="SessionType"/> → <see cref="ServerType"/> and finds the
        /// best available server. Returns the selected server with a reserved slot.
        /// Caller must call SaveChangesAsync after this returns.
        /// </summary>
        public async Task<Server> AssignServerAsync(SessionType sessionType, CancellationToken ct)
        {
            var serverType = MapSessionTypeToServerType(sessionType);

            var available = await _servers.GetAvailableByTypeAsync(serverType, ct);

            if (available.Count == 0)
                throw new ServiceUnavailableException(
                    $"No {sessionType} VM servers are currently available. " +
                    "All servers may be at capacity or offline. Please try again shortly.");

            // Pick server with the most headroom (list is already ordered by available slots desc)
            var selected = available[0];
            selected.ReserveSlot();   // increments ActiveSessions — must be saved by caller

            return selected;
        }

        // ── Mapping ───────────────────────────────────────────────────────────────

        private static ServerType MapSessionTypeToServerType(SessionType sessionType) =>
            sessionType switch
            {
                SessionType.Scanning => ServerType.Scanning,
                SessionType.CliTools => ServerType.CliTools,
                SessionType.Sandbox => ServerType.Sandbox,
                _ => throw new BadRequestException($"Session type '{sessionType}' is not supported.")
            };
    }
}
