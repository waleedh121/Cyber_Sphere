using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CyberSphere.Domain.Interfaces.Repositories
{

    /// <summary>
    /// Gateway interface for all VM/SSH operations.
    /// Application layer depends only on this contract —
    /// it has zero knowledge of SSH.NET, credentials, or host keys.
    /// Implemented in Infrastructure/Services/VM/SshVmService.cs.
    /// </summary>
    public interface IVmService
    {
        /// <summary>
        /// Verifies the VM is reachable and accepting connections via SSH.
        /// Called before finalising a session assignment.
        /// </summary>
        Task<VmHealthResult> CheckHealthAsync(
            string ipAddress, int port, CancellationToken ct = default);

        /// <summary>
        /// Opens an SSH session to the VM and initialises the user's environment.
        /// Returns a connection token the frontend uses to establish its terminal channel.
        /// </summary>
        Task<VmConnectionResult> InitialiseSessionAsync(
            Guid sessionId,
            string ipAddress,
            int port,
            string userName,
            CancellationToken ct = default);

        /// <summary>
        /// Sends a graceful teardown command to the VM for the given session.
        /// The VM cleans up the user's workspace / process group.
        /// </summary>
        Task TerminateSessionAsync(
            Guid sessionId,
            string ipAddress,
            int port,
            CancellationToken ct = default);
    }

    // ── Value objects ─────────────────────────────────────────────────────────────

    public sealed record VmHealthResult(
        bool IsHealthy,
        string? ErrorMessage = null
    );

    public sealed record VmConnectionResult(
        bool Success,
        string? ConnectionToken = null,
        string? ErrorMessage = null
    );
}
