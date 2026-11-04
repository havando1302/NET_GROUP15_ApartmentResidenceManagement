using System;
using ApartmentResidenceManagement.Domain.Entities;
using ApartmentResidenceManagement.Domain.Enums;
using ApartmentResidenceManagement.Wpf.ViewModels;
using Xunit;

namespace ApartmentResidenceManagement.Tests.ViewModels;

public class NotificationViewModelsTests
{
    [Fact]
    public void FromRecipient_ApprovalRequestVehicle_SetsCorrectPropertiesAndIcon()
    {
        // Arrange
        var notification = new Notification
        {
            Id = 1,
            Title = "Đơn xe mới",
            Content = "Nội dung đơn xe",
            Type = NotificationType.ApprovalRequest,
            Priority = NotificationPriority.Important,
            TargetAudience = TargetAudienceType.SystemAdmin,
            RelatedEntityType = "Vehicle",
            RelatedEntityId = 99,
            Sender = new UserAccount
            {
                Resident = new Resident { FullName = "Nguyễn Văn A" }
            }
        };

        var recipient = new NotificationRecipient
        {
            Id = 10,
            Notification = notification,
            IsRead = false,
            CreatedAt = DateTime.Now.AddMinutes(-5)
        };

        // Act
        var vm = NotificationItemViewModel.FromRecipient(recipient);

        // Assert
        Assert.Equal(1, vm.NotificationId);
        Assert.Equal("Đơn xe mới", vm.Title);
        Assert.True(vm.IsApprovalRequest);
        Assert.True(vm.HasRelatedEntity);
        Assert.Equal("Vehicle", vm.RelatedEntityType);
        Assert.Equal(99, vm.RelatedEntityId);
        Assert.Equal("🚗", vm.TypeIcon);
        Assert.Equal("Đơn duyệt xe", vm.TypeName);
        Assert.True(vm.IsUnread);
        Assert.Equal("Nguyễn Văn A", vm.SenderName);
        Assert.True(vm.HasPriorityBadge);
        Assert.Equal("Quan trọng", vm.PriorityBadgeText);
    }

    [Fact]
    public void FromRecipient_MaintenanceNoticeFromAdmin_SetsAdminSenderAndIcon()
    {
        // Arrange
        var notification = new Notification
        {
            Id = 2,
            Title = "Bảo trì thang máy",
            Content = "Nội dung bảo trì",
            Type = NotificationType.Maintenance,
            Priority = NotificationPriority.Normal,
            Sender = new UserAccount { Role = UserRole.Admin, Username = "admin" }
        };

        var recipient = new NotificationRecipient
        {
            Id = 20,
            Notification = notification,
            IsRead = true,
            CreatedAt = DateTime.Now.AddHours(-1)
        };

        // Act
        var vm = NotificationItemViewModel.FromRecipient(recipient);

        // Assert
        Assert.Equal("🛠️", vm.TypeIcon);
        Assert.Equal("Bảo trì - Kỹ thuật", vm.TypeName);
        Assert.False(vm.IsUnread);
        Assert.True(vm.IsRead);
        Assert.Equal("Ban quản lý", vm.SenderName);
        Assert.False(vm.HasPriorityBadge);
    }
}
