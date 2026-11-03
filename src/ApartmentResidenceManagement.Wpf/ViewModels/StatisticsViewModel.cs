using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using ApartmentResidenceManagement.Application.DTOs;
using ApartmentResidenceManagement.Application.Services;
using ApartmentResidenceManagement.Domain.Enums;
using ApartmentResidenceManagement.Wpf.Commands;
using ApartmentResidenceManagement.Wpf.Controls;

namespace ApartmentResidenceManagement.Wpf.ViewModels;

public class StatisticsViewModel : ViewModelBase
{
    private readonly StatisticsService _statisticsService;
    private readonly ApartmentService _apartmentService;

    // Tab hiện tại: 0 = Biến động cư trú, 1 = Thống kê căn hộ, 2 = Thống kê phương tiện (RP01)
    private int _selectedTabIndex = 0;

    #region Bộ Lọc Chung (RP02, RP03)
    public List<string> TimeModeOptions { get; } = new() { "Theo tháng", "Theo năm" };
    public List<int> MonthOptions { get; } = Enumerable.Range(1, 12).ToList();
    public List<int> YearOptions { get; } = new() { 2024, 2025, 2026, 2027 };

    private string _selectedTimeMode = "Theo tháng";
    private int _selectedMonth = DateTime.Today.Month;
    private int _selectedYear = DateTime.Today.Year;

    // Lọc Tầng và Căn hộ
    private ObservableCollection<int?> _availableFloors = new();
    private int? _selectedFloor;

    private ObservableCollection<ApartmentDisplayItemDto> _availableApartments = new();
    private int? _selectedApartmentId;

    // Lọc trạng thái căn hộ (Tab 2)
    public List<string> ApartmentStatusFilterOptions { get; } = new() { "Tất cả", "Đang ở", "Trống" };
    private string _selectedApartmentStatusFilter = "Tất cả";

    // Lọc phương tiện (Tab 3)
    public List<string> VehicleTypeFilterOptions { get; } = new() { "Tất cả", "Xe máy", "Ô tô", "Xe đạp" };
    private string _selectedVehicleTypeFilter = "Tất cả";

    public List<string> VehicleStatusFilterOptions { get; } = new() { "Tất cả", "Đã duyệt", "Chờ duyệt", "Từ chối" };
    private string _selectedVehicleStatusFilter = "Tất cả";
    #endregion

    #region Phân Trang (Pagination)
    private int _currentPage = 1;
    private int _pageSize = 8;
    private int _totalFilteredCount = 0;
    public List<int> PageSizeOptions { get; } = new() { 5, 8, 10, 20 };
    private ObservableCollection<int> _pageNumbers = new();

    public int CurrentPage
    {
        get => _currentPage;
        set
        {
            if (SetProperty(ref _currentPage, value))
            {
                OnPropertyChanged(nameof(CanPreviousPage));
                OnPropertyChanged(nameof(CanNextPage));
                ApplyPagination();
            }
        }
    }

    public int PageSize
    {
        get => _pageSize;
        set
        {
            if (SetProperty(ref _pageSize, value))
            {
                CurrentPage = 1;
                ApplyPagination();
            }
        }
    }

    public int TotalFilteredCount
    {
        get => _totalFilteredCount;
        set
        {
            if (SetProperty(ref _totalFilteredCount, value))
            {
                OnPropertyChanged(nameof(TotalPages));
                OnPropertyChanged(nameof(CanPreviousPage));
                OnPropertyChanged(nameof(CanNextPage));
                OnPropertyChanged(nameof(PaginationSummary));
            }
        }
    }

    public int TotalPages => Math.Max(1, (int)Math.Ceiling((double)TotalFilteredCount / PageSize));
    public bool CanPreviousPage => CurrentPage > 1;
    public bool CanNextPage => CurrentPage < TotalPages;
    public ObservableCollection<int> PageNumbers { get => _pageNumbers; set => SetProperty(ref _pageNumbers, value); }

