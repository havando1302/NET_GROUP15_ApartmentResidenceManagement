using System.Collections.Generic;
using System.Threading.Tasks;
using ApartmentResidenceManagement.Domain.Entities;
using ApartmentResidenceManagement.Domain.Enums;

namespace ApartmentResidenceManagement.Application.Services;

public interface IAppNotificationService
{
    Task<IEnumerable<NotificationRecipient>> GetUserNotificationsAsync(int userAccountId);
    Task<int> GetUnreadCountAsync(int userAccountId);
    Task<bool> MarkAsReadAsync(int notificationId, int userAccountId);
    Task<bool> MarkAllAsReadAsync(int userAccountId);

    /// <summary>
    /// Tạo thông báo từ Ban quản lý gửi đến cư dân (chỉ cư dân có tài khoản đang hoạt động).
    /// </summary>
    Task<Notification> CreateAnnouncementAsync(
        int adminAccountId,
        string title,
        string content,
        NotificationType type,
        NotificationPriority priority,
        TargetAudienceType targetAudience,
        List<int>? specificResidentIds = null);

    /// <summary>
    /// Gửi thông báo cho Ban quản lý khi có đơn duyệt mới gửi từ Cư dân.
    /// </summary>
    Task<Notification?> NotifyAdminNewApprovalRequestAsync(
        int requesterAccountId,
        string entityType,
        int entityId,
        string title,
        string content,
        NotificationPriority priority = NotificationPriority.Important);

    /// <summary>
    /// Gửi thông báo kết quả phê duyệt đơn cho Cư dân.
    /// </summary>
    Task<Notification?> NotifyResidentApprovalResultAsync(
        int residentId,
        string entityType,
        int entityId,
        bool isApproved,
        string title,
        string content);
}
