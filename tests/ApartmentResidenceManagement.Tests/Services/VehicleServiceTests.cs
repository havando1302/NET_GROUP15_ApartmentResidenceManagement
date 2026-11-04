using System.Threading.Tasks;
using ApartmentResidenceManagement.Domain.Entities;
using ApartmentResidenceManagement.Domain.Enums;
using ApartmentResidenceManagement.Domain.Exceptions;
using ApartmentResidenceManagement.Domain.Interfaces;
using ApartmentResidenceManagement.Application.Services;
using Moq;
using Xunit;

namespace ApartmentResidenceManagement.Tests.Services;

public class VehicleServiceTests
{
    private readonly Mock<IUnitOfWork> _mockUow;
    private readonly Mock<IVehicleRepository> _mockVehicleRepo;
    private readonly Mock<IResidentRepository> _mockResidentRepo;
    private readonly VehicleService _service;
    private readonly Mock<IResidenceHistoryRepository> _mockResidenceRepo;

    public VehicleServiceTests()
    {
        _mockUow = new Mock<IUnitOfWork>();
        _mockVehicleRepo = new Mock<IVehicleRepository>();
        _mockResidentRepo = new Mock<IResidentRepository>();

        _mockUow.Setup(u => u.Vehicles).Returns(_mockVehicleRepo.Object);
        _mockUow.Setup(u => u.Residents).Returns(_mockResidentRepo.Object);

        _mockResidenceRepo = new Mock<IResidenceHistoryRepository>();
        _mockUow.Setup(u => u.ResidenceHistories).Returns(_mockResidenceRepo.Object);
        _mockResidenceRepo.Setup(r => r.GetActiveByResidentIdAsync(It.IsAny<int>()))
            .ReturnsAsync((int id) => new ResidenceHistory { ResidentId = id, IsActive = true });
        _service = new VehicleService(_mockUow.Object);
    }