    public string PaginationSummary
    {
        get
        {
            if (TotalFilteredCount == 0) return "Hiển thị 0 / 0 bản ghi";
            int start = (CurrentPage - 1) * PageSize + 1;
            int end = Math.Min(CurrentPage * PageSize, TotalFilteredCount);
            return $"Hiển thị {start} – {end} của {TotalFilteredCount} bản ghi.";
        }
    }
    #endregion

    #region Tab 1: Biến động cư trú (Mockup 2)
    private int _residenceNewRegs;
    private double? _residenceNewRegsChangePercent;
    private int _residenceEnded;
    private double? _residenceEndedChangePercent;
    private int _residenceTransfers;
    private int _residenceCurrent;

    private int _permanentResidents;
    private double _permanentRate;
    private int _temporaryResidents;
    private double _temporaryRate;

    private ObservableCollection<BarCategoryGroup> _residenceFluctuationBars = new();
    private ObservableCollection<DonutSliceItem> _residenceStructureSlices = new();
    private ObservableCollection<MonthlyResidenceTrendDto> _residenceTrendTable = new();
    private ObservableCollection<MonthlyResidenceTrendDto> _pagedResidenceTrendTable = new();
    #endregion

    #region Tab 2: Thống kê căn hộ (Mockup 3)
    private int _aptTotal;
    private int _aptOccupied;
    private int _aptVacant;
    private double _aptOccupancyRate;

    private ObservableCollection<BarCategoryGroup> _aptFloorDistributionBars = new();
    private ObservableCollection<DonutSliceItem> _aptOccupancySlices = new();
    private ObservableCollection<FloorApartmentStatDto> _aptFloorTable = new();
    private ObservableCollection<FloorApartmentStatDto> _pagedAptFloorTable = new();
    #endregion

    #region Tab 3: Thống kê phương tiện (Mockup 4)
    private int _vehTotal;
    private int _vehApproved;
    private int _vehPending;
    private int _vehRejected;

    private ObservableCollection<BarCategoryGroup> _vehTypeDistributionBars = new();
    private ObservableCollection<DonutSliceItem> _vehStatusSlices = new();
    private ObservableCollection<VehicleReportDetailDto> _vehDetailsTable = new();
    private ObservableCollection<VehicleReportDetailDto> _pagedVehDetailsTable = new();
    #endregion

    #region Trạng thái và Thông báo
    private bool _isLoading;
    private string _errorMessage = string.Empty;
    private bool _hasError;
    private string _successMessage = string.Empty;
    private bool _hasSuccess;
    #endregion

    #region Binding Properties
    public int SelectedTabIndex
    {
        get => _selectedTabIndex;
        set
        {
            if (SetProperty(ref _selectedTabIndex, value))
            {
                OnPropertyChanged(nameof(IsTab1Active));
                OnPropertyChanged(nameof(IsTab2Active));
                OnPropertyChanged(nameof(IsTab3Active));
                CurrentPage = 1;
                _ = LoadActiveTabDataAsync();
            }
        }
    }

    public bool IsTab1Active => SelectedTabIndex == 0;
    public bool IsTab2Active => SelectedTabIndex == 1;
    public bool IsTab3Active => SelectedTabIndex == 2;

    public string SelectedTimeMode
    {
        get => _selectedTimeMode;
        set => SetProperty(ref _selectedTimeMode, value);
    }

    public int SelectedMonth
    {
        get => _selectedMonth;
        set => SetProperty(ref _selectedMonth, value);
    }

    public int SelectedYear
    {
        get => _selectedYear;
        set => SetProperty(ref _selectedYear, value);
    }

    public ObservableCollection<int?> AvailableFloors
    {
        get => _availableFloors;
        set => SetProperty(ref _availableFloors, value);
    }

    public int? SelectedFloor
    {
        get => _selectedFloor;
        set
        {
            if (SetProperty(ref _selectedFloor, value))
            {
                UpdateApartmentOptionsForFloor();
            }
        }
    }

    public ObservableCollection<ApartmentDisplayItemDto> AvailableApartments
    {
        get => _availableApartments;
        set => SetProperty(ref _availableApartments, value);
    }

    public int? SelectedApartmentId
    {
        get => _selectedApartmentId;
        set => SetProperty(ref _selectedApartmentId, value);
    }

