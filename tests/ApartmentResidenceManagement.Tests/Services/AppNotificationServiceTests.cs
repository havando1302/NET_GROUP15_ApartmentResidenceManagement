using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApartmentResidenceManagement.Application.Services;
using ApartmentResidenceManagement.Domain.Entities;
using ApartmentResidenceManagement.Domain.Enums;
using ApartmentResidenceManagement.Domain.Interfaces;
using Moq;
using Xunit;

namespace ApartmentResidenceManagement.Tests.Services;

public class AppNotificationServiceTests
{
    private readonly Mock<IUnitOfWork> _mockUow;
    private readonly Mock<INotificationRepository> _mockNotifRepo;
    private readonly Mock<IResidentRepository> _mockResidentRepo;
    private readonly Mock<IUserAccountRepository> _mockUserAccountRepo;
    private readonly AppNotificationService _service;

    public AppNotificationServiceTests()
    {
        _mockUow = new Mock<IUnitOfWork>();
        _mockNotifRepo = new Mock<INotificationRepository>();
        _mockResidentRepo = new Mock<IResidentRepository>();
        _mockUserAccountRepo = new Mock<IUserAccountRepository>();

        _mockUow.Setup(u => u.Notifications).Returns(_mockNotifRepo.Object);
        _mockUow.Setup(u => u.Residents).Returns(_mockResidentRepo.Object);
        _mockUow.Setup(u => u.UserAccounts).Returns(_mockUserAccountRepo.Object);
        _mockUow.Setup(u => u.CompleteAsync()).ReturnsAsync(1);

        _service = new AppNotificationService(_mockUow.Object);
    }

