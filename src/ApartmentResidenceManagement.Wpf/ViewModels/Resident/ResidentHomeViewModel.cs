using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using ApartmentResidenceManagement.Domain.Entities;
using ApartmentResidenceManagement.Wpf.Commands;

namespace ApartmentResidenceManagement.Wpf.ViewModels;

public class AnnouncementItem
{
    public string Icon { get; set; } = "📢";
    public string Title { get; set; } = string.Empty;
    public string DateAndAuthor { get; set; } = string.Empty;
    public string Tag { get; set; } = "Thông báo";
    public string TagForeground { get; set; } = "#2563EB";
    public string TagBackground { get; set; } = "#EFF6FF";
}

public class ResidentHomeViewModel : ViewModelBase
{
    private UserAccount? _account;

    private string _residentName = "Nguyễn Văn Minh";
    private string _apartmentNumber = "A-1203";
    private string _buildingBlock = "A";
    private string _floorNumber = "12";
    private int _familyCount = 4;
    private int _vehicleCount = 2;
    private string _residencyStatus = "Đang cư trú";
    private string _errorMessage = string.Empty;
    private bool _isLoading;

    #region Properties
    public string ResidentName
    {
        get => _residentName;
        set => SetProperty(ref _residentName, value);
    }

    public string ApartmentNumber
    {
        get => _apartmentNumber;
        set => SetProperty(ref _apartmentNumber, value);
    }

    public string BuildingBlock
    {
        get => _buildingBlock;
        set => SetProperty(ref _buildingBlock, value);
    }

    public string FloorNumber
    {
        get => _floorNumber;
        set => SetProperty(ref _floorNumber, value);
    }

    public int FamilyCount
    {
        get => _familyCount;
        set => SetProperty(ref _familyCount, value);
    }

    public int VehicleCount
    {
        get => _vehicleCount;
        set => SetProperty(ref _vehicleCount, value);
    }

    public string ResidencyStatus
    {
        get => _residencyStatus;
        set => SetProperty(ref _residencyStatus, value);
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public ObservableCollection<AnnouncementItem> RecentAnnouncements { get; } = new();
    #endregion

    #region Commands
    public ICommand RefreshCommand { get; }
    public ICommand RegisterTemporaryStayCommand { get; private set; } = null!;
    public ICommand RequestServiceCommand { get; private set; } = null!;
    public ICommand SendFeedbackCommand { get; private set; } = null!;
    public ICommand LookupFeesCommand { get; private set; } = null!;
    public ICommand ViewAllAnnouncementsCommand { get; private set; } = null!;
    #endregion

    public ResidentHomeViewModel(
        Action? onRegisterTemporaryStay = null,
        Action? onRequestService = null,
        Action? onSendFeedback = null,
        Action? onLookupFees = null,
        Action? onViewAllAnnouncements = null)
    {
        RefreshCommand = new AsyncRelayCommand(_ => LoadStatsAsync());
        ConfigureNavigation(onRegisterTemporaryStay, onRequestService, onSendFeedback, onLookupFees, onViewAllAnnouncements);
        InitMockAnnouncements();
    }

    public void ConfigureNavigation(
        Action? onRegisterTemporaryStay,
        Action? onRequestService,
        Action? onSendFeedback,
        Action? onLookupFees,
        Action? onViewAllAnnouncements)
    {
        RegisterTemporaryStayCommand = new RelayCommand(_ => onRegisterTemporaryStay?.Invoke());
        RequestServiceCommand = new RelayCommand(_ => onRequestService?.Invoke());
        SendFeedbackCommand = new RelayCommand(_ => onSendFeedback?.Invoke());
        LookupFeesCommand = new RelayCommand(_ => onLookupFees?.Invoke());
        ViewAllAnnouncementsCommand = new RelayCommand(_ => onViewAllAnnouncements?.Invoke());
        OnPropertyChanged(nameof(RegisterTemporaryStayCommand));
        OnPropertyChanged(nameof(RequestServiceCommand));
        OnPropertyChanged(nameof(SendFeedbackCommand));
        OnPropertyChanged(nameof(LookupFeesCommand));
        OnPropertyChanged(nameof(ViewAllAnnouncementsCommand));
    }

    private void InitMockAnnouncements()
    {
        RecentAnnouncements.Clear();
        RecentAnnouncements.Add(new AnnouncementItem
        {
            Icon = "📢",
            Title = "Thông báo tạm ngừng cấp nước",
            DateAndAuthor = "25/01/2026 • Ban quản lý",
            Tag = "Quan trọng",
            TagForeground = "#DC2626",
            TagBackground = "#FEE2E2"
        });
        RecentAnnouncements.Add(new AnnouncementItem
        {
            Icon = "📅",
            Title = "Lịch vệ sinh thang máy tháng 02/2026",
            DateAndAuthor = "24/01/2026 • Ban quản lý",
            Tag = "Thông báo",
            TagForeground = "#2563EB",
            TagBackground = "#EFF6FF"
        });
        RecentAnnouncements.Add(new AnnouncementItem
        {
            Icon = "ℹ️",
            Title = "Hướng dẫn đăng ký tạm trú trực tuyến",
            DateAndAuthor = "20/01/2026 • Ban quản lý",
            Tag = "Thông tin",
            TagForeground = "#16A34A",
            TagBackground = "#DCFCE7"
        });
    }

    public void Initialize(UserAccount account)
    {
        _account = account;
        UpdateFromAccount();
    }

    private void UpdateFromAccount()
    {
        if (_account == null) return;

        if (_account.Resident != null)
        {
            ResidentName = _account.Resident.FullName;
            ResidencyStatus = "Đang cư trú";
            ApartmentNumber = "A-1203";
            BuildingBlock = "A";
            FloorNumber = "12";
            FamilyCount = 4;
        }
        else
        {
            ResidentName = _account.Username;
            ResidencyStatus = "Đang cư trú";
            ApartmentNumber = "A-1203";
            BuildingBlock = "A";
            FloorNumber = "12";
            FamilyCount = 4;
        }
    }

    public Task LoadStatsAsync()
    {
        IsLoading = true;
        try
        {
            UpdateFromAccount();
            InitMockAnnouncements();
            ErrorMessage = string.Empty;
        }
        catch (Exception)
        {
            ErrorMessage = "Không thể làm mới thông tin tổng quan.";
        }
        finally
        {
            IsLoading = false;
        }
        return Task.CompletedTask;
    }
}
