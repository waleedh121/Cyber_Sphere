using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CyberSphere.Domain.Entities;

namespace CyberSphere.Domain.Interfaces.Repositories
{
    public interface INotificationRepository
    {
        Task<Notification?> GetByIdAsync(Guid id, CancellationToken ct = default);

        /// <summary>Paged notifications for a user, newest first.</summary>
        Task<(IReadOnlyList<Notification> Items, int TotalCount)> GetByUserIdAsync(
            Guid userId, int page, int pageSize, bool unreadOnly, CancellationToken ct = default);

        /// <summary>Count of unread notifications for the badge indicator.</summary>
        Task<int> GetUnreadCountAsync(Guid userId, CancellationToken ct = default);

        Task AddAsync(Notification notification, CancellationToken ct = default);
        Task AddRangeAsync(IEnumerable<Notification> notifications, CancellationToken ct = default);
        void Update(Notification notification);
        void Delete(Notification notification);
        Task SaveChangesAsync(CancellationToken ct = default);
    }

}
