using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CyberSphere.Domain.Enums
{

    /// <summary>
    /// Maps to the six platform sessions defined in the system architecture.
    /// Each type is routed to a different VM pool.
    /// </summary>
    public enum SessionType
    {
        Scanning = 1,   // Session 1 — Scanning VM
        CliTools = 3,   // Session 3 — CLI Tools VM
        Sandbox = 4,   // Session 4 — Sandbox VM
    }

    public enum SessionStatus
    {
        Active = 0,
        Ended = 1,
        Terminated = 2,   // Force-terminated by admin or system (e.g. timeout)
        Failed = 3    // VM assignment or SSH handshake failed
    }

    public enum ServerType
    {
        Scanning = 1,
        CliTools = 3,
        Sandbox = 4,
    }

    public enum ServerStatus
    {
        Online = 0,
        Offline = 1,
        Maintenance = 2
    }
}
