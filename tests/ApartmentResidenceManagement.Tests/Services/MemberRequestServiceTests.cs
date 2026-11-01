using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using ApartmentResidenceManagement.Application.Services;
using ApartmentResidenceManagement.Domain.Entities;
using ApartmentResidenceManagement.Domain.Enums;
using ApartmentResidenceManagement.Domain.Interfaces;
using Moq;
using Xunit;

namespace ApartmentResidenceManagement.Tests.Services;

public class MemberRequestServiceTests
{
    private readonly Mock<IUnitOfWork> _mockUow;
    private readonly Mock<IApartmentRepository> _mockApartmentRepo;
    private readonly Mock<IResidentRepository> _mockResidentRepo;
    private readonly Mock<IResidenceHistoryRepository> _mockResidenceHistoryRepo;
    private readonly Mock<IMemberRegistrationRequestRepository> _mockRequestRepo;
    private readonly ResidenceService _residenceService;
    private readonly MemberRequestService _service;

    public MemberRequestServiceTests()
    {
        _mockUow = new Mock<IUnitOfWork>();
        _mockApartmentRepo = new Mock<IApartmentRepository>();
        _mockResidentRepo = new Mock<IResidentRepository>();
        _mockResidenceHistoryRepo = new Mock<IResidenceHistoryRepository>();
        _mockRequestRepo = new Mock<IMemberRegistrationRequestRepository>();

        _mockUow.Setup(u => u.Apartments).Returns(_mockApartmentRepo.Object);
        _mockUow.Setup(u => u.Residents).Returns(_mockResidentRepo.Object);
        _mockUow.Setup(u => u.ResidenceHistories).Returns(_mockResidenceHistoryRepo.Object);
        _mockUow.Setup(u => u.MemberRegistrationRequests).Returns(_mockRequestRepo.Object);

        _residenceService = new ResidenceService(_mockUow.Object);
        _service = new MemberRequestService(_mockUow.Object, _residenceService);
    }

