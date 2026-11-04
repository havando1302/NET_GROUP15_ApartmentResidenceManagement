using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApartmentResidenceManagement.Domain.Entities;
using ApartmentResidenceManagement.Domain.Enums;
using ApartmentResidenceManagement.Domain.Interfaces;

namespace ApartmentResidenceManagement.Application.Services;

public class AppNotificationService : IAppNotificationService
{
    private readonly IUnitOfWork _unitOfWork;

    public AppNotificationService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<IEnumerable<NotificationRecipient>> GetUserNotificationsAsync(int userAccountId)
    {
        return await _unitOfWork.Notifications.GetUserNotificationsAsync(userAccountId);
    }

    public async Task<int> GetUnreadCountAsync(int userAccountId)
    {
        return await _unitOfWork.Notifications.GetUnreadCountAsync(userAccountId);
    }

    public async Task<bool> MarkAsReadAsync(int notificationId, int userAccountId)
    {
        var result = await _unitOfWork.Notifications.MarkAsReadAsync(notificationId, userAccountId);
        if (result)
        {
            await _unitOfWork.CompleteAsync();
        }
        return result;
    }

    public async Task<bool> MarkAllAsReadAsync(int userAccountId)
    {
        var result = await _unitOfWork.Notifications.MarkAllAsReadAsync(userAccountId);
        if (result)
        {
            await _unitOfWork.CompleteAsync();
        }
        return result;
    }

    public async Task<Notification> CreateAnnouncementAsync(
        int adminAccountId,
        string title,
        string content,
        NotificationType type,
        NotificationPriority priority,
        TargetAudienceType targetAudience,
        List<int>? specificResidentIds = null)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Tiêu đề thông báo không được để trống.", nameof(title));

        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Nội dung thông báo không được để trống.", nameof(content));

        // Lấy toàn bộ cư dân có tài khoản đang hoạt động
        var allResidents = await _unitOfWork.Residents.GetAllWithDetailsAsync();
        
        // Điều kiện tiên quyết: Chỉ chọn cư dân có tài khoản UserAccount đang HOẠT ĐỘNG
        var activeAccountResidents = allResidents
            .Where(r => r.UserAccount != null && r.UserAccount.IsActive)
            .ToList();

        List<Resident> targetResidents = targetAudience switch
        {
            TargetAudienceType.All => activeAccountResidents,
            TargetAudienceType.ActiveResidents => activeAccountResidents
                .Where(r => r.ResidenceHistories.Any(rh => rh.EndDate == null))
                .ToList(),
            TargetAudienceType.MovedOutResidents => activeAccountResidents
                .Where(r => r.ResidenceHistories.Count > 0 && r.ResidenceHistories.All(rh => rh.EndDate != null))
                .ToList(),
            TargetAudienceType.SpecificResidents => specificResidentIds != null && specificResidentIds.Count > 0
                ? activeAccountResidents.Where(r => specificResidentIds.Contains(r.Id)).ToList()
                : new List<Resident>(),
            _ => activeAccountResidents
        };

        var notification = new Notification
        {
            Title = title.Trim(),
            Content = content.Trim(),
            Type = type,
            Priority = priority,
            TargetAudience = targetAudience,
            SenderId = adminAccountId,
            CreatedAt = DateTime.Now
        };

        foreach (var res in targetResidents)
        {
            if (res.UserAccount != null)
            {
                notification.Recipients.Add(new NotificationRecipient
                {
                    UserAccountId = res.UserAccount.Id,
                    IsRead = false,
                    CreatedAt = notification.CreatedAt
                });
            }
        }

        await _unitOfWork.Notifications.AddAsync(notification);
        await _unitOfWork.CompleteAsync();

        return notification;
    }

    public async Task<Notification?> NotifyAdminNewApprovalRequestAsync(
        int requesterAccountId,
        string entityType,
        int entityId,
        string title,
        string content,
        NotificationPriority priority = NotificationPriority.Important)
    {
        var adminAccounts = (await _unitOfWork.UserAccounts.GetAllAsync())
            .Where(u => u.Role == UserRole.Admin && u.IsActive)
            .ToList();

        if (adminAccounts.Count == 0) return null;

        var notification = new Notification
        {
            Title = title.Trim(),
            Content = content.Trim(),
            Type = NotificationType.ApprovalRequest,
            Priority = priority,
            TargetAudience = TargetAudienceType.SystemAdmin,
            SenderId = requesterAccountId,
            RelatedEntityType = entityType,
            RelatedEntityId = entityId,
            CreatedAt = DateTime.Now
        };

        foreach (var admin in adminAccounts)
        {
            notification.Recipients.Add(new NotificationRecipient
            {
                UserAccountId = admin.Id,
                IsRead = false,
                CreatedAt = notification.CreatedAt
            });
        }

        await _unitOfWork.Notifications.AddAsync(notification);
        await _unitOfWork.CompleteAsync();

        return notification;
    }

    public async Task<Notification?> NotifyResidentApprovalResultAsync(
        int residentId,
        string entityType,
        int entityId,
        bool isApproved,
        string title,
        string content)
    {
        var userAccount = await _unitOfWork.UserAccounts.GetByResidentIdAsync(residentId);
        if (userAccount == null || !userAccount.IsActive)
        {
            return null;
        }

        var notification = new Notification
        {
            Title = title.Trim(),
            Content = content.Trim(),
            Type = NotificationType.ApprovalResult,
            Priority = isApproved ? NotificationPriority.Normal : NotificationPriority.Important,
            TargetAudience = TargetAudienceType.SpecificResidents,
            RelatedEntityType = entityType,
            RelatedEntityId = entityId,
            CreatedAt = DateTime.Now
        };

        notification.Recipients.Add(new NotificationRecipient
        {
            UserAccountId = userAccount.Id,
            IsRead = false,
            CreatedAt = notification.CreatedAt
        });

        await _unitOfWork.Notifications.AddAsync(notification);
        await _unitOfWork.CompleteAsync();

        return notification;
    }
}
