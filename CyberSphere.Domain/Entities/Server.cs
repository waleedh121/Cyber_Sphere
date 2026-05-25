using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CyberSphere.Domain.Enums;

namespace CyberSphere.Domain.Entities
{

    /// <summary>
    /// Represents a single VM in the infrastructure pool.
    /// The backend tracks capacity and routes sessions here;
    /// actual execution happens inside the VM over SSH.
    /// </summary>
    public sealed class Server
    {
        public Guid Id { get; private set; }
        public string Name { get; private set; } = string.Empty;
        public string IpAddress { get; private set; } = string.Empty;
        public int SshPort { get; private set; }
        public ServerType Type { get; private set; }
        public ServerStatus Status { get; private set; }
        public int MaxSessions { get; private set; }
        public int ActiveSessions { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime? LastHealthCheckAt { get; private set; }

        // ── Navigation ────────────────────────────────────────────────────────────
        public ICollection<Session> Sessions { get; private set; } = new List<Session>();

        private Server() { }

        public static Server Create(
            string name,
            string ipAddress,
            ServerType type,
            int maxSessions = 10,
            int sshPort = 22)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(ipAddress);

            return new Server
            {
                Id = Guid.NewGuid(),
                Name = name.Trim(),
                IpAddress = ipAddress.Trim(),
                SshPort = sshPort,
                Type = type,
                Status = ServerStatus.Online,
                MaxSessions = maxSessions,
                ActiveSessions = 0,
                CreatedAt = DateTime.UtcNow
            };
        }

        // ── Capacity ──────────────────────────────────────────────────────────────

        public bool HasCapacity => Status == ServerStatus.Online &&
                                   ActiveSessions < MaxSessions;

        /// <summary>
        /// Reserves a slot atomically. Called by VmAssignmentService before SaveChanges.
        /// Throws if the server is full — the caller must have checked HasCapacity first.
        /// </summary>
        public void ReserveSlot()
        {
            if (!HasCapacity)
                throw new InvalidOperationException(
                    $"Server '{Name}' has no available capacity (Active={ActiveSessions}, Max={MaxSessions}).");

            ActiveSessions++;
        }

        /// <summary>Releases a slot when a session ends or fails.</summary>
        public void ReleaseSlot() =>
            ActiveSessions = Math.Max(0, ActiveSessions - 1);

        // ── Administration ────────────────────────────────────────────────────────

        public void SetOnline() => Status = ServerStatus.Online;
        public void SetOffline() => Status = ServerStatus.Offline;
        public void SetMaintenance() => Status = ServerStatus.Maintenance;

        public void RecordHealthCheck() =>
            LastHealthCheckAt = DateTime.UtcNow;

        public void UpdateConfig(string name, int maxSessions)
        {
            Name = name.Trim();
            MaxSessions = maxSessions;
        }
    }
}
