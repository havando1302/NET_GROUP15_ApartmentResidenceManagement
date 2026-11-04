using System;
using System.Windows.Documents;
using ApartmentResidenceManagement.Domain.Entities;
using ApartmentResidenceManagement.Domain.Enums;
using ApartmentResidenceManagement.Wpf.Helpers;
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

    [Fact]
    public void FlowDocumentHelper_LoadContentIntoDocument_ParsesMarkupCorrectly()
    {
        // Arrange
        var doc = new FlowDocument();
        string markup = "Xin chào <b>Cư dân</b> toà nhà <color=#2563EB>A1</color>!<br/>Bảo trì hôm nay.";

        // Act
        FlowDocumentHelper.LoadContentIntoDocument(doc, markup);

        // Assert
        Assert.NotEmpty(doc.Blocks);
        var textRange = new TextRange(doc.ContentStart, doc.ContentEnd);
        Assert.Contains("Xin chào", textRange.Text);
        Assert.Contains("Cư dân", textRange.Text);
    }

    [Fact]
    public void FlowDocumentHelper_SaveAndLoadDocument_RoundTripsContent()
    {
        // Arrange
        var doc1 = new FlowDocument();
        doc1.Blocks.Add(new Paragraph(new Run("Nội dung thử nghiệm lưu trữ XAML")));

        // Act
        string xaml = FlowDocumentHelper.SaveDocumentToXaml(doc1);
        Assert.NotEmpty(xaml);

        var doc2 = new FlowDocument();
        FlowDocumentHelper.LoadContentIntoDocument(doc2, xaml);
        var textRange = new TextRange(doc2.ContentStart, doc2.ContentEnd);

        // Assert
        Assert.Contains("Nội dung thử nghiệm lưu trữ XAML", textRange.Text);
    }

    [Fact]
    public void SelectableResidentItemViewModel_InitializesCorrectly()
    {
        var item = new SelectableResidentItemViewModel
        {
            ResidentId = 12,
            FullName = "Hoàng Văn H",
            Username = "hoangvanh",
            ApartmentInfo = "Căn hộ 1205",
            StatusText = "Đang cư trú",
            IsSelected = true
        };

        Assert.Equal(12, item.ResidentId);
        Assert.Equal("Hoàng Văn H", item.FullName);
        Assert.True(item.IsSelected);
    }

    [Fact]
    public void SentNotificationItemViewModel_CalculatesBadgesAndMapsToDetailCorrectly()
    {
        var sentVm = new SentNotificationItemViewModel
        {
            NotificationId = 88,
            Title = "Bảo trì định kỳ",
            Content = "<p>Nội dung</p>",
            Type = NotificationType.Maintenance,
            Priority = NotificationPriority.Urgent,
            TargetAudience = TargetAudienceType.ActiveResidents,
            CreatedAt = new DateTime(2026, 11, 15, 8, 30, 0),
            RecipientCount = 20,
            ReadCount = 15
        };

        Assert.Equal("🛠️", sentVm.TypeIcon);
        Assert.Equal("Bảo trì", sentVm.TypeName);
        Assert.Equal("Nhóm cư dân đang cư trú", sentVm.TargetAudienceText);
        Assert.Equal("15/20 đã đọc", sentVm.ReadProgressText);
        Assert.True(sentVm.HasPriorityBadge);
        Assert.Equal("Khẩn", sentVm.PriorityBadgeText);

        var detail = sentVm.ToNotificationDetail();
        Assert.Equal(88, detail.NotificationId);
        Assert.Equal("Bảo trì định kỳ", detail.Title);
        Assert.Equal("<p>Nội dung</p>", detail.Content);
        Assert.True(detail.IsRead);
        Assert.Equal("Ban quản lý", detail.SenderName);
    }
}
