using System;
using System.Collections.Generic;
using ApartmentResidenceManagement.Domain.Enums;

namespace ApartmentResidenceManagement.Application.DTOs;

/// <summary>
/// Dữ liệu tổng hợp cho màn hình Tổng quan hệ thống (Dashboard)
/// </summary>
public class DashboardSummaryDto
{
    public int TotalApartments { get; set; }
    public int OccupiedApartments { get; set; }
    public int VacantApartments { get; set; }
    public int CurrentResidents { get; set; }
    public double OccupancyRate { get; set; }

    public int PendingVehicles { get; set; }
    public int PendingMemberRequestsCount { get; set; }

    public List<ResidenceActivityItemDto> RecentActivities { get; set; } = new();
    public List<MonthlyResidenceTrendDto> MonthlyTrends { get; set; } = new();
}

/// <summary>
/// Mục hoạt động cư trú gần đây (Top 5 sự kiện thực tế)
/// </summary>
public class ResidenceActivityItemDto
{
    public string ResidentName { get; set; } = string.Empty;
    public string ApartmentNumber { get; set; } = string.Empty;
    /// <summary>
    /// New: Đăng ký mới (+), Transfer: Chuyển căn hộ (⇄), End: Kết thúc cư trú (-)
    /// </summary>
    public string ActivityType { get; set; } = "New"; 
    public string Description { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string DateDisplay => Date.ToString("dd/MM/yyyy");
}

/// <summary>
/// Biến động cư trú theo tháng (dùng cho Bar Chart và Bảng chi tiết)
/// </summary>
public class MonthlyResidenceTrendDto
{
    public int Month { get; set; }
    public int Year { get; set; }
    public string MonthYearLabel { get; set; } = string.Empty; // Ví dụ "T5/2026"
    public int NewRegistrations { get; set; }
    public int EndedResidences { get; set; }
    public int ApartmentTransfers { get; set; }
    public int TotalFluctuations => NewRegistrations + EndedResidences + ApartmentTransfers;
}

/// <summary>
/// Báo cáo Biến động cư trú (Tab 1 của Báo cáo & Thống kê)
/// </summary>
public class ResidenceStatisticsReportDto
{
    public int NewRegistrations { get; set; }
    public double? NewRegistrationsChangePercent { get; set; } // +80%

    public int EndedResidences { get; set; }
    public double? EndedResidencesChangePercent { get; set; } // -25%

    public int ApartmentTransfers { get; set; }
    public int CurrentResidents { get; set; }

    // Cơ cấu cư trú
    public int PermanentResidents { get; set; } // Chủ hộ + Thành viên
    public double PermanentRate { get; set; }
    public int TemporaryResidents { get; set; } // Khách thuê + Tạm trú
    public double TemporaryRate { get; set; }

    public List<MonthlyResidenceTrendDto> MonthlyTrends { get; set; } = new();
}

/// <summary>
/// Báo cáo Thống kê Căn hộ (Tab 2 của Báo cáo & Thống kê)
/// </summary>
public class ApartmentStatisticsReportDto
{
    public int TotalApartments { get; set; }
    public int OccupiedApartments { get; set; }
    public int VacantApartments { get; set; }
    public double OccupancyRate { get; set; }

    public List<FloorApartmentStatDto> FloorStatistics { get; set; } = new();
}

public class FloorApartmentStatDto
{
    public int Floor { get; set; }
    public string FloorLabel => $"Tầng {Floor}";
    public int TotalApartments { get; set; }
    public int OccupiedApartments { get; set; }
    public int VacantApartments { get; set; }
    public double OccupancyRate { get; set; }
}

/// <summary>
/// Báo cáo Thống kê Phương tiện (Tab 3 của Báo cáo & Thống kê)
/// </summary>
public class VehicleStatisticsReportDto
{
    public int TotalVehicles { get; set; }
    public int ApprovedVehicles { get; set; }
    public int PendingVehicles { get; set; }
    public int RejectedVehicles { get; set; }

    public double ApprovedRate { get; set; }
    public double PendingRate { get; set; }
    public double RejectedRate { get; set; }

    // Theo loại phương tiện (Xe máy, Ô tô)
    public int MotorbikeApproved { get; set; }
    public int MotorbikePending { get; set; }
    public int MotorbikeRejected { get; set; }

    public int CarApproved { get; set; }
    public int CarPending { get; set; }
    public int CarRejected { get; set; }

    public List<VehicleReportDetailDto> VehicleDetails { get; set; } = new();
}

public class VehicleReportDetailDto
{
    public int Index { get; set; }
    public string LicensePlate { get; set; } = string.Empty;
    public VehicleType VehicleType { get; set; }
    public string VehicleTypeLabel => VehicleType switch
    {
        VehicleType.Car => "Ô tô",
        VehicleType.Moto => "Xe máy",
        VehicleType.Bicycle => "Xe đạp",
        _ => "Khác"
    };
    public string ApartmentNumber { get; set; } = "Chưa rõ";
    public string OwnerName { get; set; } = "Chưa rõ";
    public VehicleRegistrationStatus RegistrationStatus { get; set; }
    public string StatusLabel => RegistrationStatus switch
    {
        VehicleRegistrationStatus.Approved => "Đã duyệt",
        VehicleRegistrationStatus.Pending => "Chờ duyệt",
        VehicleRegistrationStatus.Rejected => "Từ chối",
        VehicleRegistrationStatus.Cancelled => "Đã hủy",
        _ => "Chưa rõ"
    };
    public DateTime? RegistrationDate { get; set; }
    public string RegistrationDateDisplay => RegistrationDate.HasValue ? RegistrationDate.Value.ToString("dd/MM/yyyy") : "--/--/----";
}
