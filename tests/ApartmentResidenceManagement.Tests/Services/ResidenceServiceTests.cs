using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApartmentResidenceManagement.Application.Common;
using ApartmentResidenceManagement.Domain.Entities;
using ApartmentResidenceManagement.Domain.Enums;
using ApartmentResidenceManagement.Domain.Interfaces;
using ApartmentResidenceManagement.Application.Services;
using Moq;
using Xunit;

namespace ApartmentResidenceManagement.Tests.Services;

public class ResidenceServiceTests
{
    private readonly Mock<IUnitOfWork> _mockUow;
    private readonly Mock<IApartmentRepository> _mockApartmentRepo;
    private readonly Mock<IResidentRepository> _mockResidentRepo;
    private readonly Mock<IResidenceHistoryRepository> _mockResidenceHistoryRepo;
    private readonly ResidenceService _service;

    public ResidenceServiceTests()
    {
        _mockUow = new Mock<IUnitOfWork>();
        _mockApartmentRepo = new Mock<IApartmentRepository>();
        _mockResidentRepo = new Mock<IResidentRepository>();
        _mockResidenceHistoryRepo = new Mock<IResidenceHistoryRepository>();

        _mockUow.Setup(u => u.Apartments).Returns(_mockApartmentRepo.Object);
        _mockUow.Setup(u => u.Residents).Returns(_mockResidentRepo.Object);
        _mockUow.Setup(u => u.ResidenceHistories).Returns(_mockResidenceHistoryRepo.Object);
        _mockUow
            .Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task<OperationResult<ResidenceHistory>>>>()))
            .Returns((Func<Task<OperationResult<ResidenceHistory>>> operation) => operation());

        _service = new ResidenceService(_mockUow.Object);
    }

    [Fact]
    public async Task RegisterResidence_ValidData_ShouldSucceed()
    {
        // Arrange
        int apartmentId = 1;
        int residentId = 10;
        var apartment = new Apartment { Id = apartmentId, ApartmentNumber = "101", Status = ApartmentStatus.Empty };
        var resident = new Resident { Id = residentId, FullName = "Nguyen Van A", DateOfBirth = new DateTime(1990, 1, 1) };

        _mockApartmentRepo.Setup(r => r.GetByIdAsync(apartmentId)).ReturnsAsync(apartment);
        _mockResidentRepo.Setup(r => r.GetByIdAsync(residentId)).ReturnsAsync(resident);
        _mockResidenceHistoryRepo.Setup(r => r.GetActiveByResidentIdAsync(residentId)).ReturnsAsync((ResidenceHistory?)null);
        _mockResidenceHistoryRepo.Setup(r => r.GetActiveByApartmentIdAsync(apartmentId)).ReturnsAsync(new List<ResidenceHistory>());

        // Act
        var result = await _service.RegisterResidenceAsync(apartmentId, residentId, RelationshipType.Owner, DateTime.Now);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(apartmentId, result.Value.ApartmentId);
        Assert.Equal(residentId, result.Value.ResidentId);
        Assert.Equal(RelationshipType.Owner, result.Value.RelationshipType);
        Assert.True(result.Value.IsActive);
        Assert.Equal(ApartmentStatus.Occupied, apartment.Status); // Căn hộ phải đổi sang Occupied
        _mockUow.Verify(u => u.CompleteAsync(), Times.Once);
    }

    [Fact]
    public async Task RegisterResidence_ResidentAlreadyHasActiveResidence_ReturnsFailure()
    {
        // Arrange
        int apartmentId = 1;
        int residentId = 10;
        var apartment = new Apartment { Id = apartmentId, ApartmentNumber = "101" };
        var resident = new Resident { Id = residentId, FullName = "Nguyen Van A" };
        var existingActive = new ResidenceHistory 
        { 
            ApartmentId = 2, 
            ResidentId = residentId, 
            IsActive = true,
            Apartment = new Apartment { Id = 2, ApartmentNumber = "202" }
        };

        _mockApartmentRepo.Setup(r => r.GetByIdAsync(apartmentId)).ReturnsAsync(apartment);
        _mockResidentRepo.Setup(r => r.GetByIdAsync(residentId)).ReturnsAsync(resident);
        _mockResidenceHistoryRepo.Setup(r => r.GetActiveByResidentIdAsync(residentId)).ReturnsAsync(existingActive);

        var result = await _service.RegisterResidenceAsync(
            apartmentId, residentId, RelationshipType.Tenant, DateTime.Now);

        Assert.False(result.IsSuccess);
        Assert.Contains("hiện đang cư trú tại căn hộ", result.ErrorMessage);
    }

    [Fact]
    public async Task RegisterResidence_ApartmentUnderMaintenance_ReturnsFailure()
    {
        // Arrange
        int apartmentId = 1;
        int residentId = 10;
        var apartment = new Apartment { Id = apartmentId, ApartmentNumber = "101", Status = ApartmentStatus.UnderMaintenance };

        _mockApartmentRepo.Setup(r => r.GetByIdAsync(apartmentId)).ReturnsAsync(apartment);

        var result = await _service.RegisterResidenceAsync(
            apartmentId, residentId, RelationshipType.Owner, DateTime.Now);

        Assert.False(result.IsSuccess);
        Assert.Contains("đang sửa chữa", result.ErrorMessage);
    }

    [Fact]
    public async Task RegisterResidence_AddSecondOwnerToSameApartment_ReturnsFailure()
    {
        // Arrange
        int apartmentId = 1;
        int residentId = 10;
        var apartment = new Apartment { Id = apartmentId, ApartmentNumber = "101", Status = ApartmentStatus.Occupied };
        var resident = new Resident { Id = residentId, FullName = "Nguyen Van B", DateOfBirth = new DateTime(1990, 1, 1) };
        var existingOwner = new ResidenceHistory { ApartmentId = apartmentId, ResidentId = 9, RelationshipType = RelationshipType.Owner, IsActive = true };

        _mockApartmentRepo.Setup(r => r.GetByIdAsync(apartmentId)).ReturnsAsync(apartment);
        _mockResidentRepo.Setup(r => r.GetByIdAsync(residentId)).ReturnsAsync(resident);
        _mockResidenceHistoryRepo.Setup(r => r.GetActiveByResidentIdAsync(residentId)).ReturnsAsync((ResidenceHistory?)null);
        _mockResidenceHistoryRepo.Setup(r => r.GetActiveByApartmentIdAsync(apartmentId)).ReturnsAsync(new List<ResidenceHistory> { existingOwner });

        var result = await _service.RegisterResidenceAsync(
            apartmentId, residentId, RelationshipType.Owner, DateTime.Now);

        Assert.False(result.IsSuccess);
        Assert.Contains("đã có Chủ hộ", result.ErrorMessage);
    }

    [Fact]
    public async Task RegisterResidence_OverlappingHistory_ShouldRejectOperation()
    {
        var apartment = new Apartment { Id = 1, ApartmentNumber = "101", Status = ApartmentStatus.Empty };
        var resident = new Resident { Id = 10, DateOfBirth = new DateTime(1990, 1, 1) };
        _mockApartmentRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(apartment);
        _mockResidentRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(resident);
        _mockResidenceHistoryRepo.Setup(r => r.GetActiveByResidentIdAsync(10)).ReturnsAsync((ResidenceHistory?)null);
        _mockResidenceHistoryRepo.Setup(r => r.GetActiveByApartmentIdAsync(1))
            .ReturnsAsync(new List<ResidenceHistory>());
        _mockResidenceHistoryRepo.Setup(r => r.GetHistoryByResidentIdAsync(10))
            .ReturnsAsync(new List<ResidenceHistory>
            {
                new() { ResidentId = 10, StartDate = new DateTime(2026, 1, 1), EndDate = new DateTime(2026, 6, 30), IsActive = false }
            });

        var result = await _service.RegisterResidenceAsync(
            1, 10, RelationshipType.Tenant, new DateTime(2026, 3, 1));

        Assert.False(result.IsSuccess);
        Assert.Contains("chồng lấn", result.ErrorMessage);
        Assert.Equal(ApartmentStatus.Empty, apartment.Status);
        _mockUow.Verify(u => u.CompleteAsync(), Times.Never);
    }

    [Fact]
    public async Task TerminateResidence_WhenNoActiveResidentLeft_ShouldSetApartmentEmpty()
    {
        // Arrange: Căn hộ chỉ có 1 cư dân đang ở, kết thúc cư trú thì căn hộ phải chuyển sang Empty
        var residence = new ResidenceHistory
        {
            Id = 5,
            ApartmentId = 1,
            ResidentId = 10,
            StartDate = DateTime.Today.AddMonths(-1),
            IsActive = true
        };
        var apartment = new Apartment { Id = 1, Status = ApartmentStatus.Occupied };
        _mockResidenceHistoryRepo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(residence);
        _mockResidenceHistoryRepo.Setup(r => r.GetActiveByApartmentIdAsync(1))
            .ReturnsAsync(new List<ResidenceHistory> { residence });
        _mockApartmentRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(apartment);

        // Act
        var result = await _service.TerminateResidenceAsync(5, DateTime.Today);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(residence.IsActive);
        Assert.Equal(DateTime.Today, residence.EndDate);
        Assert.Equal(ApartmentStatus.Empty, apartment.Status);
        _mockUow.Verify(u => u.CompleteAsync(), Times.Once);
    }

    [Fact]
    public async Task TerminateResidence_WithRemainingResident_ShouldKeepApartmentOccupied()
    {
        // Arrange: Căn hộ có nhiều cư dân, một người rời đi thì căn hộ vẫn là Occupied
        var residence = new ResidenceHistory
        {
            Id = 5,
            ApartmentId = 1,
            ResidentId = 10,
            StartDate = DateTime.Today.AddMonths(-1),
            IsActive = true
        };
        var remainingResidence = new ResidenceHistory { Id = 6, ApartmentId = 1, ResidentId = 11, IsActive = true };
        var apartment = new Apartment { Id = 1, Status = ApartmentStatus.Occupied };
        _mockResidenceHistoryRepo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(residence);
        _mockResidenceHistoryRepo.Setup(r => r.GetActiveByApartmentIdAsync(1))
            .ReturnsAsync(new List<ResidenceHistory> { residence, remainingResidence });
        _mockApartmentRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(apartment);

        // Act
        var result = await _service.TerminateResidenceAsync(5, DateTime.Today);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(residence.IsActive);
        Assert.Equal(DateTime.Today, residence.EndDate);
        Assert.Equal(ApartmentStatus.Occupied, apartment.Status);
        _mockUow.Verify(u => u.CompleteAsync(), Times.Once);
    }

    [Fact]
    public async Task TerminateResidence_EndDateInFuture_ReturnsFailure()
    {
        var residence = new ResidenceHistory
        {
            Id = 5,
            ApartmentId = 1,
            ResidentId = 10,
            StartDate = DateTime.Today.AddMonths(-1),
            IsActive = true
        };
        _mockResidenceHistoryRepo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(residence);

        var result = await _service.TerminateResidenceAsync(5, DateTime.Today.AddDays(1));

        Assert.False(result.IsSuccess);
        Assert.Contains("tương lai", result.ErrorMessage);
        Assert.True(residence.IsActive);
        _mockUow.Verify(u => u.CompleteAsync(), Times.Never);
    }

    [Fact]
    public async Task TerminateResidence_EndDateBeforeStartDate_ReturnsFailure()
    {
        var residence = new ResidenceHistory
        {
            Id = 5,
            ApartmentId = 1,
            ResidentId = 10,
            StartDate = DateTime.Today.AddDays(-5),
            IsActive = true
        };
        _mockResidenceHistoryRepo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(residence);

        var result = await _service.TerminateResidenceAsync(5, DateTime.Today.AddDays(-6));

        Assert.False(result.IsSuccess);
        Assert.Contains("nhỏ hơn ngày bắt đầu", result.ErrorMessage);
        Assert.True(residence.IsActive);
        _mockUow.Verify(u => u.CompleteAsync(), Times.Never);
    }

    [Fact]
    public async Task TerminateResidence_AlreadyTerminated_ReturnsFailure()
    {
        var residence = new ResidenceHistory
        {
            Id = 5,
            ApartmentId = 1,
            ResidentId = 10,
            StartDate = DateTime.Today.AddMonths(-2),
            EndDate = DateTime.Today.AddMonths(-1),
            IsActive = false
        };
        _mockResidenceHistoryRepo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(residence);

        var result = await _service.TerminateResidenceAsync(5, DateTime.Today);

        Assert.False(result.IsSuccess);
        Assert.Contains("đã được kết thúc trước đó", result.ErrorMessage);
        _mockUow.Verify(u => u.CompleteAsync(), Times.Never);
    }

    [Fact]
    public async Task TerminateResidence_NotFound_ReturnsFailure()
    {
        _mockResidenceHistoryRepo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((ResidenceHistory?)null);

        var result = await _service.TerminateResidenceAsync(999, DateTime.Today);

        Assert.False(result.IsSuccess);
        Assert.Contains("Không tìm thấy thông tin cư trú", result.ErrorMessage);
        _mockUow.Verify(u => u.CompleteAsync(), Times.Never);
    }
}
