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

public class StatisticsServiceTests
{
    private readonly Mock<IUnitOfWork> _mockUow;
    private readonly Mock<IApartmentRepository> _mockApartmentRepo;
    private readonly Mock<IResidenceHistoryRepository> _mockResidenceHistoryRepo;
    private readonly Mock<IVehicleRepository> _mockVehicleRepo;
    private readonly StatisticsService _service;

    public StatisticsServiceTests()
    {
        _mockUow = new Mock<IUnitOfWork>();
        _mockApartmentRepo = new Mock<IApartmentRepository>();
        _mockResidenceHistoryRepo = new Mock<IResidenceHistoryRepository>();
        _mockVehicleRepo = new Mock<IVehicleRepository>();

        _mockUow.Setup(u => u.Apartments).Returns(_mockApartmentRepo.Object);
        _mockUow.Setup(u => u.ResidenceHistories).Returns(_mockResidenceHistoryRepo.Object);
        _mockUow.Setup(u => u.Vehicles).Returns(_mockVehicleRepo.Object);

        _service = new StatisticsService(_mockUow.Object);
    }

    [Fact]
    public async Task GetDashboardSummary_EmptyDatabase_ReturnsZerosWithoutException()
    {
        // 1. Không có căn hộ: tổng bằng 0, tỷ lệ bằng 0, không chia cho 0
        _mockApartmentRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Apartment>());
        _mockResidenceHistoryRepo.Setup(r => r.GetAllWithDetailsAsync()).ReturnsAsync(new List<ResidenceHistory>());
        _mockVehicleRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Vehicle>());

        var result = await _service.GetDashboardSummaryAsync();

        Assert.Equal(0, result.TotalApartments);
        Assert.Equal(0, result.OccupiedApartments);
        Assert.Equal(0, result.VacantApartments);
        Assert.Equal(0, result.CurrentResidents);
        Assert.Equal(0, result.OccupancyRate);
        Assert.Equal(0, result.PendingVehicles);
        Assert.Empty(result.RecentActivities);
    }

    [Fact]
    public async Task GetDashboardSummary_MultipleResidentsInOneApartment_CountsApartmentOnlyOnce()
    {
        // 2. Một căn có nhiều cư dân: căn hộ chỉ được tính là đang ở đúng 1 lần
        var apt = new Apartment { Id = 101, ApartmentNumber = "A-101", Floor = 1 };
        var apartments = new List<Apartment> { apt };

        var histories = new List<ResidenceHistory>
        {
            new() { Id = 1, ApartmentId = 101, ResidentId = 1, StartDate = DateTime.Today.AddMonths(-3), EndDate = null, IsActive = true },
            new() { Id = 2, ApartmentId = 101, ResidentId = 2, StartDate = DateTime.Today.AddMonths(-3), EndDate = null, IsActive = true },
            new() { Id = 3, ApartmentId = 101, ResidentId = 3, StartDate = DateTime.Today.AddMonths(-2), EndDate = null, IsActive = true }
        };

        _mockApartmentRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(apartments);
        _mockResidenceHistoryRepo.Setup(r => r.GetAllWithDetailsAsync()).ReturnsAsync(histories);
        _mockVehicleRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Vehicle>());

        var result = await _service.GetDashboardSummaryAsync();

        Assert.Equal(1, result.TotalApartments);
        Assert.Equal(1, result.OccupiedApartments);
        Assert.Equal(0, result.VacantApartments);
        Assert.Equal(3, result.CurrentResidents);
        Assert.Equal(100.0, result.OccupancyRate);
    }

    [Fact]
    public async Task GetDashboardSummary_ResidentWithMultipleHistories_CountsResidentOnlyOnce()
    {
        // 3. Một cư dân có nhiều bản ghi lịch sử: không đếm trùng cư dân hiện tại
        var apt1 = new Apartment { Id = 1, ApartmentNumber = "101", Floor = 1 };
        var apt2 = new Apartment { Id = 2, ApartmentNumber = "102", Floor = 1 };

        var histories = new List<ResidenceHistory>
        {
            // Quá khứ ở căn 101 (đã kết thúc)
            new() { Id = 1, ApartmentId = 1, ResidentId = 99, StartDate = DateTime.Today.AddYears(-1), EndDate = DateTime.Today.AddMonths(-1), IsActive = false },
            // Hiện tại ở căn 102 (đang hiệu lực)
            new() { Id = 2, ApartmentId = 2, ResidentId = 99, StartDate = DateTime.Today.AddMonths(-1), EndDate = null, IsActive = true }
        };

        _mockApartmentRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Apartment> { apt1, apt2 });
        _mockResidenceHistoryRepo.Setup(r => r.GetAllWithDetailsAsync()).ReturnsAsync(histories);
        _mockVehicleRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Vehicle>());

        var result = await _service.GetDashboardSummaryAsync();

        Assert.Equal(2, result.TotalApartments);
        Assert.Equal(1, result.OccupiedApartments);
        Assert.Equal(1, result.VacantApartments);
        Assert.Equal(1, result.CurrentResidents); // Cư dân ID 99 chỉ được tính 1 lần
    }

    [Fact]
    public async Task GetResidenceFluctuationReport_ApartmentTransfer_RecognizesTransferAndPreservesTotalResidents()
    {
        // 4. Chuyển căn hộ: không làm tăng/giảm tổng cư dân toàn tòa nếu cư dân vẫn ở trong tòa
        var apt1 = new Apartment { Id = 1, ApartmentNumber = "A-101", Floor = 1 };
        var apt2 = new Apartment { Id = 2, ApartmentNumber = "B-201", Floor = 2 };

        int testMonth = DateTime.Today.Month;
        int testYear = DateTime.Today.Year;
        var transferDate = new DateTime(testYear, testMonth, 5);

        var histories = new List<ResidenceHistory>
        {
            // Bản ghi cũ: kết thúc vào ngày chuyển
            new() { Id = 1, ApartmentId = 1, ResidentId = 50, StartDate = DateTime.Today.AddMonths(-6), EndDate = transferDate, IsActive = false },
            // Bản ghi mới: bắt đầu từ ngày chuyển
            new() { Id = 2, ApartmentId = 2, ResidentId = 50, StartDate = transferDate, EndDate = null, IsActive = true }
        };

        _mockApartmentRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Apartment> { apt1, apt2 });
        _mockResidenceHistoryRepo.Setup(r => r.GetAllWithDetailsAsync()).ReturnsAsync(histories);

        var report = await _service.GetResidenceFluctuationReportAsync(testMonth, testYear, null, null);

        Assert.Equal(1, report.ApartmentTransfers); // Nhận diện là 1 lượt chuyển căn hộ
        Assert.Equal(0, report.NewRegistrations);  // Không bị suy diễn thành đăng ký mới
        Assert.Equal(0, report.EndedResidences);    // Không bị suy diễn thành kết thúc cư trú khỏi tòa
        Assert.Equal(1, report.CurrentResidents);   // Cư dân vẫn đang ở trong tòa
    }

    [Fact]
    public async Task GetApartmentReport_FilterByFloor_ReturnsOnlyTargetFloorData()
    {
        // 6. Lọc theo tầng: chỉ thống kê dữ liệu thuộc tầng được chọn
        var apartments = new List<Apartment>
        {
            new() { Id = 1, ApartmentNumber = "901", Floor = 9 },
            new() { Id = 2, ApartmentNumber = "902", Floor = 9 },
            new() { Id = 3, ApartmentNumber = "1001", Floor = 10 }
        };

        var histories = new List<ResidenceHistory>
        {
            new() { Id = 1, ApartmentId = 1, ResidentId = 10, StartDate = DateTime.Today.AddMonths(-1), EndDate = null, IsActive = true }
        };

        _mockApartmentRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(apartments);
        _mockResidenceHistoryRepo.Setup(r => r.GetAllWithDetailsAsync()).ReturnsAsync(histories);

        var report = await _service.GetApartmentReportAsync(DateTime.Today.Year, 9, null);

        Assert.Equal(2, report.TotalApartments); // Chỉ có 2 căn ở tầng 9
        Assert.Equal(1, report.OccupiedApartments);
        Assert.Equal(1, report.VacantApartments);
        Assert.Equal(50.0, report.OccupancyRate);
        Assert.Single(report.FloorStatistics);
        Assert.Equal(9, report.FloorStatistics[0].Floor);
    }

    [Fact]
    public async Task GetVehicleReport_CountsApprovedPendingRejectedAccurately()
    {
        // BR05: Phương tiện có đúng 3 trạng thái Approved, Pending, Rejected không trùng lặp
        var vehicles = new List<Vehicle>
        {
            new() { Id = 1, LicensePlate = "29A-111.11", VehicleType = VehicleType.Car, RegistrationStatus = VehicleRegistrationStatus.Approved },
            new() { Id = 2, LicensePlate = "29A-222.22", VehicleType = VehicleType.Moto, RegistrationStatus = VehicleRegistrationStatus.Approved },
            new() { Id = 3, LicensePlate = "29A-333.33", VehicleType = VehicleType.Moto, RegistrationStatus = VehicleRegistrationStatus.Pending },
            new() { Id = 4, LicensePlate = "29A-444.44", VehicleType = VehicleType.Car, RegistrationStatus = VehicleRegistrationStatus.Rejected }
        };

        _mockVehicleRepo.Setup(r => r.GetAllWithDetailsAsync()).ReturnsAsync(vehicles);
        _mockResidenceHistoryRepo.Setup(r => r.GetAllWithDetailsAsync()).ReturnsAsync(new List<ResidenceHistory>());

        var report = await _service.GetVehicleReportAsync(null, null, null, null);

        Assert.Equal(4, report.TotalVehicles);
        Assert.Equal(2, report.ApprovedVehicles);
        Assert.Equal(1, report.PendingVehicles);
        Assert.Equal(1, report.RejectedVehicles);
        Assert.Equal(50.0, report.ApprovedRate);
        Assert.Equal(25.0, report.PendingRate);
        Assert.Equal(25.0, report.RejectedRate);
    }

    [Fact]
    public async Task GetDashboardSummary_WhenDatabaseThrows_PropagatesExceptionGracefully()
    {
        // 8. Mất kết nối Aiven: ngoại lệ được bắn ra để UI bắt và thông báo, không treo ứng dụng
        _mockApartmentRepo.Setup(r => r.GetAllAsync()).ThrowsAsync(new TimeoutException("Aiven connection timeout"));

        await Assert.ThrowsAsync<TimeoutException>(() => _service.GetDashboardSummaryAsync());
    }
}
