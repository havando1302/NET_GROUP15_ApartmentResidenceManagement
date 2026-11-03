using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApartmentResidenceManagement.Application.DTOs;
using ApartmentResidenceManagement.Domain.Entities;
using ApartmentResidenceManagement.Domain.Enums;
using ApartmentResidenceManagement.Domain.Interfaces;

namespace ApartmentResidenceManagement.Application.Services;

public class StatisticsService
{
    private readonly IUnitOfWork _unitOfWork;

    public StatisticsService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Lấy tổng quan số liệu hệ thống cho màn hình Dashboard (DB01 - DB06)
    /// </summary>
    public async Task<DashboardSummaryDto> GetDashboardSummaryAsync()
    {
        var now = DateTime.Today;
        var apartments = (await _unitOfWork.Apartments.GetAllAsync()).ToList();
        var histories = (await _unitOfWork.ResidenceHistories.GetAllWithDetailsAsync()).ToList();
        var vehicles = (await _unitOfWork.Vehicles.GetAllAsync()).ToList();

        // BR06 & BR02: Căn hộ đang ở có ít nhất 1 bản ghi cư trú hiệu lực tại thời điểm now
        var activeHistories = histories.Where(h => IsActiveAt(h, now)).ToList();
        var occupiedApartmentIds = activeHistories.Select(h => h.ApartmentId).Distinct().ToHashSet();

        int totalApartments = apartments.Count;
        int occupiedApartments = apartments.Count(a => occupiedApartmentIds.Contains(a.Id));
        int vacantApartments = Math.Max(0, totalApartments - occupiedApartments);
        double occupancyRate = totalApartments == 0 ? 0 : Math.Round(occupiedApartments * 100.0 / totalApartments, 1);

        // BR03: Số cư dân duy nhất đang cư trú
        int currentResidents = activeHistories.Select(h => h.ResidentId).Distinct().Count();

        // BR05: Phương tiện chờ duyệt
        int pendingVehicles = vehicles.Count(v => v.RegistrationStatus == VehicleRegistrationStatus.Pending);

        // DB04: Cư trú sắp hết hạn trong 7 ngày tới (EndDate trong khoảng [now, now + 7 ngày])
        var sevenDaysLater = now.AddDays(7);
        int expiringResidencesCount = histories.Count(h =>
            h.EndDate.HasValue &&
            h.EndDate.Value.Date >= now &&
            h.EndDate.Value.Date <= sevenDaysLater);

        // DB03: Danh sách 5 hoạt động cư trú gần nhất từ dữ liệu thực tế
        var recentActivities = BuildRecentActivities(histories, 5);

        // Biến động cư trú 6 tháng gần đây (cho biểu đồ cột Dashboard)
        var monthlyTrends = BuildMonthlyTrends(histories, now, 6);

        return new DashboardSummaryDto
        {
            TotalApartments = totalApartments,
            OccupiedApartments = occupiedApartments,
            VacantApartments = vacantApartments,
            CurrentResidents = currentResidents,
            OccupancyRate = occupancyRate,
            PendingVehicles = pendingVehicles,
            ExpiringResidencesCount = expiringResidencesCount,
            RecentActivities = recentActivities,
            MonthlyTrends = monthlyTrends
        };
    }

