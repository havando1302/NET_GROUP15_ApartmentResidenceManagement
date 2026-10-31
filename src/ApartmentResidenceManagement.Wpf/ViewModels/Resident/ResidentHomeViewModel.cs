using System;
using System.Threading.Tasks;
using System.Windows.Input;
using ApartmentResidenceManagement.Domain.Entities;
using ApartmentResidenceManagement.Wpf.Commands;

namespace ApartmentResidenceManagement.Wpf.ViewModels;

public class ResidentHomeViewModel : ViewModelBase
{
    private UserAccount? _account;

    private string _residentName = "Nguyễn Văn Minh";
    private string _apartmentNumber = "A-1203";
    private string _buildingBlock = "A";
    private string _floorNumber = "12";
    private string _apartmentArea = "72 m²";
    private int _familyCount = 4;
    private int _residencyChangesCount = 2;
    private int _pendingRequestsCount = 0;
    private int _newAnnouncementsCount = 3;
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

    public string ApartmentArea
    {
        get => _apartmentArea;
        set => SetProperty(ref _apartmentArea, value);
    }

    public int FamilyCount
    {
        get => _familyCount;
        set => SetProperty(ref _familyCount, value);
    }

    public int ResidencyChangesCount
    {
        get => _residencyChangesCount;
        set => SetProperty(ref _residencyChangesCount, value);
    }

    public int PendingRequestsCount
    {
        get => _pendingRequestsCount;
        set => SetProperty(ref _pendingRequestsCount, value);
    }

    public int NewAnnouncementsCount
    {
        get => _newAnnouncementsCount;
        set => SetProperty(ref _newAnnouncementsCount, value);
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
    #endregion

    #region Navigation Commands
    public ICommand RefreshCommand { get; }
    public ICommand ViewApartmentDetailCommand { get; private set; } = null!;
    public ICommand ViewAllHistoryCommand { get; private set; } = null!;
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
        Action? onViewAllAnnouncements = null,
        Action? onViewApartmentDetail = null,
        Action? onViewAllHistory = null)
    {
        RefreshCommand = new AsyncRelayCommand(_ => LoadStatsAsync());
        ConfigureNavigation(
            onRegisterTemporaryStay, 
            onRequestService, 
            onSendFeedback, 
            onLookupFees, 
            onViewAllAnnouncements,
            onViewApartmentDetail,
            onViewAllHistory);
    }

    public void ConfigureNavigation(
        Action? onRegisterTemporaryStay,
        Action? onRequestService,
        Action? onSendFeedback,
        Action? onLookupFees,
        Action? onViewAllAnnouncements,
        Action? onViewApartmentDetail = null,
        Action? onViewAllHistory = null)
    {
        RegisterTemporaryStayCommand = new RelayCommand(_ => onRegisterTemporaryStay?.Invoke());
        RequestServiceCommand = new RelayCommand(_ => onRequestService?.Invoke());
        SendFeedbackCommand = new RelayCommand(_ => onSendFeedback?.Invoke());
        LookupFeesCommand = new RelayCommand(_ => onLookupFees?.Invoke());
        ViewAllAnnouncementsCommand = new RelayCommand(_ => onViewAllAnnouncements?.Invoke());
        ViewApartmentDetailCommand = new RelayCommand(_ => onViewApartmentDetail?.Invoke());
        ViewAllHistoryCommand = new RelayCommand(_ => onViewAllHistory?.Invoke());

        OnPropertyChanged(nameof(RegisterTemporaryStayCommand));
        OnPropertyChanged(nameof(RequestServiceCommand));
        OnPropertyChanged(nameof(SendFeedbackCommand));
        OnPropertyChanged(nameof(LookupFeesCommand));
        OnPropertyChanged(nameof(ViewAllAnnouncementsCommand));
        OnPropertyChanged(nameof(ViewApartmentDetailCommand));
        OnPropertyChanged(nameof(ViewAllHistoryCommand));
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
            ApartmentArea = "72 m²";
            FamilyCount = 4;
            ResidencyChangesCount = 2;
            PendingRequestsCount = 0;
            NewAnnouncementsCount = 3;
        }
        else
        {
            ResidentName = _account.Username == "admin" ? "Ban quản lý" : "Nguyễn Văn Minh";
            ResidencyStatus = "Đang cư trú";
            ApartmentNumber = "A-1203";
            BuildingBlock = "A";
            FloorNumber = "12";
            ApartmentArea = "72 m²";
            FamilyCount = 4;
            ResidencyChangesCount = 2;
            PendingRequestsCount = 0;
            NewAnnouncementsCount = 3;
        }
    }

    public Task LoadStatsAsync()
    {
        IsLoading = true;
        try
        {
            UpdateFromAccount();
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
