using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using ApartmentResidenceManagement.Application.DTOs;
using ApartmentResidenceManagement.Application.Services;
using ApartmentResidenceManagement.Wpf.Commands;
using ApartmentResidenceManagement.Wpf.Controls;

namespace ApartmentResidenceManagement.Wpf.ViewModels;

public class DashboardViewModel : ViewModelBase
{
    private readonly StatisticsService _statisticsService;

    // 4 Thẻ KPI chính (DB01)
    private int _totalApartments;
    private int _occupiedApartments;
    private int _vacantApartments;
    private int _currentResidents;
    private double _occupancyRate;
    private int _pendingVehicles;
    private int _pendingMemberRequestsCount;

    // Biểu đồ tròn và cột 2D (DB02, UI01, UI02)
    private ObservableCollection<DonutSliceItem> _apartmentDonutSlices = new();
    private ObservableCollection<BarCategoryGroup> _residenceTrendBars = new();

    // Danh sách 5 hoạt động gần đây (DB03)
    private ObservableCollection<ResidenceActivityItemDto> _recentActivities = new();

    // Trạng thái và lỗi (KT06, KT07)
    private bool _isLoading;
    private string _errorMessage = string.Empty;
    private bool _hasError;

    // Callback điều hướng sang trang khác (DB05)
    public Action<string>? OnNavigateRequested { get; set; }

    #region Binding Properties
    public int TotalApartments
    {
        get => _totalApartments;
        set => SetProperty(ref _totalApartments, value);
    }

    public int OccupiedApartments
    {
        get => _occupiedApartments;
        set => SetProperty(ref _occupiedApartments, value);
    }

    public int VacantApartments
    {
        get => _vacantApartments;
        set => SetProperty(ref _vacantApartments, value);
    }

    public int CurrentResidents
    {
        get => _currentResidents;
        set => SetProperty(ref _currentResidents, value);
    }

    public double OccupancyRate
    {
        get => _occupancyRate;
        set
        {
            if (SetProperty(ref _occupancyRate, value))
            {
                OnPropertyChanged(nameof(OccupancyRateDisplay));
                OnPropertyChanged(nameof(VacantRateDisplay));
            }
        }
    }

    public string OccupancyRateDisplay => $"{OccupancyRate:N1}%".Replace('.', ',');
    public string VacantRateDisplay => $"{Math.Max(0, 100.0 - OccupancyRate):N1}%".Replace('.', ',');

    public int PendingVehicles
    {
        get => _pendingVehicles;
        set => SetProperty(ref _pendingVehicles, value);
    }

    public int PendingMemberRequestsCount
    {
        get => _pendingMemberRequestsCount;
        set => SetProperty(ref _pendingMemberRequestsCount, value);
    }

    public ObservableCollection<DonutSliceItem> ApartmentDonutSlices
    {
        get => _apartmentDonutSlices;
        set => SetProperty(ref _apartmentDonutSlices, value);
    }

    public ObservableCollection<BarCategoryGroup> ResidenceTrendBars
    {
        get => _residenceTrendBars;
        set => SetProperty(ref _residenceTrendBars, value);
    }

    public ObservableCollection<ResidenceActivityItemDto> RecentActivities
    {
        get => _recentActivities;
        set => SetProperty(ref _recentActivities, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set
        {
            if (SetProperty(ref _errorMessage, value))
            {
                HasError = !string.IsNullOrEmpty(value);
            }
        }
    }

    public bool HasError
    {
        get => _hasError;
        set => SetProperty(ref _hasError, value);
    }
    #endregion

    #region Commands
    public ICommand RefreshCommand { get; }
    public ICommand NavigateCommand { get; }
    #endregion

    public DashboardViewModel(StatisticsService statisticsService)
    {
        _statisticsService = statisticsService;

        RefreshCommand = new AsyncRelayCommand(_ => LoadStatsAsync());
        NavigateCommand = new RelayCommand(param =>
        {
            if (param is string destination)
            {
                OnNavigateRequested?.Invoke(destination);
            }
        });
    }

    /// <summary>
    /// Tải dữ liệu thống kê thực tế từ MySQL Aiven (DB06, KT03, KT06)
    /// </summary>
    public async Task LoadStatsAsync()
    {
        if (IsLoading) return;

        IsLoading = true;
        ErrorMessage = string.Empty;

        try
        {
            var summary = await _statisticsService.GetDashboardSummaryAsync();

            // 1. Cập nhật thẻ KPI
            TotalApartments = summary.TotalApartments;
            OccupiedApartments = summary.OccupiedApartments;
            VacantApartments = summary.VacantApartments;
            CurrentResidents = summary.CurrentResidents;
            OccupancyRate = summary.OccupancyRate;
            PendingVehicles = summary.PendingVehicles;
            PendingMemberRequestsCount = summary.PendingMemberRequestsCount;

            // 2. Cập nhật Biểu đồ Donut Tình trạng sử dụng căn hộ (DB02, UI01, UI03)
            var slices = new ObservableCollection<DonutSliceItem>
            {
                new DonutSliceItem
                {
                    Title = "Đang ở",
                    Value = OccupiedApartments,
                    Brush = new LinearGradientBrush(
                        Color.FromRgb(37, 99, 235), // Blue #2563EB
                        Color.FromRgb(96, 165, 250), // Sky #60A5FA
                        new System.Windows.Point(0, 0),
                        new System.Windows.Point(1, 1))
                },
                new DonutSliceItem
                {
                    Title = "Trống",
                    Value = VacantApartments,
                    Brush = new SolidColorBrush(Color.FromRgb(203, 213, 225)) // Light Gray #CBD5E1
                }
            };
            ApartmentDonutSlices = slices;

            // 3. Cập nhật Biểu đồ Bar Cột 6 tháng (UI02, UI03)
            var barGroups = new ObservableCollection<BarCategoryGroup>();
            var blueBrush = new LinearGradientBrush(
                Color.FromRgb(59, 130, 246),
                Color.FromRgb(96, 165, 250),
                new System.Windows.Point(0, 1),
                new System.Windows.Point(0, 0));

            var purpleBrush = new LinearGradientBrush(
                Color.FromRgb(168, 85, 247),
                Color.FromRgb(192, 132, 252),
                new System.Windows.Point(0, 1),
                new System.Windows.Point(0, 0));

            foreach (var trend in summary.MonthlyTrends)
            {
                barGroups.Add(new BarCategoryGroup
                {
                    CategoryLabel = trend.MonthYearLabel,
                    Series = new List<BarSeriesItem>
                    {
                        new BarSeriesItem
                        {
                            SeriesName = "Đăng ký mới",
                            Value = trend.NewRegistrations,
                            Brush = blueBrush
                        },
                        new BarSeriesItem
                        {
                            SeriesName = "Kết thúc cư trú",
                            Value = trend.EndedResidences,
                            Brush = purpleBrush
                        }
                    }
                });
            }
            ResidenceTrendBars = barGroups;

            // 4. Danh sách 5 hoạt động gần đây (DB03)
            RecentActivities = new ObservableCollection<ResidenceActivityItemDto>(summary.RecentActivities);
        }
        catch (Exception ex)
        {
            // KT07: Thông báo lỗi kết nối Aiven
            ErrorMessage = $"Không thể tải số liệu từ máy chủ: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
}
