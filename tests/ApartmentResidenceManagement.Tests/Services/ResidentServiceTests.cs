using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ApartmentResidenceManagement.Domain.Entities;
using ApartmentResidenceManagement.Domain.Exceptions;
using ApartmentResidenceManagement.Domain.Interfaces;
using ApartmentResidenceManagement.Application.Services;
using Moq;
using Xunit;

namespace ApartmentResidenceManagement.Tests.Services;

public class ResidentServiceTests
{
    private readonly Mock<IUnitOfWork> _mockUow;
    private readonly Mock<IResidentRepository> _mockResidentRepo;
    private readonly ResidentService _service;

    public ResidentServiceTests()
    {
        _mockUow = new Mock<IUnitOfWork>();
        _mockResidentRepo = new Mock<IResidentRepository>();

        _mockUow.Setup(u => u.Residents).Returns(_mockResidentRepo.Object);

        _service = new ResidentService(_mockUow.Object);
    }

    [Fact]
    public async Task CreateResident_InvalidDateOfBirth_ShouldThrowBusinessRuleException()
    {
        // Arrange
        var resident = new Resident 
        { 
            FullName = "Nguyen Van A", 
            DateOfBirth = DateTime.Now.AddDays(1), // Trong tương lai
            IdentityCard = "001200000001",
            PhoneNumber = "0912345678",
            Email = "nguyenvana@gmail.com",
            HomeTown = "Hà Nội"
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => _service.CreateResidentAsync(resident));
        Assert.Contains("Ngày sinh không hợp lệ", ex.Message);
    }

    [Fact]
    public async Task CreateResident_InvalidPhoneNumber_ShouldThrowBusinessRuleException()
    {
        // Arrange
        var resident = new Resident 
        { 
            FullName = "Nguyen Van A", 
            DateOfBirth = new DateTime(1990, 1, 1),
            IdentityCard = "001200000001",
            PhoneNumber = "1234", // Lỗi định dạng
            Email = "nguyenvana@gmail.com",
            HomeTown = "Hà Nội"
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => _service.CreateResidentAsync(resident));
        Assert.Contains("Số điện thoại không hợp lệ", ex.Message);
    }

