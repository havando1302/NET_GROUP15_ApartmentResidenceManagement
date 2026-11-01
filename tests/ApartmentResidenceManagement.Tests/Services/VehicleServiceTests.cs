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

    public VehicleServiceTests()
    {
        _mockUow = new Mock<IUnitOfWork>();
        _mockVehicleRepo = new Mock<IVehicleRepository>();
        _mockResidentRepo = new Mock<IResidentRepository>();

        _mockUow.Setup(u => u.Vehicles).Returns(_mockVehicleRepo.Object);
        _mockUow.Setup(u => u.Residents).Returns(_mockResidentRepo.Object);

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
    public async Task SubmitVehicleRegistration_ValidData_ShouldRemainPending()
    {
        var vehicle = new Vehicle { LicensePlate = "29A1-12345", OwnerId = 10, VehicleType = VehicleType.Moto };
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
    public async Task ReviewVehicle_ApproveInactiveOwner_ShouldRejectOperation()
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
    public async Task ReviewVehicle_Reject_ShouldUpdateStatus()
    {
        var vehicle = new Vehicle { Id = 5, OwnerId = 10, RegistrationStatus = VehicleRegistrationStatus.Pending };
        _mockVehicleRepo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(vehicle);

        await _service.ReviewVehicleAsync(5, VehicleRegistrationStatus.Rejected);

        Assert.Equal(VehicleRegistrationStatus.Rejected, vehicle.RegistrationStatus);
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
}