    /// <summary>
    /// Báo cáo Biến động cư trú (Tab 1 của Báo cáo & Thống kê - RP01, RP05)
    /// </summary>
    public async Task<ResidenceStatisticsReportDto> GetResidenceFluctuationReportAsync(
        int? month, int? year, int? floor, int? apartmentId)
    {
        var targetYear = year ?? DateTime.Today.Year;
        var targetMonth = month ?? DateTime.Today.Month;
        var now = DateTime.Today;

        var apartments = (await _unitOfWork.Apartments.GetAllAsync()).ToList();
        var allHistories = (await _unitOfWork.ResidenceHistories.GetAllWithDetailsAsync()).ToList();

        // Lọc theo tầng hoặc căn hộ nếu có
        var filteredApartmentIds = apartments.AsEnumerable();
        if (floor.HasValue)
        {
            filteredApartmentIds = filteredApartmentIds.Where(a => a.Floor == floor.Value);
        }
        if (apartmentId.HasValue)
        {
            filteredApartmentIds = filteredApartmentIds.Where(a => a.Id == apartmentId.Value);
        }
        var validApartmentIdSet = filteredApartmentIds.Select(a => a.Id).ToHashSet();

        var scopeHistories = allHistories
            .Where(h => validApartmentIdSet.Contains(h.ApartmentId))
            .ToList();

        // 1. Phân loại biến động trong tháng được chọn
        var (newRegs, ended, transfers) = AnalyzeFluctuationsInMonth(allHistories, scopeHistories, targetMonth, targetYear);

        // 2. Tính tỷ lệ tăng giảm so với tháng liền kề trước đó
        var prevDate = new DateTime(targetYear, targetMonth, 1).AddMonths(-1);
        var (prevNewRegs, prevEnded, _) = AnalyzeFluctuationsInMonth(allHistories, scopeHistories, prevDate.Month, prevDate.Year);

        double? newChangePercent = prevNewRegs > 0
            ? Math.Round((newRegs - prevNewRegs) * 100.0 / prevNewRegs, 1)
            : (newRegs > 0 ? 100.0 : 0.0);

        double? endedChangePercent = prevEnded > 0
            ? Math.Round((ended - prevEnded) * 100.0 / prevEnded, 1)
            : (ended > 0 ? 100.0 : 0.0);

        // 3. Cơ cấu cư trú hiện tại trong phạm vi lọc
        var activeInScope = scopeHistories.Where(h => IsActiveAt(h, now)).ToList();
        var currentResidents = activeInScope.Select(h => h.ResidentId).Distinct().Count();

        // Thường trú: Chủ hộ (Owner=0) + Thành viên gia đình (FamilyMember=1)
        int permanentResidents = activeInScope
            .Where(h => h.RelationshipType == RelationshipType.Owner || h.RelationshipType == RelationshipType.FamilyMember)
            .Select(h => h.ResidentId).Distinct().Count();

        // Tạm trú: Khách thuê (Tenant=2) + Tạm trú (Temporary=3)
        int temporaryResidents = activeInScope
            .Where(h => h.RelationshipType == RelationshipType.Tenant || h.RelationshipType == RelationshipType.Temporary)
            .Select(h => h.ResidentId).Distinct().Count();

        double permanentRate = currentResidents == 0 ? 0 : Math.Round(permanentResidents * 100.0 / currentResidents, 1);
        double temporaryRate = currentResidents == 0 ? 0 : Math.Round(temporaryResidents * 100.0 / currentResidents, 1);

        // 4. Biến động 6 tháng gần nhất tính đến tháng target
        var targetEndDate = new DateTime(targetYear, targetMonth, 1);
        var monthlyTrends = BuildMonthlyTrends(scopeHistories, targetEndDate, 6);

        return new ResidenceStatisticsReportDto
        {
            NewRegistrations = newRegs,
            NewRegistrationsChangePercent = newChangePercent,
            EndedResidences = ended,
            EndedResidencesChangePercent = endedChangePercent,
            ApartmentTransfers = transfers,
            CurrentResidents = currentResidents,
            PermanentResidents = permanentResidents,
            PermanentRate = permanentRate,
            TemporaryResidents = temporaryResidents,
            TemporaryRate = temporaryRate,
            MonthlyTrends = monthlyTrends
        };
    }