    [Fact]
    public async Task CreateResident_DuplicateIdentityCard_ShouldThrowBusinessRuleException()
    {
        // Arrange
        var resident = new Resident 
        { 
            FullName = "Nguyen Van A", 
            DateOfBirth = new DateTime(1990, 1, 1),
            IdentityCard = "123456789012",
            PhoneNumber = "0912345678",
            Email = "nguyenvana@gmail.com",
            HomeTown = "Hà Nội"
        };
        _mockResidentRepo.Setup(r => r.GetByIdentityCardAsync("123456789012")).ReturnsAsync(resident);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => _service.CreateResidentAsync(resident));
        Assert.Contains("đã tồn tại trong hệ thống", ex.Message);
    }

    [Fact]
    public async Task CreateResident_MissingIdentityCard_ShouldThrowBusinessRuleException()
    {
        var resident = new Resident
        {
            FullName = "Nguyen Van A",
            DateOfBirth = new DateTime(1990, 1, 1),
            IdentityCard = null,
            PhoneNumber = "0912345678",
            Email = "nguyenvana@gmail.com",
            HomeTown = "Hà Nội"
        };

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => _service.CreateResidentAsync(resident));
        Assert.Contains("Số CCCD/CMND là bắt buộc", ex.Message);
    }

    [Fact]
    public async Task CreateResident_MissingPhoneNumber_ShouldThrowBusinessRuleException()
    {
        var resident = new Resident
        {
            FullName = "Nguyen Van A",
            DateOfBirth = new DateTime(1990, 1, 1),
            IdentityCard = "001200000001",
            PhoneNumber = null,
            Email = "nguyenvana@gmail.com",
            HomeTown = "Hà Nội"
        };

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => _service.CreateResidentAsync(resident));
        Assert.Contains("Số điện thoại liên hệ là bắt buộc", ex.Message);
    }

    [Fact]
    public async Task CreateResident_MissingEmail_ShouldThrowBusinessRuleException()
    {
        var resident = new Resident
        {
            FullName = "Nguyen Van A",
            DateOfBirth = new DateTime(1990, 1, 1),
            IdentityCard = "001200000001",
            PhoneNumber = "0912345678",
            Email = null,
            HomeTown = "Hà Nội"
        };

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => _service.CreateResidentAsync(resident));
        Assert.Contains("Email liên hệ là bắt buộc", ex.Message);
    }

    [Fact]
    public async Task CreateResident_MissingHomeTown_ShouldThrowBusinessRuleException()
    {
        var resident = new Resident
        {
            FullName = "Nguyen Van A",
            DateOfBirth = new DateTime(1990, 1, 1),
            IdentityCard = "001200000001",
            PhoneNumber = "0912345678",
            Email = "nguyenvana@gmail.com",
            HomeTown = null
        };

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => _service.CreateResidentAsync(resident));
        Assert.Contains("Quê quán / Hộ khẩu thường trú là bắt buộc", ex.Message);
    }

    [Fact]
    public async Task UpdateAvatarAsync_ValidResident_ShouldUpdateAvatarPath()
    {
        // Arrange
        var resident = new Resident
        {
            Id = 1,
            FullName = "Nguyen Van A",
            DateOfBirth = new DateTime(1990, 1, 1),
            AvatarPath = "/Assets/resident_avatar.png"
        };
        _mockResidentRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(resident);
        _mockUow.Setup(u => u.CompleteAsync()).ReturnsAsync(1);

        // Act
        var result = await _service.UpdateAvatarAsync(1, "/Assets/avatar_young_man.png");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("/Assets/avatar_young_man.png", result.AvatarPath);
        _mockResidentRepo.Verify(r => r.Update(resident), Times.Once);
        _mockUow.Verify(u => u.CompleteAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateAvatarAsync_ResidentNotFound_ShouldThrowException()
    {
        // Arrange
        _mockResidentRepo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Resident?)null);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => _service.UpdateAvatarAsync(999, "/Assets/avatar_son.png"));
        Assert.Contains("Không tìm thấy cư dân", ex.Message);
    }

    [Fact]
    public async Task DeleteResident_WithActiveResidence_ThrowsBusinessRuleException()
    {
        var resident = new Resident { Id = 10, FullName = "Nguyễn Văn A" };
        var mockHistRepo = new Mock<IResidenceHistoryRepository>();
        _mockResidentRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(resident);
        _mockUow.Setup(u => u.ResidenceHistories).Returns(mockHistRepo.Object);
        mockHistRepo.Setup(r => r.GetActiveByResidentIdAsync(10))
            .ReturnsAsync(new ResidenceHistory { Id = 1, ResidentId = 10, IsActive = true });

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => _service.DeleteResidentAsync(10));
        Assert.Contains("đang có hồ sơ cư trú hoạt động", ex.Message);
    }

    [Fact]
    public async Task DeleteResident_WithPastHistory_ThrowsBusinessRuleException()
    {
        var resident = new Resident { Id = 10, FullName = "Nguyễn Văn A" };
        var mockHistRepo = new Mock<IResidenceHistoryRepository>();
        _mockResidentRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(resident);
        _mockUow.Setup(u => u.ResidenceHistories).Returns(mockHistRepo.Object);
        mockHistRepo.Setup(r => r.GetActiveByResidentIdAsync(10)).ReturnsAsync((ResidenceHistory?)null);
        mockHistRepo.Setup(r => r.GetHistoryByResidentIdAsync(10))
            .ReturnsAsync(new List<ResidenceHistory> { new ResidenceHistory { Id = 1, ResidentId = 10, IsActive = false } });

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => _service.DeleteResidentAsync(10));
        Assert.Contains("đã có dữ liệu lịch sử cư trú", ex.Message);
    }

    [Fact]
    public async Task DeleteResident_WithVehicles_ThrowsBusinessRuleException()
    {
        var resident = new Resident { Id = 10, FullName = "Nguyễn Văn A" };
        var mockHistRepo = new Mock<IResidenceHistoryRepository>();
        var mockVehRepo = new Mock<IVehicleRepository>();
        _mockResidentRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(resident);
        _mockUow.Setup(u => u.ResidenceHistories).Returns(mockHistRepo.Object);
        _mockUow.Setup(u => u.Vehicles).Returns(mockVehRepo.Object);
        mockHistRepo.Setup(r => r.GetActiveByResidentIdAsync(10)).ReturnsAsync((ResidenceHistory?)null);
        mockHistRepo.Setup(r => r.GetHistoryByResidentIdAsync(10)).ReturnsAsync(new List<ResidenceHistory>());
        mockVehRepo.Setup(r => r.GetVehiclesByOwnerIdAsync(10))
            .ReturnsAsync(new List<Vehicle> { new Vehicle { Id = 5, OwnerId = 10, LicensePlate = "29A1-12345" } });

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => _service.DeleteResidentAsync(10));
        Assert.Contains("đang có phương tiện đăng ký", ex.Message);
    }

    [Fact]
    public async Task DeleteResident_NoHistoryOrDependencies_DeletesSuccessfully()
    {
        var resident = new Resident { Id = 10, FullName = "Nguyễn Văn A" };
        var mockHistRepo = new Mock<IResidenceHistoryRepository>();
        var mockVehRepo = new Mock<IVehicleRepository>();
        var mockAccRepo = new Mock<IUserAccountRepository>();
        var mockReqRepo = new Mock<IMemberRegistrationRequestRepository>();

        _mockResidentRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(resident);
        _mockUow.Setup(u => u.ResidenceHistories).Returns(mockHistRepo.Object);
        _mockUow.Setup(u => u.Vehicles).Returns(mockVehRepo.Object);
        _mockUow.Setup(u => u.UserAccounts).Returns(mockAccRepo.Object);
        _mockUow.Setup(u => u.MemberRegistrationRequests).Returns(mockReqRepo.Object);
        _mockUow.Setup(u => u.CompleteAsync()).ReturnsAsync(1);

        mockHistRepo.Setup(r => r.GetActiveByResidentIdAsync(10)).ReturnsAsync((ResidenceHistory?)null);
        mockHistRepo.Setup(r => r.GetHistoryByResidentIdAsync(10)).ReturnsAsync(new List<ResidenceHistory>());
        mockVehRepo.Setup(r => r.GetVehiclesByOwnerIdAsync(10)).ReturnsAsync(new List<Vehicle>());
        mockReqRepo.Setup(r => r.GetByRequesterResidentIdAsync(10)).ReturnsAsync(new List<MemberRegistrationRequest>());
        mockAccRepo.Setup(r => r.GetByResidentIdAsync(10)).ReturnsAsync(new UserAccount { Id = 20, ResidentId = 10 });

        await _service.DeleteResidentAsync(10);

        _mockResidentRepo.Verify(r => r.Delete(resident), Times.Once);
        mockAccRepo.Verify(r => r.Delete(It.Is<UserAccount>(a => a.Id == 20)), Times.Once);
        _mockUow.Verify(u => u.CompleteAsync(), Times.Once);
    }
}
