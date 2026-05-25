using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CyberSphere.Domain.Enums;


namespace CyberSphere.Domain.Entities
{

    /// <summary>
    /// Tracks one user's active or historical connection to a VM.
    /// The backend creates the session and routes the user to the VM;
    /// all tool execution, scanning, and sandboxing happen inside the VM.
    /// This entity is intentionally kept separate from AiSession.
    /// </summary>
    public sealed class Session
    {
        public Guid Id { get; private set; }
        public Guid UserId { get; private set; }
        public Guid ServerId { get; private set; }
        public SessionType Type { get; private set; }
        public SessionStatus Status { get; private set; }

        // ── VM connection info (populated on successful start) ────────────────────
        /// <summary>
        /// The SSH connection token / one-time ticket issued to the user.
        /// Frontend uses this to open a terminal/iframe to the VM.
        /// Cleared when the session ends.
        /// </summary>
        public string? ConnectionToken { get; private set; }
        public string? VmIpAddress { get; private set; }
        public int? VmPort { get; private set; }

        // ── Lifecycle ─────────────────────────────────────────────────────────────
        public DateTime StartedAt { get; private set; }
        public DateTime? EndedAt { get; private set; }

        /// <summary>Reason populated when session is Terminated or Failed.</summary>
        public string? TerminationReason { get; private set; }

        // ── Navigation ────────────────────────────────────────────────────────────
        public User User { get; private set; } = null!;
        public Server Server { get; private set; } = null!;

        private Session() { }

        public static Session Create(Guid userId, Guid serverId, SessionType type)
        {
            return new Session
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                ServerId = serverId,
                Type = type,
                Status = SessionStatus.Active,
                StartedAt = DateTime.UtcNow
            };
        }

        // ── Lifecycle methods ─────────────────────────────────────────────────────

        /// <summary>Stamped by VmService after SSH handshake succeeds.</summary>
        public void SetConnectionInfo(string connectionToken, string vmIpAddress, int vmPort)
        {
            ConnectionToken = connectionToken;
            VmIpAddress = vmIpAddress;
            VmPort = vmPort;
        }

        public void MarkFailed(string reason)
        {
            Status = SessionStatus.Failed;
            EndedAt = DateTime.UtcNow;
            TerminationReason = reason;
            ClearConnectionInfo();
        }

        public void End()
        {
            if (Status == SessionStatus.Active)
            {
                Status = SessionStatus.Ended;
                EndedAt = DateTime.UtcNow;
                ClearConnectionInfo();
            }
        }

        public void Terminate(string reason)
        {
            Status = SessionStatus.Terminated;
            EndedAt = DateTime.UtcNow;
            TerminationReason = reason;
            ClearConnectionInfo();
        }

        public bool IsActive => Status == SessionStatus.Active;

        private void ClearConnectionInfo()
        {
            ConnectionToken = null;
            // Keep VmIpAddress/VmPort for audit — clear only the auth token
        }
    }
}