    [Fact]
    public async Task CreateVehicle_OwnerNotFound_ShouldThrowBusinessRuleException()
    {
        // Arrange
        int ownerId = 10;
        var vehicle = new Vehicle { LicensePlate = "29A1-12345", OwnerId = ownerId };
        _mockResidentRepo.Setup(r => r.GetByIdAsync(ownerId)).ReturnsAsync((Resident?)null);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => _service.CreateVehicleAsync(vehicle));
        Assert.Contains("Không tìm thấy cư dân sở hữu phương tiện", ex.Message);
    }

    [Fact]
    public async Task CreateVehicle_DuplicateLicensePlate_ShouldThrowBusinessRuleException()
    {
        // Arrange
        int ownerId = 10;
        var vehicle = new Vehicle { LicensePlate = "29A1-12345", OwnerId = ownerId };

        _mockResidentRepo.Setup(r => r.GetByIdAsync(ownerId)).ReturnsAsync(new Resident { Id = ownerId });
        _mockVehicleRepo.Setup(r => r.GetByLicensePlateAsync("29A1-12345")).ReturnsAsync(vehicle);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => _service.CreateVehicleAsync(vehicle));
        Assert.Contains("đã tồn tại trong hệ thống", ex.Message);
    }

    [Fact]
    public async Task SubmitVehicleRegistration_WithoutImage_ShouldThrowBusinessRuleException()
    {
        var vehicle = new Vehicle { LicensePlate = "29A1-12345", OwnerId = 10, VehicleType = VehicleType.Moto, ImagePath = null };
        _mockResidentRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(new Resident { Id = 10 });

        await Assert.ThrowsAsync<BusinessRuleException>(() => _service.SubmitVehicleRegistrationAsync(vehicle));
        _mockVehicleRepo.Verify(r => r.AddAsync(It.IsAny<Vehicle>()), Times.Never);
    }

    [Fact]
    public async Task SubmitVehicleRegistration_ValidData_ShouldRemainPending()
    {
        var vehicle = new Vehicle { LicensePlate = "29A1-12345", OwnerId = 10, VehicleType = VehicleType.Moto, ImagePath = "/uploads/vehicles/test.jpg" };
        _mockResidentRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(new Resident { Id = 10 });

        var result = await _service.SubmitVehicleRegistrationAsync(vehicle);

        Assert.Equal(VehicleRegistrationStatus.Pending, result.RegistrationStatus);
        _mockVehicleRepo.Verify(r => r.AddAsync(vehicle), Times.Once);
        _mockUow.Verify(u => u.CompleteAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateVehicle_ByAdmin_ShouldBeApprovedImmediately()
    {
        var vehicle = new Vehicle { LicensePlate = "30A-12345", OwnerId = 10, VehicleType = VehicleType.Car };
        _mockResidentRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(new Resident { Id = 10 });

        var result = await _service.CreateVehicleAsync(vehicle);

        Assert.Equal(VehicleRegistrationStatus.Approved, result.RegistrationStatus);
    }

    [Fact]
    public async Task ReviewVehicle_ApproveMissingOwner_ShouldRejectOperation()
    {
        var vehicle = new Vehicle { Id = 5, OwnerId = 10, RegistrationStatus = VehicleRegistrationStatus.Pending };
        _mockVehicleRepo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(vehicle);
        _mockResidentRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync((Resident?)null);

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            _service.ReviewVehicleAsync(5, VehicleRegistrationStatus.Approved));

        Assert.Equal(VehicleRegistrationStatus.Pending, vehicle.RegistrationStatus);
        _mockUow.Verify(u => u.CompleteAsync(), Times.Never);
    }

    [Fact]
    public async Task ReviewVehicle_Reject_ShouldUpdateStatusAndSaveRejectReason()
    {
        var vehicle = new Vehicle { Id = 5, OwnerId = 10, RegistrationStatus = VehicleRegistrationStatus.Pending };
        _mockVehicleRepo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(vehicle);

        await _service.ReviewVehicleAsync(5, VehicleRegistrationStatus.Rejected, "Không đủ chỗ để xe trong hầm");

        Assert.Equal(VehicleRegistrationStatus.Rejected, vehicle.RegistrationStatus);
        Assert.Equal("Không đủ chỗ để xe trong hầm", vehicle.RejectReason);
        _mockUow.Verify(u => u.CompleteAsync(), Times.Once);
    }

    [Fact]
    public async Task ReviewVehicle_Approve_ShouldClearRejectReason()
    {
        var vehicle = new Vehicle { Id = 5, OwnerId = 10, RegistrationStatus = VehicleRegistrationStatus.Rejected, RejectReason = "Lý do cũ" };
        _mockVehicleRepo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(vehicle);
        _mockResidentRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(new Resident { Id = 10 });

        await _service.ReviewVehicleAsync(5, VehicleRegistrationStatus.Approved);

        Assert.Equal(VehicleRegistrationStatus.Approved, vehicle.RegistrationStatus);
        Assert.Null(vehicle.RejectReason);
        _mockUow.Verify(u => u.CompleteAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateVehicleRegistration_DifferentOwner_ShouldRejectOperation()
    {
        var existing = new Vehicle { Id = 5, OwnerId = 10, LicensePlate = "29A1-12345" };
        var update = new Vehicle { Id = 5, OwnerId = 11, LicensePlate = "29A1-54321" };
        _mockVehicleRepo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(existing);

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            _service.UpdateVehicleRegistrationAsync(11, update));

        _mockVehicleRepo.Verify(r => r.Update(It.IsAny<Vehicle>()), Times.Never);
    }
    [Fact]
    public async Task SubmitVehicleRegistration_InactiveOwner_ShouldNotSave()
    {
        _mockResidentRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(new Resident { Id = 10 });
        _mockResidenceRepo.Setup(r => r.GetActiveByResidentIdAsync(10)).ReturnsAsync((ResidenceHistory?)null);
        await Assert.ThrowsAsync<BusinessRuleException>(() => _service.SubmitVehicleRegistrationAsync(
            new Vehicle { OwnerId = 10, LicensePlate = "29A1-12345", VehicleType = VehicleType.Moto, ImagePath = "/uploads/vehicles/test.jpg" }));
        _mockVehicleRepo.Verify(r => r.AddAsync(It.IsAny<Vehicle>()), Times.Never);
        _mockUow.Verify(u => u.CompleteAsync(), Times.Never);
    }

    [Fact]
    public async Task ReviewVehicle_ApproveInactiveResidence_ShouldNotChangeStatus()
    {
        var vehicle = new Vehicle { Id = 5, OwnerId = 10, RegistrationStatus = VehicleRegistrationStatus.Pending };
        _mockVehicleRepo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(vehicle);
        _mockResidentRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(new Resident { Id = 10 });
        _mockResidenceRepo.Setup(r => r.GetActiveByResidentIdAsync(10)).ReturnsAsync((ResidenceHistory?)null);
        await Assert.ThrowsAsync<BusinessRuleException>(() => _service.ReviewVehicleAsync(5, VehicleRegistrationStatus.Approved));
        Assert.Equal(VehicleRegistrationStatus.Pending, vehicle.RegistrationStatus);
        _mockUow.Verify(u => u.CompleteAsync(), Times.Never);
    }

    [Fact]
    public async Task ReviewVehicle_ApproveActiveResidence_ShouldSaveApprovedStatus()
    {
        var vehicle = new Vehicle { Id = 5, OwnerId = 10, RegistrationStatus = VehicleRegistrationStatus.Pending };
        _mockVehicleRepo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(vehicle);
        _mockResidentRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(new Resident { Id = 10 });
        await _service.ReviewVehicleAsync(5, VehicleRegistrationStatus.Approved);
        Assert.Equal(VehicleRegistrationStatus.Approved, vehicle.RegistrationStatus);
        _mockUow.Verify(u => u.CompleteAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateVehicle_WithColorAndImagePath_ShouldSucceed()
    {
        int ownerId = 10;
        var vehicle = new Vehicle 
        { 
            LicensePlate = "29A-123.45", 
            OwnerId = ownerId, 
            VehicleType = VehicleType.Moto, 
            Brand = "Honda", 
            Color = "Đen", 
            ImagePath = "/Assets/car.png" 
        };

        _mockResidentRepo.Setup(r => r.GetByIdAsync(ownerId)).ReturnsAsync(new Resident { Id = ownerId });
        _mockVehicleRepo.Setup(r => r.GetByLicensePlateAsync(It.IsAny<string>())).ReturnsAsync((Vehicle?)null);

        var created = await _service.CreateVehicleAsync(vehicle);

        Assert.Equal("29A-123.45", created.LicensePlate);
        Assert.Equal("Đen", created.Color);
        Assert.Equal("/Assets/car.png", created.ImagePath);
        Assert.NotEqual(default, created.RegistrationDate);
        _mockVehicleRepo.Verify(r => r.AddAsync(vehicle), Times.Once);
        _mockUow.Verify(u => u.CompleteAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateVehicle_WithColorAndImagePath_ShouldUpdateExistingFields()
    {
        int vehicleId = 7;
        var existing = new Vehicle 
        { 
            Id = vehicleId, 
            LicensePlate = "30G-456.78", 
            OwnerId = 10, 
            VehicleType = VehicleType.Car, 
            Brand = "Toyota", 
            Color = "Trắng" 
        };

        var updated = new Vehicle 
        { 
            Id = vehicleId, 
            LicensePlate = "30G-456.78", 
            OwnerId = 10, 
            VehicleType = VehicleType.Car, 
            Brand = "Toyota", 
            Color = "Đen", 
            ImagePath = "/Assets/new_car.png" 
        };

        _mockVehicleRepo.Setup(r => r.GetByIdAsync(vehicleId)).ReturnsAsync(existing);
        _mockResidentRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(new Resident { Id = 10 });
        _mockVehicleRepo.Setup(r => r.GetByLicensePlateAsync("30G-456.78")).ReturnsAsync(existing);

        await _service.UpdateVehicleAsync(updated);

        Assert.Equal("Đen", existing.Color);
        Assert.Equal("/Assets/new_car.png", existing.ImagePath);
        _mockVehicleRepo.Verify(r => r.Update(existing), Times.Once);
        _mockUow.Verify(u => u.CompleteAsync(), Times.Once);
    }
}