    [Fact]
    public async Task CreateAnnouncementAsync_ThrowsException_WhenTitleOrContentIsEmpty()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.CreateAnnouncementAsync(1, "", "Nội dung", NotificationType.General, NotificationPriority.Normal, TargetAudienceType.All));

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.CreateAnnouncementAsync(1, "Tiêu đề", "   ", NotificationType.General, NotificationPriority.Normal, TargetAudienceType.All));
    }

    [Fact]
    public async Task CreateAnnouncementAsync_AllAudience_OnlySendsToResidentsWithActiveAccounts()
    {
        // Arrange
        var resident1 = new Resident
        {
            Id = 1,
            FullName = "Nguyễn Văn A",
            UserAccount = new UserAccount { Id = 101, Username = "nguyenvana", IsActive = true }
        };

        var resident2 = new Resident
        {
            Id = 2,
            FullName = "Trần Thị B",
            UserAccount = new UserAccount { Id = 102, Username = "tranthib", IsActive = false } // Bị khóa
        };

        var resident3 = new Resident
        {
            Id = 3,
            FullName = "Lê Văn C",
            UserAccount = null // Chưa có tài khoản
        };

        _mockResidentRepo.Setup(r => r.GetAllWithDetailsAsync())
            .ReturnsAsync(new List<Resident> { resident1, resident2, resident3 });

        Notification? savedNotification = null;
        _mockNotifRepo.Setup(n => n.AddAsync(It.IsAny<Notification>()))
            .Callback<Notification>(n => savedNotification = n)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.CreateAnnouncementAsync(
            adminAccountId: 999,
            title: "Thông báo bảo trì",
            content: "Nội dung thông báo bảo trì",
            type: NotificationType.Maintenance,
            priority: NotificationPriority.Important,
            targetAudience: TargetAudienceType.All);

        // Assert
        Assert.NotNull(savedNotification);
        Assert.Equal("Thông báo bảo trì", savedNotification.Title);
        // Chỉ resident1 có UserAccount active mới được nhận
        Assert.Single(savedNotification.Recipients);
        Assert.Equal(101, savedNotification.Recipients.First().UserAccountId);
        _mockUow.Verify(u => u.CompleteAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateAnnouncementAsync_SpecificResidents_OnlySendsToSelectedActiveAccountResidents()
    {
        // Arrange
        var resident1 = new Resident
        {
            Id = 1,
            FullName = "Nguyễn Văn A",
            UserAccount = new UserAccount { Id = 101, IsActive = true }
        };

        var resident2 = new Resident
        {
            Id = 2,
            FullName = "Trần Thị B",
            UserAccount = new UserAccount { Id = 102, IsActive = true }
        };

        _mockResidentRepo.Setup(r => r.GetAllWithDetailsAsync())
            .ReturnsAsync(new List<Resident> { resident1, resident2 });

        Notification? savedNotification = null;
        _mockNotifRepo.Setup(n => n.AddAsync(It.IsAny<Notification>()))
            .Callback<Notification>(n => savedNotification = n)
            .Returns(Task.CompletedTask);

        // Act: Chỉ gửi cho resident2
        await _service.CreateAnnouncementAsync(
            adminAccountId: 999,
            title: "Thông báo riêng",
            content: "Nội dung riêng",
            type: NotificationType.General,
            priority: NotificationPriority.Normal,
            targetAudience: TargetAudienceType.SpecificResidents,
            specificResidentIds: new List<int> { 2 });

        // Assert
        Assert.NotNull(savedNotification);
        Assert.Single(savedNotification.Recipients);
        Assert.Equal(102, savedNotification.Recipients.First().UserAccountId);
    }

    [Fact]
    public async Task NotifyAdminNewApprovalRequestAsync_SendsToAllActiveAdmins()
    {
        // Arrange
        var admin1 = new UserAccount { Id = 1, Role = UserRole.Admin, IsActive = true };
        var admin2 = new UserAccount { Id = 2, Role = UserRole.Admin, IsActive = false }; // Inactive
        var resident = new UserAccount { Id = 3, Role = UserRole.Resident, IsActive = true };

        _mockUserAccountRepo.Setup(u => u.GetAllAsync())
            .ReturnsAsync(new List<UserAccount> { admin1, admin2, resident });

        Notification? savedNotification = null;
        _mockNotifRepo.Setup(n => n.AddAsync(It.IsAny<Notification>()))
            .Callback<Notification>(n => savedNotification = n)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.NotifyAdminNewApprovalRequestAsync(
            requesterAccountId: 3,
            entityType: "Vehicle",
            entityId: 50,
            title: "Đơn đăng ký xe mới: 29A-999.99",
            content: "Cư dân gửi đơn đăng ký xe mới");

        // Assert
        Assert.NotNull(savedNotification);
        Assert.Equal(NotificationType.ApprovalRequest, savedNotification.Type);
        Assert.Equal("Vehicle", savedNotification.RelatedEntityType);
        Assert.Equal(50, savedNotification.RelatedEntityId);
        Assert.Single(savedNotification.Recipients);
        Assert.Equal(1, savedNotification.Recipients.First().UserAccountId);
        _mockUow.Verify(u => u.CompleteAsync(), Times.Once);
    }

    [Fact]
    public async Task NotifyResidentApprovalResultAsync_SendsNotificationToResidentAccount()
    {
        // Arrange
        int residentId = 15;
        var residentAccount = new UserAccount { Id = 115, ResidentId = residentId, IsActive = true };

        _mockUserAccountRepo.Setup(u => u.GetByResidentIdAsync(residentId))
            .ReturnsAsync(residentAccount);

        Notification? savedNotification = null;
        _mockNotifRepo.Setup(n => n.AddAsync(It.IsAny<Notification>()))
            .Callback<Notification>(n => savedNotification = n)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.NotifyResidentApprovalResultAsync(
            residentId: residentId,
            entityType: "Vehicle",
            entityId: 10,
            isApproved: true,
            title: "Phương tiện đã được phê duyệt",
            content: "Xe biển số 29A-123.45 đã được Ban quản lý phê duyệt.");

        // Assert
        Assert.NotNull(savedNotification);
        Assert.Equal(NotificationType.ApprovalResult, savedNotification.Type);
        Assert.Equal(NotificationPriority.Normal, savedNotification.Priority);
        Assert.Single(savedNotification.Recipients);
        Assert.Equal(115, savedNotification.Recipients.First().UserAccountId);
        _mockUow.Verify(u => u.CompleteAsync(), Times.Once);
    }

    [Fact]
    public async Task MarkAsReadAsync_CallsRepositoryAndCompletesUnitOfWork()
    {
        // Arrange
        _mockNotifRepo.Setup(n => n.MarkAsReadAsync(5, 101)).ReturnsAsync(true);

        // Act
        var result = await _service.MarkAsReadAsync(5, 101);

        // Assert
        Assert.True(result);
        _mockNotifRepo.Verify(n => n.MarkAsReadAsync(5, 101), Times.Once);
        _mockUow.Verify(u => u.CompleteAsync(), Times.Once);
    }

    [Fact]
    public async Task MarkAllAsReadAsync_CallsRepositoryAndCompletesUnitOfWork()
    {
        // Arrange
        _mockNotifRepo.Setup(n => n.MarkAllAsReadAsync(101)).ReturnsAsync(true);

        // Act
        var result = await _service.MarkAllAsReadAsync(101);

        // Assert
        Assert.True(result);
        _mockNotifRepo.Verify(n => n.MarkAllAsReadAsync(101), Times.Once);
        _mockUow.Verify(u => u.CompleteAsync(), Times.Once);
    }

    [Fact]
    public async Task GetSentAnnouncementsAsync_CallsRepository()
    {
        // Arrange
        var sampleList = new List<Notification>
        {
            new Notification { Id = 1, Title = "Thông báo 1", Type = NotificationType.General }
        };
        _mockNotifRepo.Setup(r => r.GetSentAnnouncementsAsync()).ReturnsAsync(sampleList);

        // Act
        var result = await _service.GetSentAnnouncementsAsync();

        // Assert
        Assert.Single(result);
        _mockNotifRepo.Verify(r => r.GetSentAnnouncementsAsync(), Times.Once);
    }
}
