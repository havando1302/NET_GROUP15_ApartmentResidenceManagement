using System.Collections.Generic;
using System.Threading.Tasks;
using ApartmentResidenceManagement.Domain.Entities;
using ApartmentResidenceManagement.Domain.Enums;
using ApartmentResidenceManagement.Domain.Exceptions;
using ApartmentResidenceManagement.Domain.Interfaces;
using ApartmentResidenceManagement.Application.Services;
using Moq;
using Xunit;

namespace ApartmentResidenceManagement.Tests.Services;

public class ApartmentServiceTests
{
    private readonly Mock<IUnitOfWork> _mockUow;
    private readonly Mock<IApartmentRepository> _mockApartmentRepo;
    private readonly ApartmentService _service;

    public ApartmentServiceTests()
    {
        _mockUow = new Mock<IUnitOfWork>();
        _mockApartmentRepo = new Mock<IApartmentRepository>();

        _mockUow.Setup(u => u.Apartments).Returns(_mockApartmentRepo.Object);

        _service = new ApartmentService(_mockUow.Object);
    }

    [Fact]
    public async Task GetAllApartments_ShouldReturnAllApartments()
    {
        // Arrange
        var list = new List<Apartment>
        {
            new() { Id = 1, ApartmentNumber = "101", Floor = 1, Area = 65, Status = ApartmentStatus.Empty },
            new() { Id = 2, ApartmentNumber = "102", Floor = 1, Area = 70, Status = ApartmentStatus.Occupied }
        };
        _mockApartmentRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(list);

        // Act
        var result = await _service.GetAllApartmentsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, ((List<Apartment>)result).Count);
    }

    [Fact]
    public async Task GetApartmentById_ExistingId_ShouldReturnApartment()
    {
        // Arrange
        var apartment = new Apartment { Id = 1, ApartmentNumber = "101", Floor = 1, Area = 65 };
        _mockApartmentRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(apartment);

        // Act
        var result = await _service.GetApartmentByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("101", result.ApartmentNumber);
    }

    [Fact]
    public async Task CreateApartment_ValidData_ShouldCreateSuccessfully()
    {
        // Arrange
        var apartment = new Apartment { ApartmentNumber = " 101 ", Floor = 1, Area = 60 };
        _mockApartmentRepo.Setup(r => r.GetByApartmentNumberAsync("101")).ReturnsAsync((Apartment?)null);
        _mockApartmentRepo.Setup(r => r.AddAsync(It.IsAny<Apartment>())).Returns(Task.CompletedTask);
        _mockUow.Setup(u => u.CompleteAsync()).ReturnsAsync(1);

        // Act
        var created = await _service.CreateApartmentAsync(apartment);

        // Assert
        Assert.NotNull(created);
        Assert.Equal("101", created.ApartmentNumber);
        Assert.Equal(ApartmentStatus.Empty, created.Status);
        _mockApartmentRepo.Verify(r => r.AddAsync(It.IsAny<Apartment>()), Times.Once);
        _mockUow.Verify(u => u.CompleteAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateApartment_DuplicateNumber_ShouldThrowBusinessRuleException()
    {
        // Arrange
        var apartment = new Apartment { ApartmentNumber = "101", Floor = 1, Area = 50 };
        _mockApartmentRepo.Setup(r => r.GetByApartmentNumberAsync("101")).ReturnsAsync(apartment);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => _service.CreateApartmentAsync(apartment));
        Assert.Contains("đã tồn tại trong hệ thống", ex.Message);
    }

    [Fact]
    public async Task CreateApartment_EmptyNumber_ShouldThrowBusinessRuleException()
    {
        var apartment = new Apartment { ApartmentNumber = "   ", Floor = 1, Area = 50 };

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => _service.CreateApartmentAsync(apartment));
        Assert.Contains("không được để trống", ex.Message);
    }

    [Fact]
    public async Task CreateApartment_InvalidFloor_ShouldThrowBusinessRuleException()
    {
        var apartment = new Apartment { ApartmentNumber = "101", Floor = 0, Area = 50 };

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => _service.CreateApartmentAsync(apartment));
        Assert.Contains("Số tầng phải lớn hơn 0", ex.Message);
    }

    [Fact]
    public async Task CreateApartment_NonFiniteArea_ShouldThrowBusinessRuleException()
    {
        var apartment = new Apartment { ApartmentNumber = "101", Floor = 1, Area = double.NaN };

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => _service.CreateApartmentAsync(apartment));
        Assert.Contains("Diện tích", ex.Message);
    }

    [Fact]
    public async Task UpdateApartment_ValidData_ShouldUpdateSuccessfully()
    {
        // Arrange
        var existing = new Apartment { Id = 1, ApartmentNumber = "101", Floor = 1, Area = 50, Status = ApartmentStatus.Empty };
        var update = new Apartment { Id = 1, ApartmentNumber = "101-A", Floor = 2, Area = 65, Status = ApartmentStatus.UnderMaintenance };

        _mockApartmentRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);
        _mockApartmentRepo.Setup(r => r.GetByApartmentNumberAsync("101-A")).ReturnsAsync((Apartment?)null);
        _mockUow.Setup(u => u.CompleteAsync()).ReturnsAsync(1);

        // Act
        await _service.UpdateApartmentAsync(update);

        // Assert
        Assert.Equal("101-A", existing.ApartmentNumber);
        Assert.Equal(2, existing.Floor);
        Assert.Equal(65, existing.Area);
        Assert.Equal(ApartmentStatus.UnderMaintenance, existing.Status);
        _mockApartmentRepo.Verify(r => r.Update(existing), Times.Once);
        _mockUow.Verify(u => u.CompleteAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateApartment_DuplicateNumberOnOtherApartment_ShouldThrowBusinessRuleException()
    {
        // Arrange
        var existing = new Apartment { Id = 1, ApartmentNumber = "101", Floor = 1, Area = 50 };
        var duplicate = new Apartment { Id = 2, ApartmentNumber = "102", Floor = 1, Area = 50 };
        var update = new Apartment { Id = 1, ApartmentNumber = "102", Floor = 1, Area = 50 };

        _mockApartmentRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);
        _mockApartmentRepo.Setup(r => r.GetByApartmentNumberAsync("102")).ReturnsAsync(duplicate);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => _service.UpdateApartmentAsync(update));
        Assert.Contains("đã được sử dụng bởi căn hộ khác", ex.Message);
    }

    [Fact]
    public async Task DeleteApartment_NonExisting_ShouldThrowBusinessRuleException()
    {
        _mockApartmentRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Apartment?)null);

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => _service.DeleteApartmentAsync(99));
        Assert.Contains("Không tìm thấy căn hộ cần xóa", ex.Message);
    }

    [Fact]
    public async Task DeleteApartment_Existing_ShouldDeleteSuccessfully()
    {
        // Arrange
        var apartment = new Apartment { Id = 1, ApartmentNumber = "101" };
        _mockApartmentRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(apartment);
        _mockUow.Setup(u => u.CompleteAsync()).ReturnsAsync(1);

        // Act
        await _service.DeleteApartmentAsync(1);

        // Assert
        _mockApartmentRepo.Verify(r => r.Delete(apartment), Times.Once);
        _mockUow.Verify(u => u.CompleteAsync(), Times.Once);
    }
}