    [Fact]
    public async Task CreateRequestAsync_WhenValid_ReturnsSuccessAndPendingStatus()
    {
        // Arrange
        int aptId = 101;
        int requesterId = 1;
        var apartment = new Apartment { Id = aptId, ApartmentNumber = "101", Status = ApartmentStatus.Occupied };
        var requester = new Resident { Id = requesterId, FullName = "Nguyễn Văn A" };
        var activeRes = new ResidenceHistory { ApartmentId = aptId, ResidentId = requesterId, IsActive = true };

        _mockApartmentRepo.Setup(r => r.GetByIdAsync(aptId)).ReturnsAsync(apartment);
        _mockResidentRepo.Setup(r => r.GetByIdAsync(requesterId)).ReturnsAsync(requester);
        _mockResidenceHistoryRepo.Setup(r => r.GetActiveByApartmentIdAsync(aptId))
            .ReturnsAsync(new List<ResidenceHistory> { activeRes });
        _mockRequestRepo.Setup(r => r.GetByApartmentIdAsync(aptId))
            .ReturnsAsync(new List<MemberRegistrationRequest>());

        // Act
        var result = await _service.CreateRequestAsync(
            aptId, requesterId, "Nguyễn Văn Con", new DateTime(2010, 5, 20),
            "079205001122", "0901234567", "con@gmail.com", RelationshipType.FamilyMember);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(RequestStatus.Pending, result.Value.Status);
        Assert.Equal("Nguyễn Văn Con", result.Value.FullName);
        _mockRequestRepo.Verify(r => r.AddAsync(It.IsAny<MemberRegistrationRequest>()), Times.Once);
        _mockUow.Verify(u => u.CompleteAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateRequestAsync_WhenFullNameEmpty_ReturnsFailure()
    {
        // Act
        var result = await _service.CreateRequestAsync(
            1, 1, "   ", DateTime.Today.AddYears(-10), null, null, null, RelationshipType.FamilyMember);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Họ và tên", result.ErrorMessage);
    }

    [Fact]
    public async Task CreateRequestAsync_WhenRequesterNotLivingInApartment_ReturnsFailure()
    {
        // Arrange
        int aptId = 101;
        int requesterId = 2;
        var apartment = new Apartment { Id = aptId, ApartmentNumber = "101", Status = ApartmentStatus.Occupied };
        var requester = new Resident { Id = requesterId, FullName = "Người Lạ" };

        _mockApartmentRepo.Setup(r => r.GetByIdAsync(aptId)).ReturnsAsync(apartment);
        _mockResidentRepo.Setup(r => r.GetByIdAsync(requesterId)).ReturnsAsync(requester);
        _mockResidenceHistoryRepo.Setup(r => r.GetActiveByApartmentIdAsync(aptId))
            .ReturnsAsync(new List<ResidenceHistory>()); // Empty -> not living here

        // Act
        var result = await _service.CreateRequestAsync(
            aptId, requesterId, "Nguyễn Văn B", new DateTime(2000, 1, 1),
            null, null, null, RelationshipType.FamilyMember);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Bạn không phải là cư dân", result.ErrorMessage);
    }

    [Fact]
    public async Task ApproveRequestAsync_WhenPending_CreatesResidentAndRegistersResidence()
    {
        // Arrange
        int reqId = 5;
        int aptId = 101;
        var req = new MemberRegistrationRequest
        {
            Id = reqId,
            ApartmentId = aptId,
            RequesterResidentId = 1,
            FullName = "Trần Thị Bé",
            DateOfBirth = new DateTime(2005, 3, 15),
            IdentityCard = "079205001122",
            PhoneNumber = "0912345678",
            RelationshipType = RelationshipType.FamilyMember,
            Status = RequestStatus.Pending
        };

        var apartment = new Apartment { Id = aptId, ApartmentNumber = "101", Status = ApartmentStatus.Occupied };

        _mockRequestRepo.Setup(r => r.GetByIdAsync(reqId)).ReturnsAsync(req);
        _mockResidentRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Resident, bool>>>()))
            .ReturnsAsync(new List<Resident>()); // resident doesn't exist yet
        _mockResidentRepo.Setup(r => r.AddAsync(It.IsAny<Resident>()))
            .Callback<Resident>(r => r.Id = 10);
        _mockResidentRepo.Setup(r => r.GetByIdAsync(10))
            .ReturnsAsync(new Resident { Id = 10, FullName = "Trần Thị Bé", DateOfBirth = new DateTime(2005, 3, 15) });
        _mockApartmentRepo.Setup(r => r.GetByIdAsync(aptId)).ReturnsAsync(apartment);
        _mockResidenceHistoryRepo.Setup(r => r.GetActiveByResidentIdAsync(It.IsAny<int>()))
            .ReturnsAsync((ResidenceHistory?)null);
        _mockResidenceHistoryRepo.Setup(r => r.GetHistoryByResidentIdAsync(It.IsAny<int>()))
            .ReturnsAsync(new List<ResidenceHistory>());

        // Act
        var result = await _service.ApproveRequestAsync(reqId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(RequestStatus.Approved, req.Status);
        Assert.NotNull(req.ProcessedAt);
        _mockResidentRepo.Verify(r => r.AddAsync(It.IsAny<Resident>()), Times.Once);
        _mockResidenceHistoryRepo.Verify(r => r.AddAsync(It.IsAny<ResidenceHistory>()), Times.Once);
    }

    [Fact]
    public async Task RejectRequestAsync_WhenPending_SetsStatusRejectedAndReason()
    {
        // Arrange
        int reqId = 9;
        var req = new MemberRegistrationRequest
        {
            Id = reqId,
            ApartmentId = 101,
            FullName = "Nguyễn Văn T",
            Status = RequestStatus.Pending
        };

        _mockRequestRepo.Setup(r => r.GetByIdAsync(reqId)).ReturnsAsync(req);

        // Act
        var result = await _service.RejectRequestAsync(reqId, "Hồ sơ không khớp thông tin hợp đồng");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(RequestStatus.Rejected, req.Status);
        Assert.Equal("Hồ sơ không khớp thông tin hợp đồng", req.RejectReason);
        Assert.NotNull(req.ProcessedAt);
        _mockRequestRepo.Verify(r => r.Update(req), Times.Once);
        _mockUow.Verify(u => u.CompleteAsync(), Times.Once);
    }
}
