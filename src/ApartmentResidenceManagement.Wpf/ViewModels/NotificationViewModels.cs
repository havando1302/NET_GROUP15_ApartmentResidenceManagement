using System;
using ApartmentResidenceManagement.Domain.Entities;
using ApartmentResidenceManagement.Domain.Enums;

namespace ApartmentResidenceManagement.Wpf.ViewModels;

public class NotificationItemViewModel : ViewModelBase
{
    private bool _isRead;

    public int RecipientId { get; set; }
    public int NotificationId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public NotificationPriority Priority { get; set; }
    public TargetAudienceType TargetAudience { get; set; }
    
    public string? RelatedEntityType { get; set; }
    public int? RelatedEntityId { get; set; }
    public DateTime CreatedAt { get; set; }

    public string SenderName { get; set; } = "Hệ thống";

    public bool IsRead
    {
        get => _isRead;
        set => SetProperty(ref _isRead, value);
    }

    public bool IsUnread => !IsRead;

    public bool HasRelatedEntity => !string.IsNullOrEmpty(RelatedEntityType) && RelatedEntityId.HasValue;

    public bool IsApprovalRequest => Type == NotificationType.ApprovalRequest;

    public string TypeIcon => Type switch
    {
        NotificationType.ApprovalRequest => RelatedEntityType == "Vehicle" ? "🚗" : "👥",
        NotificationType.ApprovalResult => "✅",
        NotificationType.Maintenance => "🛠️",
        NotificationType.FeeNotice => "💳",
        NotificationType.UrgentNotice => "🚨",
        _ => "📢"
    };

    public string TypeName => Type switch
    {
        NotificationType.ApprovalRequest => RelatedEntityType == "Vehicle" ? "Đơn duyệt xe" : "Yêu cầu thành viên",
        NotificationType.ApprovalResult => "Kết quả duyệt đơn",
        NotificationType.Maintenance => "Bảo trì - Kỹ thuật",
        NotificationType.FeeNotice => "Phí dịch vụ",
        NotificationType.UrgentNotice => "Khẩn cấp",
        _ => "Thông báo chung"
    };

    public string TypeBadgeBg => Type switch
    {
        NotificationType.ApprovalRequest => "#EFF6FF",
        NotificationType.ApprovalResult => "#ECFDF5",
        NotificationType.Maintenance => "#FEF3C7",
        NotificationType.FeeNotice => "#F3E8FF",
        NotificationType.UrgentNotice => "#FEE2E2",
        _ => "#F1F5F9"
    };

    public string TypeBadgeFg => Type switch
    {
        NotificationType.ApprovalRequest => "#1D4ED8",
        NotificationType.ApprovalResult => "#059669",
        NotificationType.Maintenance => "#D97706",
        NotificationType.FeeNotice => "#7C3AED",
        NotificationType.UrgentNotice => "#DC2626",
        _ => "#475569"
    };

    public string PriorityBadgeText => Priority switch
    {
        NotificationPriority.Urgent => "Khẩn",
        NotificationPriority.Important => "Quan trọng",
        _ => string.Empty
    };

    public bool HasPriorityBadge => Priority != NotificationPriority.Normal;

    public string PriorityBadgeBg => Priority switch
    {
        NotificationPriority.Urgent => "#FEE2E2",
        NotificationPriority.Important => "#FEF3C7",
        _ => "#F1F5F9"
    };

    public string PriorityBadgeFg => Priority switch
    {
        NotificationPriority.Urgent => "#DC2626",
        NotificationPriority.Important => "#B45309",
        _ => "#64748B"
    };

    public string TimeAgoText
    {
        get
        {
            var diff = DateTime.Now - CreatedAt;
            if (diff.TotalMinutes < 1) return "Vừa xong";
            if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes} phút trước";
            if (diff.TotalHours < 24) return $"{(int)diff.TotalHours} giờ trước";
            if (diff.TotalDays < 7) return $"{(int)diff.TotalDays} ngày trước";
            return CreatedAt.ToString("dd/MM/yyyy HH:mm");
        }
    }

    public static NotificationItemViewModel FromRecipient(NotificationRecipient recipient)
    {
        var n = recipient.Notification;
        string sender = n.Sender?.Resident?.FullName ?? (n.Sender?.Username ?? "Hệ thống");
        if (n.Sender?.Role == UserRole.Admin) sender = "Ban quản lý";

        return new NotificationItemViewModel
        {
            RecipientId = recipient.Id,
            NotificationId = n.Id,
            Title = n.Title,
            Content = n.Content,
            Type = n.Type,
            Priority = n.Priority,
            TargetAudience = n.TargetAudience,
            RelatedEntityType = n.RelatedEntityType,
            RelatedEntityId = n.RelatedEntityId,
            CreatedAt = recipient.CreatedAt,
            IsRead = recipient.IsRead,
            SenderName = sender
        };
    }
}

public class SelectableResidentItemViewModel : ViewModelBase
{
    private bool _isSelected;

    public int ResidentId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string ApartmentInfo { get; set; } = string.Empty;
    public string StatusText { get; set; } = string.Empty;
    public string AvatarPath { get; set; } = "/Assets/resident_avatar.png";

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
}