    public string SelectedApartmentStatusFilter
    {
        get => _selectedApartmentStatusFilter;
        set => SetProperty(ref _selectedApartmentStatusFilter, value);
    }

    public string SelectedVehicleTypeFilter
    {
        get => _selectedVehicleTypeFilter;
        set => SetProperty(ref _selectedVehicleTypeFilter, value);
    }

    public string SelectedVehicleStatusFilter
    {
        get => _selectedVehicleStatusFilter;
        set => SetProperty(ref _selectedVehicleStatusFilter, value);
    }

    // Tab 1 Bindings
    public int ResidenceNewRegs { get => _residenceNewRegs; set => SetProperty(ref _residenceNewRegs, value); }
    public double? ResidenceNewRegsChangePercent { get => _residenceNewRegsChangePercent; set => SetProperty(ref _residenceNewRegsChangePercent, value); }
    public string ResidenceNewRegsChangeDisplay => ResidenceNewRegsChangePercent.HasValue ? $"{(ResidenceNewRegsChangePercent >= 0 ? "↑ +" : "↓ ")}{ResidenceNewRegsChangePercent:N0}%" : "";

    public int ResidenceEnded { get => _residenceEnded; set => SetProperty(ref _residenceEnded, value); }
    public double? ResidenceEndedChangePercent { get => _residenceEndedChangePercent; set => SetProperty(ref _residenceEndedChangePercent, value); }
    public string ResidenceEndedChangeDisplay => ResidenceEndedChangePercent.HasValue ? $"{(ResidenceEndedChangePercent >= 0 ? "↑ +" : "↓ ")}{ResidenceEndedChangePercent:N0}%" : "";

    public int ResidenceTransfers { get => _residenceTransfers; set => SetProperty(ref _residenceTransfers, value); }
    public int ResidenceCurrent { get => _residenceCurrent; set => SetProperty(ref _residenceCurrent, value); }

    public int PermanentResidents { get => _permanentResidents; set => SetProperty(ref _permanentResidents, value); }
    public double PermanentRate { get => _permanentRate; set => SetProperty(ref _permanentRate, value); }
    public string PermanentRateDisplay => $"{PermanentRate:N1}%".Replace('.', ',');

    public int TemporaryResidents { get => _temporaryResidents; set => SetProperty(ref _temporaryResidents, value); }
    public double TemporaryRate { get => _temporaryRate; set => SetProperty(ref _temporaryRate, value); }
    public string TemporaryRateDisplay => $"{TemporaryRate:N1}%".Replace('.', ',');

    public ObservableCollection<BarCategoryGroup> ResidenceFluctuationBars { get => _residenceFluctuationBars; set => SetProperty(ref _residenceFluctuationBars, value); }
    public ObservableCollection<DonutSliceItem> ResidenceStructureSlices { get => _residenceStructureSlices; set => SetProperty(ref _residenceStructureSlices, value); }
    public ObservableCollection<MonthlyResidenceTrendDto> ResidenceTrendTable { get => _residenceTrendTable; set => SetProperty(ref _residenceTrendTable, value); }
    public ObservableCollection<MonthlyResidenceTrendDto> PagedResidenceTrendTable { get => _pagedResidenceTrendTable; set => SetProperty(ref _pagedResidenceTrendTable, value); }

    // Tab 2 Bindings
    public int AptTotal { get => _aptTotal; set => SetProperty(ref _aptTotal, value); }
    public int AptOccupied { get => _aptOccupied; set => SetProperty(ref _aptOccupied, value); }
    public int AptVacant { get => _aptVacant; set => SetProperty(ref _aptVacant, value); }
    public double AptOccupancyRate { get => _aptOccupancyRate; set => SetProperty(ref _aptOccupancyRate, value); }
    public string AptOccupancyRateDisplay => $"{AptOccupancyRate:N1}%".Replace('.', ',');
    public string AptVacantRateDisplay => $"{Math.Max(0, 100.0 - AptOccupancyRate):N1}%".Replace('.', ',');

