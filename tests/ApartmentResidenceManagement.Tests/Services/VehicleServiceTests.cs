using System;
using System.Linq;
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
        var apartments = new Mock<IApartmentRepository>();
        apartments.Setup(r => r.LockForVehicleQuotaAsync(It.IsAny<int>()))
            .ReturnsAsync((int id) => new Apartment { Id = id });
        _mockUow.Setup(u => u.Apartments).Returns(apartments.Object);
        _mockUow.Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task<Vehicle>>>()))
            .Returns((Func<Task<Vehicle>> action) => action());
        _mockUow.Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task<bool>>>()))
            .Returns((Func<Task<bool>> action) => action());
        _mockResidenceRepo.Setup(r => r.GetActiveByApartmentIdAsync(It.IsAny<int>()))
            .ReturnsAsync(new[] { new ResidenceHistory { ResidentId = 10, IsActive = true } });
        _mockVehicleRepo.Setup(r => r.GetAllWithDetailsAsync()).ReturnsAsync(Array.Empty<Vehicle>());
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
    [Fact]
    public async Task SubmitVehicleRegistration_InactiveOwner_ShouldNotSave()
    {
        _mockResidentRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(new Resident { Id = 10 });
        _mockResidenceRepo.Setup(r => r.GetActiveByResidentIdAsync(10)).ReturnsAsync((ResidenceHistory?)null);
        await Assert.ThrowsAsync<BusinessRuleException>(() => _service.SubmitVehicleRegistrationAsync(
            new Vehicle { OwnerId = 10, LicensePlate = "29A1-12345", VehicleType = VehicleType.Moto }));
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
    public async Task SubmitRegistration_SavesColorAndTimestampWithoutConsumingQuota()
    {
        _mockResidentRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(new Resident { Id = 10 });
        var before = DateTime.UtcNow;
        var vehicle = new Vehicle { OwnerId = 10, LicensePlate = "29A1-12345", VehicleType = VehicleType.Moto, Color = "  Đỏ  " };
        var saved = await _service.SubmitVehicleRegistrationAsync(vehicle);
        Assert.Equal("Đỏ", saved.Color);
        Assert.InRange(saved.RegisteredAtUtc!.Value, before, DateTime.UtcNow);
        Assert.Equal(VehicleRegistrationStatus.Pending, saved.RegistrationStatus);
        _mockVehicleRepo.Verify(r => r.GetAllWithDetailsAsync(), Times.Never);
    }

    [Fact]
    public async Task Approve_FullMotorcycleQuota_RejectsAndDoesNotSave()
    {
        var pending = new Vehicle { Id = 9, OwnerId = 10, VehicleType = VehicleType.Moto, RegistrationStatus = VehicleRegistrationStatus.Pending };
        _mockVehicleRepo.Setup(r => r.GetByIdAsync(9)).ReturnsAsync(pending);
        _mockResidentRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(new Resident { Id = 10 });
        _mockVehicleRepo.Setup(r => r.GetAllWithDetailsAsync()).ReturnsAsync(Enumerable.Range(1, 3).Select(i => new Vehicle { Id = i, OwnerId = 10, VehicleType = VehicleType.Moto, RegistrationStatus = VehicleRegistrationStatus.Approved }).ToArray());
        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => _service.ReviewVehicleAsync(9, VehicleRegistrationStatus.Approved));
        Assert.Contains("hạn mức", ex.Message);
        Assert.Equal(VehicleRegistrationStatus.Pending, pending.RegistrationStatus);
        _mockUow.Verify(u => u.CompleteAsync(), Times.Never);
    }

    [Fact]
    public async Task Approve_CarQuotaCountsOtherActiveResidentsInApartment()
    {
        var pending = new Vehicle { Id = 9, OwnerId = 10, VehicleType = VehicleType.Car, RegistrationStatus = VehicleRegistrationStatus.Pending };
        _mockVehicleRepo.Setup(r => r.GetByIdAsync(9)).ReturnsAsync(pending);
        _mockResidentRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(new Resident { Id = 10 });
        _mockResidenceRepo.Setup(r => r.GetActiveByApartmentIdAsync(It.IsAny<int>())).ReturnsAsync(new[] { new ResidenceHistory { ResidentId = 10 }, new ResidenceHistory { ResidentId = 20 } });
        _mockVehicleRepo.Setup(r => r.GetAllWithDetailsAsync()).ReturnsAsync(new[] { new Vehicle { Id = 1, OwnerId = 20, VehicleType = VehicleType.Car, RegistrationStatus = VehicleRegistrationStatus.Approved } });
        await Assert.ThrowsAsync<BusinessRuleException>(() => _service.ReviewVehicleAsync(9, VehicleRegistrationStatus.Approved));
        _mockUow.Verify(u => u.CompleteAsync(), Times.Never);
    }

    [Fact]
    public async Task Approve_BicyclesHaveNoQuota()
    {
        var pending = new Vehicle { Id = 9, OwnerId = 10, VehicleType = VehicleType.Bicycle, RegistrationStatus = VehicleRegistrationStatus.Pending };
        _mockVehicleRepo.Setup(r => r.GetByIdAsync(9)).ReturnsAsync(pending);
        _mockResidentRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(new Resident { Id = 10 });
        await _service.ReviewVehicleAsync(9, VehicleRegistrationStatus.Approved);
        Assert.Equal(VehicleRegistrationStatus.Approved, pending.RegistrationStatus);
    }

    [Fact]
    public async Task UpdateRegistration_PreservesDateAndStoresColor()
    {
        var timestamp = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var existing = new Vehicle { Id = 5, OwnerId = 10, LicensePlate = "29A1-12345", RegisteredAtUtc = timestamp };
        _mockVehicleRepo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(existing);
        _mockResidentRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(new Resident { Id = 10 });
        await _service.UpdateVehicleRegistrationAsync(10, new Vehicle { Id = 5, OwnerId = 10, LicensePlate = "29A1-12345", Color = " Xanh " });
        Assert.Equal("Xanh", existing.Color);
        Assert.Equal(timestamp, existing.RegisteredAtUtc);
        Assert.Equal(VehicleRegistrationStatus.Pending, existing.RegistrationStatus);
    }
    [Fact]
    public async Task CreateByAdmin_FullCarQuota_RejectsBeforeSaving()
    {
        _mockResidentRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(new Resident { Id = 10 });
        _mockVehicleRepo.Setup(r => r.GetAllWithDetailsAsync()).ReturnsAsync(new[] { new Vehicle { Id = 1, OwnerId = 10, VehicleType = VehicleType.Car, RegistrationStatus = VehicleRegistrationStatus.Approved } });
        await Assert.ThrowsAsync<BusinessRuleException>(() => _service.CreateVehicleAsync(new Vehicle { OwnerId = 10, LicensePlate = "30A-12345", VehicleType = VehicleType.Car }));
        _mockVehicleRepo.Verify(r => r.AddAsync(It.IsAny<Vehicle>()), Times.Never);
        _mockUow.Verify(u => u.CompleteAsync(), Times.Never);
    }

    [Fact]
    public async Task Approve_PendingAndRejectedVehiclesDoNotConsumeQuota()
    {
        var pending = new Vehicle { Id = 9, OwnerId = 10, VehicleType = VehicleType.Car, RegistrationStatus = VehicleRegistrationStatus.Pending };
        _mockVehicleRepo.Setup(r => r.GetByIdAsync(9)).ReturnsAsync(pending);
        _mockResidentRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(new Resident { Id = 10 });
        _mockVehicleRepo.Setup(r => r.GetAllWithDetailsAsync()).ReturnsAsync(new[] {
            pending, new Vehicle { Id = 1, OwnerId = 10, VehicleType = VehicleType.Car, RegistrationStatus = VehicleRegistrationStatus.Rejected } });
        await _service.ReviewVehicleAsync(9, VehicleRegistrationStatus.Approved);
        Assert.Equal(VehicleRegistrationStatus.Approved, pending.RegistrationStatus);
    }

    [Fact]
    public async Task UpdateApprovedVehicle_DoesNotCountItselfAgainstQuota()
    {
        var existing = new Vehicle { Id = 1, OwnerId = 10, VehicleType = VehicleType.Car, LicensePlate = "30A-12345", RegistrationStatus = VehicleRegistrationStatus.Approved };
        _mockVehicleRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);
        _mockResidentRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(new Resident { Id = 10 });
        _mockVehicleRepo.Setup(r => r.GetAllWithDetailsAsync()).ReturnsAsync(new[] { existing });
        await _service.UpdateVehicleAsync(new Vehicle { Id = 1, OwnerId = 10, LicensePlate = "30A-12345", VehicleType = VehicleType.Car, Color = "Trắng" });
        Assert.Equal("Trắng", existing.Color);
        _mockUow.Verify(u => u.CompleteAsync(), Times.Once);
    }
}