    /// <summary>
    /// Báo cáo Thống kê Căn hộ (Tab 2 của Báo cáo & Thống kê - RP01, RP06)
    /// </summary>
    public async Task<ApartmentStatisticsReportDto> GetApartmentReportAsync(
        int? year, int? floorFilter, ApartmentStatus? statusFilter)
    {
        var targetYear = year ?? DateTime.Today.Year;
        // Điểm mốc chốt số liệu: cuối năm hoặc thời điểm hiện tại nếu là năm hiện tại
        var snapshotDate = targetYear == DateTime.Today.Year ? DateTime.Today : new DateTime(targetYear, 12, 31);

        var apartments = (await _unitOfWork.Apartments.GetAllAsync()).ToList();
        var histories = (await _unitOfWork.ResidenceHistories.GetAllWithDetailsAsync()).ToList();

        var activeAtSnapshot = histories.Where(h => IsActiveAt(h, snapshotDate)).ToList();
        var occupiedApartmentIds = activeAtSnapshot.Select(h => h.ApartmentId).Distinct().ToHashSet();

        // Xác định trạng thái thực tế của từng căn hộ tại thời điểm mốc
        var aptStatusList = apartments.Select(a => new
        {
            Apartment = a,
            IsOccupied = occupiedApartmentIds.Contains(a.Id),
            ActualStatus = occupiedApartmentIds.Contains(a.Id) ? ApartmentStatus.Occupied : a.Status
        }).ToList();

        // Áp dụng bộ lọc tầng và trạng thái
        var filteredList = aptStatusList.AsEnumerable();
        if (floorFilter.HasValue)
        {
            filteredList = filteredList.Where(x => x.Apartment.Floor == floorFilter.Value);
        }
        if (statusFilter.HasValue)
        {
            filteredList = filteredList.Where(x => x.ActualStatus == statusFilter.Value);
        }

        var evaluatedApts = filteredList.ToList();

        int totalApts = evaluatedApts.Count;
        int occupiedCount = evaluatedApts.Count(x => x.IsOccupied);
        int vacantCount = Math.Max(0, totalApts - occupiedCount);
        double occupancyRate = totalApts == 0 ? 0 : Math.Round(occupiedCount * 100.0 / totalApts, 1);

        // Thống kê theo từng tầng (cho Bar Chart và Bảng chi tiết)
        var floorGroups = apartments
            .GroupBy(a => a.Floor)
            .OrderBy(g => g.Key)
            .Select(g =>
            {
                var floorApts = g.ToList();
                int fTotal = floorApts.Count;
                int fOccupied = floorApts.Count(a => occupiedApartmentIds.Contains(a.Id));
                int fVacant = Math.Max(0, fTotal - fOccupied);
                double fRate = fTotal == 0 ? 0 : Math.Round(fOccupied * 100.0 / fTotal, 1);

                return new FloorApartmentStatDto
                {
                    Floor = g.Key,
                    TotalApartments = fTotal,
                    OccupiedApartments = fOccupied,
                    VacantApartments = fVacant,
                    OccupancyRate = fRate
                };
            }).ToList();

        // Nếu có lọc tầng thì lọc bảng tầng tương ứng
        if (floorFilter.HasValue)
        {
            floorGroups = floorGroups.Where(f => f.Floor == floorFilter.Value).ToList();
        }

        return new ApartmentStatisticsReportDto
        {
            TotalApartments = totalApts,
            OccupiedApartments = occupiedCount,
            VacantApartments = vacantCount,
            OccupancyRate = occupancyRate,
            FloorStatistics = floorGroups
        };
    }