    public ObservableCollection<BarCategoryGroup> AptFloorDistributionBars { get => _aptFloorDistributionBars; set => SetProperty(ref _aptFloorDistributionBars, value); }
    public ObservableCollection<DonutSliceItem> AptOccupancySlices { get => _aptOccupancySlices; set => SetProperty(ref _aptOccupancySlices, value); }
    public ObservableCollection<FloorApartmentStatDto> AptFloorTable { get => _aptFloorTable; set => SetProperty(ref _aptFloorTable, value); }
    public ObservableCollection<FloorApartmentStatDto> PagedAptFloorTable { get => _pagedAptFloorTable; set => SetProperty(ref _pagedAptFloorTable, value); }

    // Tab 3 Bindings
    public int VehTotal { get => _vehTotal; set => SetProperty(ref _vehTotal, value); }
    public int VehApproved { get => _vehApproved; set => SetProperty(ref _vehApproved, value); }
    public int VehPending { get => _vehPending; set => SetProperty(ref _vehPending, value); }
    public int VehRejected { get => _vehRejected; set => SetProperty(ref _vehRejected, value); }

    public string VehApprovedRateDisplay => VehTotal == 0 ? "0%" : $"{Math.Round(VehApproved * 100.0 / VehTotal, 1):N1}%".Replace('.', ',');
    public string VehPendingRateDisplay => VehTotal == 0 ? "0%" : $"{Math.Round(VehPending * 100.0 / VehTotal, 1):N1}%".Replace('.', ',');
    public string VehRejectedRateDisplay => VehTotal == 0 ? "0%" : $"{Math.Round(VehRejected * 100.0 / VehTotal, 1):N1}%".Replace('.', ',');

    public ObservableCollection<BarCategoryGroup> VehTypeDistributionBars { get => _vehTypeDistributionBars; set => SetProperty(ref _vehTypeDistributionBars, value); }
    public ObservableCollection<DonutSliceItem> VehStatusSlices { get => _vehStatusSlices; set => SetProperty(ref _vehStatusSlices, value); }
    public ObservableCollection<VehicleReportDetailDto> VehDetailsTable { get => _vehDetailsTable; set => SetProperty(ref _vehDetailsTable, value); }
    public ObservableCollection<VehicleReportDetailDto> PagedVehDetailsTable { get => _pagedVehDetailsTable; set => SetProperty(ref _pagedVehDetailsTable, value); }

    // Thông báo & Lỗi
    public bool IsLoading { get => _isLoading; set => SetProperty(ref _isLoading, value); }
    public string ErrorMessage
    {
        get => _errorMessage;
        set
        {
            if (SetProperty(ref _errorMessage, value))
                HasError = !string.IsNullOrEmpty(value);
        }
    }
    public bool HasError { get => _hasError; set => SetProperty(ref _hasError, value); }

    public string SuccessMessage
    {
        get => _successMessage;
        set
        {
            if (SetProperty(ref _successMessage, value))
                HasSuccess = !string.IsNullOrEmpty(value);
        }
    }
    public bool HasSuccess { get => _hasSuccess; set => SetProperty(ref _hasSuccess, value); }
    #endregion

    #region Commands
    public ICommand SwitchTabCommand { get; }
    public ICommand FilterCommand { get; }
    public ICommand ResetFilterCommand { get; }
    public ICommand ExportReportCommand { get; }

    public ICommand NextPageCommand { get; }
    public ICommand PreviousPageCommand { get; }
    public ICommand GoToPageCommand { get; }
    #endregion

    public StatisticsViewModel(StatisticsService statisticsService, ApartmentService apartmentService)
    {
        _statisticsService = statisticsService;
        _apartmentService = apartmentService;

        SwitchTabCommand = new RelayCommand(p =>
        {
            if (int.TryParse(p?.ToString(), out int tab))
            {
                SelectedTabIndex = tab;
            }
        });

        FilterCommand = new AsyncRelayCommand(_ =>
        {
            CurrentPage = 1;
            return LoadActiveTabDataAsync();
        });

        ResetFilterCommand = new AsyncRelayCommand(_ => ResetFiltersAsync());

        ExportReportCommand = new RelayCommand(_ => ExportCurrentReportToCsv());

        NextPageCommand = new RelayCommand(_ =>
        {
            if (CanNextPage) CurrentPage++;
        });

        PreviousPageCommand = new RelayCommand(_ =>
        {
            if (CanPreviousPage) CurrentPage--;
        });

        GoToPageCommand = new RelayCommand(p =>
        {
            if (p is int pageNum && pageNum >= 1 && pageNum <= TotalPages)
            {
                CurrentPage = pageNum;
            }
            else if (p is string s && int.TryParse(s, out int parsed) && parsed >= 1 && parsed <= TotalPages)
            {
                CurrentPage = parsed;
            }
        });
    }

