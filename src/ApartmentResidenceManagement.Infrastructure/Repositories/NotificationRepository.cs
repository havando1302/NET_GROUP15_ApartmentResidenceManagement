using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApartmentResidenceManagement.Domain.Entities;
using ApartmentResidenceManagement.Domain.Enums;
using ApartmentResidenceManagement.Domain.Interfaces;
using ApartmentResidenceManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ApartmentResidenceManagement.Infrastructure.Repositories;

public class NotificationRepository : Repository<Notification>, INotificationRepository
{
    public NotificationRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<NotificationRecipient>> GetUserNotificationsAsync(int userAccountId)
    {
        return await Context.NotificationRecipients
            .AsNoTracking()
            .Include(r => r.Notification)
                .ThenInclude(n => n.Sender)
            .Where(r => r.UserAccountId == userAccountId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<int> GetUnreadCountAsync(int userAccountId)
    {
        return await Context.NotificationRecipients
            .AsNoTracking()
            .CountAsync(r => r.UserAccountId == userAccountId && !r.IsRead);
    }

    public async Task<bool> MarkAsReadAsync(int notificationId, int userAccountId)
    {
        var recipient = await Context.NotificationRecipients
            .FirstOrDefaultAsync(r => r.NotificationId == notificationId && r.UserAccountId == userAccountId);

        if (recipient == null)
            return false;

        if (!recipient.IsRead)
        {
            recipient.IsRead = true;
            recipient.ReadAt = DateTime.Now;
        }

        return true;
    }

    public async Task<bool> MarkAllAsReadAsync(int userAccountId)
    {
        var unreadList = await Context.NotificationRecipients
            .Where(r => r.UserAccountId == userAccountId && !r.IsRead)
            .ToListAsync();

        if (!unreadList.Any())
            return false;

        var now = DateTime.Now;
        foreach (var item in unreadList)
        {
            item.IsRead = true;
            item.ReadAt = now;
        }

        return true;
    }

    public async Task AddRecipientsAsync(IEnumerable<NotificationRecipient> recipients)
    {
        await Context.NotificationRecipients.AddRangeAsync(recipients);
    }

    public async Task<Notification?> GetWithRecipientsAsync(int notificationId)
    {
        return await DbSet
            .Include(n => n.Sender)
            .Include(n => n.Recipients)
                .ThenInclude(r => r.UserAccount)
            .FirstOrDefaultAsync(n => n.Id == notificationId);
    }

    public async Task<IEnumerable<NotificationRecipient>> GetAdminApprovalNotificationsAsync(int adminAccountId)
    {
        return await Context.NotificationRecipients
            .AsNoTracking()
            .Include(r => r.Notification)
                .ThenInclude(n => n.Sender)
            .Where(r => r.UserAccountId == adminAccountId && r.Notification.Type == NotificationType.ApprovalRequest)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Notification>> GetSentAnnouncementsAsync()
    {
        return await DbSet
            .AsNoTracking()
            .Include(n => n.Sender)
            .Include(n => n.Recipients)
                .ThenInclude(r => r.UserAccount)
                    .ThenInclude(u => u.Resident)
            .Where(n => n.Type != NotificationType.ApprovalRequest)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();
    }
}