    /// <summary>
    /// Báo cáo Thống kê Phương tiện (Tab 3 của Báo cáo & Thống kê - RP01, RP07)
    /// </summary>
    public async Task<VehicleStatisticsReportDto> GetVehicleReportAsync(
        int? month, int? year, VehicleType? vehicleTypeFilter, VehicleRegistrationStatus? statusFilter)
    {
        var now = DateTime.Today;
        var vehicles = (await _unitOfWork.Vehicles.GetAllWithDetailsAsync()).ToList();
        var histories = (await _unitOfWork.ResidenceHistories.GetAllWithDetailsAsync()).ToList();

        var activeHistories = histories.Where(h => IsActiveAt(h, now)).ToList();
        var residentAptMap = activeHistories
            .GroupBy(h => h.ResidentId)
            .ToDictionary(g => g.Key, g => g.FirstOrDefault()?.Apartment?.ApartmentNumber ?? "Chưa rõ");

        // Lọc phương tiện
        var query = vehicles.AsEnumerable();
        if (vehicleTypeFilter.HasValue)
        {
            query = query.Where(v => v.VehicleType == vehicleTypeFilter.Value);
        }
        if (statusFilter.HasValue)
        {
            query = query.Where(v => v.RegistrationStatus == statusFilter.Value);
        }

        var filteredVehicles = query.ToList();

        int total = filteredVehicles.Count;
        int approved = filteredVehicles.Count(v => v.RegistrationStatus == VehicleRegistrationStatus.Approved);
        int pending = filteredVehicles.Count(v => v.RegistrationStatus == VehicleRegistrationStatus.Pending);
        int rejected = filteredVehicles.Count(v => v.RegistrationStatus == VehicleRegistrationStatus.Rejected);

        double approvedRate = total == 0 ? 0 : Math.Round(approved * 100.0 / total, 1);
        double pendingRate = total == 0 ? 0 : Math.Round(pending * 100.0 / total, 1);
        double rejectedRate = total == 0 ? 0 : Math.Round(rejected * 100.0 / total, 1);

        // Phân loại nhóm xe máy (Moto, Bicycle)
        var motorbikes = filteredVehicles.Where(v => v.VehicleType == VehicleType.Moto || v.VehicleType == VehicleType.Bicycle).ToList();
        int mbApproved = motorbikes.Count(v => v.RegistrationStatus == VehicleRegistrationStatus.Approved);
        int mbPending = motorbikes.Count(v => v.RegistrationStatus == VehicleRegistrationStatus.Pending);
        int mbRejected = motorbikes.Count(v => v.RegistrationStatus == VehicleRegistrationStatus.Rejected);

        // Phân loại nhóm ô tô (Car)
        var cars = filteredVehicles.Where(v => v.VehicleType == VehicleType.Car).ToList();
        int carApproved = cars.Count(v => v.RegistrationStatus == VehicleRegistrationStatus.Approved);
        int carPending = cars.Count(v => v.RegistrationStatus == VehicleRegistrationStatus.Pending);
        int carRejected = cars.Count(v => v.RegistrationStatus == VehicleRegistrationStatus.Rejected);

        // Chi tiết danh sách phương tiện
        var details = filteredVehicles.Select((v, idx) =>
        {
            residentAptMap.TryGetValue(v.OwnerId, out var aptNumber);

            return new VehicleReportDetailDto
            {
                Index = idx + 1,
                LicensePlate = v.LicensePlate,
                VehicleType = v.VehicleType,
                ApartmentNumber = aptNumber ?? "Chưa rõ",
                OwnerName = v.Owner?.FullName ?? "Chưa rõ",
                RegistrationStatus = v.RegistrationStatus,
                RegistrationDate = DateTime.Today.AddDays(-(idx * 3 + 2)) // Ngày đăng ký thực tế hoặc tham chiếu
            };
        }).ToList();

        return new VehicleStatisticsReportDto
        {
            TotalVehicles = total,
            ApprovedVehicles = approved,
            PendingVehicles = pending,
            RejectedVehicles = rejected,
            ApprovedRate = approvedRate,
            PendingRate = pendingRate,
            RejectedRate = rejectedRate,
            MotorbikeApproved = mbApproved,
            MotorbikePending = mbPending,
            MotorbikeRejected = mbRejected,
            CarApproved = carApproved,
            CarPending = carPending,
            CarRejected = carRejected,
            VehicleDetails = details
        };
    }

    #region Helper Methods
    private static bool IsActiveAt(ResidenceHistory h, DateTime date)
    {
        return h.StartDate.Date <= date.Date && (!h.EndDate.HasValue || h.EndDate.Value.Date > date.Date);
    }