    private async Task ResetFiltersAsync()
    {
        SelectedTimeMode = "Theo tháng";
        SelectedMonth = DateTime.Today.Month;
        SelectedYear = DateTime.Today.Year;
        SelectedFloor = null;
        SelectedApartmentId = null;
        SelectedApartmentStatusFilter = "Tất cả";
        SelectedVehicleTypeFilter = "Tất cả";
        SelectedVehicleStatusFilter = "Tất cả";
        CurrentPage = 1;

        await LoadActiveTabDataAsync();
    }

    public async Task LoadStatisticsAsync()
    {
        await LoadDropdownOptionsAsync();
        await LoadActiveTabDataAsync();
    }

    private async Task LoadDropdownOptionsAsync()
    {
        try
        {
            var apartments = (await _apartmentService.GetAllApartmentsAsync()).ToList();

            // Danh sách tầng
            var floors = apartments.Select(a => a.Floor).Distinct().OrderBy(f => f).ToList();
            AvailableFloors.Clear();
            AvailableFloors.Add(null); // Tất cả
            foreach (var f in floors)
            {
                AvailableFloors.Add(f);
            }

            // Danh sách căn hộ
            _allApartmentDtos = apartments
                .OrderBy(a => a.ApartmentNumber)
                .Select(a => new ApartmentDisplayItemDto { Id = a.Id, Floor = a.Floor, ApartmentNumber = a.ApartmentNumber })
                .ToList();

            UpdateApartmentOptionsForFloor();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Lỗi khởi tạo bộ lọc: {ex.Message}";
        }
    }

    private List<ApartmentDisplayItemDto> _allApartmentDtos = new();

    private void UpdateApartmentOptionsForFloor()
    {
        AvailableApartments.Clear();
        AvailableApartments.Add(new ApartmentDisplayItemDto { Id = null, ApartmentNumber = "Tất cả" });

        var query = _allApartmentDtos.AsEnumerable();
        if (SelectedFloor.HasValue)
        {
            query = query.Where(a => a.Floor == SelectedFloor.Value);
        }

        foreach (var apt in query)
        {
            AvailableApartments.Add(apt);
        }

        SelectedApartmentId = null;
    }

