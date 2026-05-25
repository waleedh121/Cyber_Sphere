using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CyberSphere.Domain.Enums
{
    /// <summary>
    /// Describes the event that triggered this notification.
    /// Used by the frontend to render the correct icon and message template.
    /// </summary>
    public enum NotificationType
    {
        // ── Tool submission lifecycle ─────────────────────────────────────────────
        ToolApproved = 0,   // owner notified when admin approves their tool
        ToolRejected = 1,   // owner notified when admin rejects their tool
        ToolResubmitted = 2,   // admin notified when owner resubmits a rejected tool

        // ── Ratings ───────────────────────────────────────────────────────────────
        NewRatingReceived = 3,  // owner notified when someone rates their tool

        // ── System ────────────────────────────────────────────────────────────────
        SystemAnnouncement = 4, // broadcast from admin to a user or all users
        WelcomeMessage = 5  // sent on registration
    }

}