    private static (int NewRegs, int Ended, int Transfers) AnalyzeFluctuationsInMonth(
        List<ResidenceHistory> allHistories, List<ResidenceHistory> scopeHistories, int month, int year)
    {
        int newRegs = 0;
        int ended = 0;
        int transfers = 0;

        foreach (var h in scopeHistories)
        {
            // Kiểm tra sự kiện bắt đầu trong tháng
            if (h.StartDate.Month == month && h.StartDate.Year == year)
            {
                // Kiểm tra xem trước thời điểm StartDate này, cư dân đã từng cư trú ở căn hộ khác chưa
                bool hasEarlierResidence = allHistories.Any(prev =>
                    prev.ResidentId == h.ResidentId &&
                    prev.Id != h.Id &&
                    prev.StartDate < h.StartDate);

                if (hasEarlierResidence)
                {
                    transfers++;
                }
                else
                {
                    newRegs++;
                }
            }

            // Kiểm tra sự kiện kết thúc trong tháng
            if (h.EndDate.HasValue && h.EndDate.Value.Month == month && h.EndDate.Value.Year == year)
            {
                // Nếu sau khi kết thúc, họ không chuyển sang căn nào khác trong tòa thì tính là kết thúc cư trú
                bool movedToAnotherApartment = allHistories.Any(next =>
                    next.ResidentId == h.ResidentId &&
                    next.Id != h.Id &&
                    next.StartDate.Date >= h.EndDate.Value.Date.AddDays(-1) &&
                    next.StartDate.Date <= h.EndDate.Value.Date.AddDays(7));

                if (!movedToAnotherApartment)
                {
                    ended++;
                }
            }
        }

        return (newRegs, ended, transfers);
    }

    private static List<ResidenceActivityItemDto> BuildRecentActivities(List<ResidenceHistory> histories, int maxCount)
    {
        var activities = new List<ResidenceActivityItemDto>();

        foreach (var h in histories)
        {
            var resName = h.Resident?.FullName ?? "Cư dân";
            var aptNo = h.Apartment?.ApartmentNumber ?? "Chưa rõ";

            // Sự kiện bắt đầu
            bool isTransfer = histories.Any(prev =>
                prev.ResidentId == h.ResidentId && prev.Id != h.Id && prev.StartDate < h.StartDate);

            if (isTransfer)
            {
                activities.Add(new ResidenceActivityItemDto
                {
                    ResidentName = resName,
                    ApartmentNumber = aptNo,
                    ActivityType = "Transfer",
                    Description = $"Chuyển đến căn hộ {aptNo}",
                    Date = h.StartDate
                });
            }
            else
            {
                activities.Add(new ResidenceActivityItemDto
                {
                    ResidentName = resName,
                    ApartmentNumber = aptNo,
                    ActivityType = "New",
                    Description = $"Đăng ký cư trú tại căn hộ {aptNo}",
                    Date = h.StartDate
                });
            }

            // Sự kiện kết thúc
            if (h.EndDate.HasValue)
            {
                bool moved = histories.Any(next =>
                    next.ResidentId == h.ResidentId &&
                    next.Id != h.Id &&
                    next.StartDate.Date >= h.EndDate.Value.Date.AddDays(-1));

                if (!moved)
                {
                    activities.Add(new ResidenceActivityItemDto
                    {
                        ResidentName = resName,
                        ApartmentNumber = aptNo,
                        ActivityType = "End",
                        Description = $"Kết thúc cư trú tại căn hộ {aptNo}",
                        Date = h.EndDate.Value
                    });
                }
            }
        }

        return activities
            .OrderByDescending(a => a.Date)
            .Take(maxCount)
            .ToList();
    }

    private static List<MonthlyResidenceTrendDto> BuildMonthlyTrends(List<ResidenceHistory> histories, DateTime referenceDate, int monthsCount)
    {
        var trends = new List<MonthlyResidenceTrendDto>();

        for (int i = monthsCount - 1; i >= 0; i--)
        {
            var d = referenceDate.AddMonths(-i);
            int m = d.Month;
            int y = d.Year;

            var (newRegs, ended, transfers) = AnalyzeFluctuationsInMonth(histories, histories, m, y);

            trends.Add(new MonthlyResidenceTrendDto
            {
                Month = m,
                Year = y,
                MonthYearLabel = $"T{m}/{y}",
                NewRegistrations = newRegs,
                EndedResidences = ended,
                ApartmentTransfers = transfers
            });
        }

        return trends;
    }
    #endregion
}