    public async Task LoadActiveTabDataAsync()
    {
        if (IsLoading) return;
        IsLoading = true;
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;

        try
        {
            switch (SelectedTabIndex)
            {
                case 0:
                    await LoadResidenceFluctuationDataAsync();
                    break;
                case 1:
                    await LoadApartmentStatisticsDataAsync();
                    break;
                case 2:
                    await LoadVehicleStatisticsDataAsync();
                    break;
            }
            ApplyPagination();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Không thể tải báo cáo từ MySQL Aiven: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ApplyPagination()
    {
        if (SelectedTabIndex == 0)
        {
            TotalFilteredCount = ResidenceTrendTable.Count;
            var items = ResidenceTrendTable.Skip((CurrentPage - 1) * PageSize).Take(PageSize).ToList();
            PagedResidenceTrendTable = new ObservableCollection<MonthlyResidenceTrendDto>(items);
        }
        else if (SelectedTabIndex == 1)
        {
            TotalFilteredCount = AptFloorTable.Count;
            var items = AptFloorTable.Skip((CurrentPage - 1) * PageSize).Take(PageSize).ToList();
            PagedAptFloorTable = new ObservableCollection<FloorApartmentStatDto>(items);
        }
        else
        {
            TotalFilteredCount = VehDetailsTable.Count;
            var items = VehDetailsTable.Skip((CurrentPage - 1) * PageSize).Take(PageSize).ToList();
            PagedVehDetailsTable = new ObservableCollection<VehicleReportDetailDto>(items);
        }

        // Tạo danh sách số trang
        PageNumbers.Clear();
        for (int i = 1; i <= TotalPages; i++)
        {
            PageNumbers.Add(i);
        }
        OnPropertyChanged(nameof(PaginationSummary));
    }

    private async Task LoadResidenceFluctuationDataAsync()
    {
        int? month = SelectedTimeMode == "Theo tháng" ? SelectedMonth : null;
        int? year = SelectedYear;

        var report = await _statisticsService.GetResidenceFluctuationReportAsync(
            month, year, SelectedFloor, SelectedApartmentId);

        // 1. Cập nhật thẻ KPI
        ResidenceNewRegs = report.NewRegistrations;
        ResidenceNewRegsChangePercent = report.NewRegistrationsChangePercent;
        ResidenceEnded = report.EndedResidences;
        ResidenceEndedChangePercent = report.EndedResidencesChangePercent;
        ResidenceTransfers = report.ApartmentTransfers;
        ResidenceCurrent = report.CurrentResidents;

        // 2. Cơ cấu cư trú
        PermanentResidents = report.PermanentResidents;
        PermanentRate = report.PermanentRate;
        TemporaryResidents = report.TemporaryResidents;
        TemporaryRate = report.TemporaryRate;

        // 3. Biểu đồ tròn đặc (Pie Chart) Cơ cấu cư trú
        var donutSlices = new ObservableCollection<DonutSliceItem>
        {
            new DonutSliceItem
            {
                Title = "Cư dân thường trú",
                Value = PermanentResidents,
                Brush = new SolidColorBrush(Color.FromRgb(37, 99, 235)) // Blue #2563EB
            },
            new DonutSliceItem
            {
                Title = "Cư dân tạm trú",
                Value = TemporaryResidents,
                Brush = new SolidColorBrush(Color.FromRgb(168, 85, 247)) // Purple #A855F7
            }
        };
        ResidenceStructureSlices = donutSlices;

        // 4. Biểu đồ cột 6 tháng
        var barGroups = new ObservableCollection<BarCategoryGroup>();
        var blueBrush = new LinearGradientBrush(Color.FromRgb(59, 130, 246), Color.FromRgb(96, 165, 250), new System.Windows.Point(0, 1), new System.Windows.Point(0, 0));
        var purpleBrush = new LinearGradientBrush(Color.FromRgb(168, 85, 247), Color.FromRgb(192, 132, 252), new System.Windows.Point(0, 1), new System.Windows.Point(0, 0));

        foreach (var t in report.MonthlyTrends)
        {
            barGroups.Add(new BarCategoryGroup
            {
                CategoryLabel = t.MonthYearLabel,
                Series = new List<BarSeriesItem>
                {
                    new BarSeriesItem { SeriesName = "Đăng ký mới", Value = t.NewRegistrations, Brush = blueBrush },
                    new BarSeriesItem { SeriesName = "Kết thúc cư trú", Value = t.EndedResidences, Brush = purpleBrush }
                }
            });
        }
        ResidenceFluctuationBars = barGroups;

        // 5. Bảng chi tiết biến động
        ResidenceTrendTable = new ObservableCollection<MonthlyResidenceTrendDto>(report.MonthlyTrends);
    }

    private async Task LoadApartmentStatisticsDataAsync()
    {
        int? year = SelectedYear;
        ApartmentStatus? statusFilter = SelectedApartmentStatusFilter switch
        {
            "Đang ở" => ApartmentStatus.Occupied,
            "Trống" => ApartmentStatus.Empty,
            _ => null
        };

        var report = await _statisticsService.GetApartmentReportAsync(year, SelectedFloor, statusFilter);

        // 1. Thẻ KPI
        AptTotal = report.TotalApartments;
        AptOccupied = report.OccupiedApartments;
        AptVacant = report.VacantApartments;
        AptOccupancyRate = report.OccupancyRate;

        // 2. Biểu đồ tròn đặc (Pie Chart) Tình trạng căn hộ
        var slices = new ObservableCollection<DonutSliceItem>
        {
            new DonutSliceItem
            {
                Title = "Đang ở",
                Value = AptOccupied,
                Brush = new SolidColorBrush(Color.FromRgb(37, 99, 235)) // Blue #2563EB
            },
            new DonutSliceItem
            {
                Title = "Trống",
                Value = AptVacant,
                Brush = new SolidColorBrush(Color.FromRgb(192, 132, 252)) // Light Purple #C084FC
            }
        };
        AptOccupancySlices = slices;

        // 3. Biểu đồ cột Phân bố căn hộ theo tầng
        var barGroups = new ObservableCollection<BarCategoryGroup>();
        var blueBrush = new SolidColorBrush(Color.FromRgb(59, 130, 246));
        var lightPurpleBrush = new SolidColorBrush(Color.FromRgb(192, 132, 252));

        foreach (var f in report.FloorStatistics)
        {
            barGroups.Add(new BarCategoryGroup
            {
                CategoryLabel = f.FloorLabel,
                Series = new List<BarSeriesItem>
                {
                    new BarSeriesItem { SeriesName = "Đang ở", Value = f.OccupiedApartments, Brush = blueBrush },
                    new BarSeriesItem { SeriesName = "Trống", Value = f.VacantApartments, Brush = lightPurpleBrush }
                }
            });
        }
        AptFloorDistributionBars = barGroups;

        // 4. Bảng chi tiết tình trạng căn hộ theo tầng
        AptFloorTable = new ObservableCollection<FloorApartmentStatDto>(report.FloorStatistics);
    }

    private async Task LoadVehicleStatisticsDataAsync()
    {
        int? month = SelectedTimeMode == "Theo tháng" ? SelectedMonth : null;
        int? year = SelectedYear;

        VehicleType? typeFilter = SelectedVehicleTypeFilter switch
        {
            "Xe máy" => VehicleType.Moto,
            "Ô tô" => VehicleType.Car,
            "Xe đạp" => VehicleType.Bicycle,
            _ => null
        };

        VehicleRegistrationStatus? statusFilter = SelectedVehicleStatusFilter switch
        {
            "Đã duyệt" => VehicleRegistrationStatus.Approved,
            "Chờ duyệt" => VehicleRegistrationStatus.Pending,
            "Từ chối" => VehicleRegistrationStatus.Rejected,
            _ => null
        };

        var report = await _statisticsService.GetVehicleReportAsync(month, year, typeFilter, statusFilter);

        // 1. Thẻ KPI
        VehTotal = report.TotalVehicles;
        VehApproved = report.ApprovedVehicles;
        VehPending = report.PendingVehicles;
        VehRejected = report.RejectedVehicles;

        // 2. Biểu đồ tròn đặc (Pie Chart) Tỷ lệ phương tiện theo trạng thái
        var slices = new ObservableCollection<DonutSliceItem>
        {
            new DonutSliceItem { Title = "Đã duyệt", Value = VehApproved, Brush = new SolidColorBrush(Color.FromRgb(37, 99, 235)) },
            new DonutSliceItem { Title = "Chờ duyệt", Value = VehPending, Brush = new SolidColorBrush(Color.FromRgb(250, 204, 21)) },
            new DonutSliceItem { Title = "Từ chối", Value = VehRejected, Brush = new SolidColorBrush(Color.FromRgb(248, 113, 113)) }
        };
        VehStatusSlices = slices;

        // 3. Biểu đồ cột Số lượng phương tiện theo loại
        var barGroups = new ObservableCollection<BarCategoryGroup>();
        var blueBrush = new SolidColorBrush(Color.FromRgb(59, 130, 246));
        var yellowBrush = new SolidColorBrush(Color.FromRgb(250, 204, 21));
        var redBrush = new SolidColorBrush(Color.FromRgb(248, 113, 113));

        // Nhóm Xe máy
        barGroups.Add(new BarCategoryGroup
        {
            CategoryLabel = "Xe máy",
            Series = new List<BarSeriesItem>
            {
                new BarSeriesItem { SeriesName = "Đã duyệt", Value = report.MotorbikeApproved, Brush = blueBrush },
                new BarSeriesItem { SeriesName = "Chờ duyệt", Value = report.MotorbikePending, Brush = yellowBrush },
                new BarSeriesItem { SeriesName = "Từ chối", Value = report.MotorbikeRejected, Brush = redBrush }
            }
        });

        // Nhóm Ô tô
        barGroups.Add(new BarCategoryGroup
        {
            CategoryLabel = "Ô tô",
            Series = new List<BarSeriesItem>
            {
                new BarSeriesItem { SeriesName = "Đã duyệt", Value = report.CarApproved, Brush = blueBrush },
                new BarSeriesItem { SeriesName = "Chờ duyệt", Value = report.CarPending, Brush = yellowBrush },
                new BarSeriesItem { SeriesName = "Từ chối", Value = report.CarRejected, Brush = redBrush }
            }
        });

        VehTypeDistributionBars = barGroups;

        // 4. Bảng chi tiết phương tiện
        VehDetailsTable = new ObservableCollection<VehicleReportDetailDto>(report.VehicleDetails);
    }

    /// <summary>
    /// Xuất báo cáo ra định dạng CSV (UTF-8 BOM) để Excel mở tiếng Việt không bị lỗi font (RP09)
    /// </summary>
    private void ExportCurrentReportToCsv()
    {
        try
        {
            var sb = new StringBuilder();
            string fileName = "";

            if (SelectedTabIndex == 0)
            {
                fileName = $"BaoCao_BienDongCuTru_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                sb.AppendLine("STT,Tháng/Năm,Đăng ký mới,Kết thúc cư trú,Chuyển căn hộ,Tổng biến động");
                int idx = 1;
                foreach (var row in ResidenceTrendTable)
                {
                    sb.AppendLine($"{idx++},{row.MonthYearLabel},{row.NewRegistrations},{row.EndedResidences},{row.ApartmentTransfers},{row.TotalFluctuations}");
                }
            }
            else if (SelectedTabIndex == 1)
            {
                fileName = $"BaoCao_ThongKeCanHo_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                sb.AppendLine("Tầng,Tổng căn hộ,Đang ở,Trống,Tỷ lệ sử dụng (%)");
                foreach (var row in AptFloorTable)
                {
                    sb.AppendLine($"{row.FloorLabel},{row.TotalApartments},{row.OccupiedApartments},{row.VacantApartments},{row.OccupancyRate:N1}%");
                }
                sb.AppendLine($"Tổng,{AptTotal},{AptOccupied},{AptVacant},{AptOccupancyRate:N1}%");
            }
            else
            {
                fileName = $"BaoCao_ThongKePhuongTien_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                sb.AppendLine("STT,Biển số,Loại phương tiện,Căn hộ,Chủ sở hữu,Trạng thái,Ngày đăng ký");
                foreach (var row in VehDetailsTable)
                {
                    sb.AppendLine($"{row.Index},{row.LicensePlate},{row.VehicleTypeLabel},{row.ApartmentNumber},{row.OwnerName},{row.StatusLabel},{row.RegistrationDateDisplay}");
                }
            }

            // Lưu file vào thư mục Desktop hoặc Downloads
            string targetFolder = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            if (!Directory.Exists(targetFolder)) targetFolder = AppDomain.CurrentDomain.BaseDirectory;
            string filePath = Path.Combine(targetFolder, fileName);

            // Ghi file với BOM để Excel hiển thị tiếng Việt hoàn hảo
            File.WriteAllText(filePath, sb.ToString(), new UTF8Encoding(true));

            SuccessMessage = $"Đã xuất báo cáo thành công ra màn hình Desktop: {fileName}";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Lỗi khi xuất file báo cáo: {ex.Message}";
        }
    }
}

public class ApartmentDisplayItemDto
{
    public int? Id { get; set; }
    public int Floor { get; set; }
    public string ApartmentNumber { get; set; } = string.Empty;

    public override string ToString() => ApartmentNumber;
}
