using System;

namespace ApartmentResidenceManagement.Domain.Entities;

public class NotificationRecipient
{
    public int Id { get; set; }
    public int NotificationId { get; set; }
    public virtual Notification Notification { get; set; } = null!;

    public int UserAccountId { get; set; }
    public virtual UserAccount UserAccount { get; set; } = null!;

    public bool IsRead { get; set; } = false;
    public DateTime? ReadAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
