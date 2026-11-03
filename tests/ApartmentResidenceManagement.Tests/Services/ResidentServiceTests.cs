using System;
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
            DateOfBirth = DateTime.Now.AddDays(1) // Trong tương lai
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
            PhoneNumber = "1234" // Lỗi định dạng
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
            IdentityCard = "123456789012"
        };
        _mockResidentRepo.Setup(r => r.GetByIdentityCardAsync("123456789012")).ReturnsAsync(resident);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => _service.CreateResidentAsync(resident));
        Assert.Contains("đã tồn tại trong hệ thống", ex.Message);
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
}
