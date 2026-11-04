using System.Collections.Generic;
using System.Threading.Tasks;
using ApartmentResidenceManagement.Domain.Entities;

namespace ApartmentResidenceManagement.Domain.Interfaces;

public interface INotificationRepository : IRepository<Notification>
{
    Task<IEnumerable<NotificationRecipient>> GetUserNotificationsAsync(int userAccountId);
    Task<int> GetUnreadCountAsync(int userAccountId);
    Task<bool> MarkAsReadAsync(int notificationId, int userAccountId);
    Task<bool> MarkAllAsReadAsync(int userAccountId);
    Task AddRecipientsAsync(IEnumerable<NotificationRecipient> recipients);
    Task<Notification?> GetWithRecipientsAsync(int notificationId);
    Task<IEnumerable<NotificationRecipient>> GetAdminApprovalNotificationsAsync(int adminAccountId);
    Task<IEnumerable<Notification>> GetSentAnnouncementsAsync();
}
