using System;
using System.Collections.Generic;
using ApartmentResidenceManagement.Domain.Enums;

namespace ApartmentResidenceManagement.Domain.Entities;

public class Notification
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public NotificationType Type { get; set; } = NotificationType.General;
    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;
    public TargetAudienceType TargetAudience { get; set; } = TargetAudienceType.All;

    public int? SenderId { get; set; }
    public virtual UserAccount? Sender { get; set; }

    /// <summary>
    /// Loại thực thể liên kết (ví dụ: "Vehicle", "MemberRegistrationRequest")
    /// </summary>
    public string? RelatedEntityType { get; set; }

    /// <summary>
    /// ID của thực thể liên kết tương ứng
    /// </summary>
    public int? RelatedEntityId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public virtual ICollection<NotificationRecipient> Recipients { get; set; } = new List<NotificationRecipient>();
}
