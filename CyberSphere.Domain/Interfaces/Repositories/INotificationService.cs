using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CyberSphere.Domain.Interfaces.Repositories
{

    /// <summary>
    /// Application-layer service for creating and dispatching notifications.
    /// Implementations live in Infrastructure so they can access INotificationRepository.
    /// Handlers call this service instead of newing Notification directly,
    /// keeping notification content (titles, messages) in one place.
    /// </summary>
    public interface INotificationService
    {
        // ── Tool lifecycle ────────────────────────────────────────────────────────

        Task NotifyToolApprovedAsync(Guid ownerId, string toolName, Guid toolId, CancellationToken ct = default);
        Task NotifyToolRejectedAsync(Guid ownerId, string toolName, Guid toolId, string? adminNotes, CancellationToken ct = default);
        Task NotifyToolResubmittedAsync(Guid adminUserId, string toolName, Guid toolId, CancellationToken ct = default);

        // ── Ratings ───────────────────────────────────────────────────────────────

        Task NotifyNewRatingAsync(Guid toolOwnerId, string raterUserName, string toolName, int stars, Guid toolId, CancellationToken ct = default);

        // ── System ────────────────────────────────────────────────────────────────

        Task NotifyWelcomeAsync(Guid userId, string userName, CancellationToken ct = default);
        Task NotifySystemAnnouncementAsync(Guid userId, string title, string message, CancellationToken ct = default);
    }
}
