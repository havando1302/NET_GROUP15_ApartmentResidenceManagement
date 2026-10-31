using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using ApartmentResidenceManagement.Domain.Entities;
using ApartmentResidenceManagement.Wpf.Commands;

namespace ApartmentResidenceManagement.Wpf.ViewModels;

public class HouseholdMemberItem
{
    public int STT { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Relationship { get; set; } = string.Empty;
    public string BirthDate { get; set; } = string.Empty;
    public string IdentityCard { get; set; } = string.Empty;
    public string Status { get; set; } = "Đang cư trú";
    public string StatusForeground { get; set; } = "#16A34A";
    public string StatusBackground { get; set; } = "#DCFCE7";
}

public class ResidencyHistoryRecordItem
{
    public string EventTime { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Status { get; set; } = "Đã duyệt";
    public string StatusForeground { get; set; } = "#16A34A";
    public string StatusBackground { get; set; } = "#DCFCE7";
}

public class MyApartmentViewModel : ViewModelBase
{
    private UserAccount? _account;

    // Thông tin căn hộ & cư trú hiện tại
    private string _apartmentNumber = "A-1203";
    private string _buildingBlock = "A";
    private string _floorNumber = "12";
    private string _residencyType = "Thường trú";
    private string _registeredDate = "10/01/2024";
    private string _residencyStatus = "Đang cư trú";

    private bool _hasApartment = true;
    private bool _isLoading;
    private string _statusMessage = string.Empty;
    private string _errorMessage = string.Empty;

    #region Properties
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

    public string ResidencyType
    {
        get => _residencyType;
        set => SetProperty(ref _residencyType, value);
    }

    public string RegisteredDate
    {
        get => _registeredDate;
        set => SetProperty(ref _registeredDate, value);
    }

    public string ResidencyStatus
    {
        get => _residencyStatus;
        set => SetProperty(ref _residencyStatus, value);
    }

    public bool HasApartment
    {
        get => _hasApartment;
        set => SetProperty(ref _hasApartment, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public ObservableCollection<HouseholdMemberItem> HouseholdMembers { get; } = new();
    public ObservableCollection<ResidencyHistoryRecordItem> HistoryRecords { get; } = new();
    #endregion

    #region Commands
    public ICommand RefreshCommand { get; }
    public ICommand DownloadCertificateCommand { get; }
    public ICommand AddMemberCommand { get; }
    public ICommand EditMemberCommand { get; }
    #endregion

    public MyApartmentViewModel()
    {
        RefreshCommand = new AsyncRelayCommand(_ => LoadApartmentDataAsync());
        DownloadCertificateCommand = new RelayCommand(_ =>
        {
            StatusMessage = "Đang xuất giấy xác nhận cư trú (PDF) cho căn hộ A-1203...";
            ErrorMessage = string.Empty;
        });
        AddMemberCommand = new RelayCommand(_ =>
        {
            StatusMessage = "Hệ thống đang mở mẫu đăng ký thêm thành viên vào hộ...";
            ErrorMessage = string.Empty;
        });
        EditMemberCommand = new RelayCommand(param =>
        {
            if (param is HouseholdMemberItem member)
            {
                StatusMessage = $"Đang mở chỉnh sửa thông tin thành viên: {member.FullName}";
            }
            else
            {
                StatusMessage = "Đang mở chỉnh sửa thông tin thành viên...";
            }
            ErrorMessage = string.Empty;
        });

        InitMockData();
    }

    private void InitMockData()
    {
        HouseholdMembers.Clear();
        HouseholdMembers.Add(new HouseholdMemberItem
        {
            STT = 1,
            FullName = "Nguyễn Văn Minh",
            Relationship = "Chủ hộ",
            BirthDate = "15/04/2002",
            IdentityCard = "001202012345",
            Status = "Đang cư trú",
            StatusForeground = "#16A34A",
            StatusBackground = "#DCFCE7"
        });
        HouseholdMembers.Add(new HouseholdMemberItem
        {
            STT = 2,
            FullName = "Trần Thị Thu Hà",
            Relationship = "Vợ",
            BirthDate = "20/08/2003",
            IdentityCard = "001203045678",
            Status = "Đang cư trú",
            StatusForeground = "#16A34A",
            StatusBackground = "#DCFCE7"
        });
        HouseholdMembers.Add(new HouseholdMemberItem
        {
            STT = 3,
            FullName = "Nguyễn Minh Quân",
            Relationship = "Con",
            BirthDate = "10/03/2025",
            IdentityCard = "---",
            Status = "Đang cư trú",
            StatusForeground = "#16A34A",
            StatusBackground = "#DCFCE7"
        });

        HistoryRecords.Clear();
        HistoryRecords.Add(new ResidencyHistoryRecordItem
        {
            EventTime = "10/01/2024",
            Content = "Đăng ký thường trú tại căn hộ A-1203",
            Status = "Đã duyệt",
            StatusForeground = "#16A34A",
            StatusBackground = "#DCFCE7"
        });
        HistoryRecords.Add(new ResidencyHistoryRecordItem
        {
            EventTime = "05/06/2024",
            Content = "Bổ sung thành viên: Nguyễn Minh Quân",
            Status = "Đã duyệt",
            StatusForeground = "#16A34A",
            StatusBackground = "#DCFCE7"
        });
    }

    public void Initialize(UserAccount account)
    {
        _account = account;
        _ = LoadApartmentDataAsync();
    }

    public Task LoadApartmentDataAsync()
    {
        IsLoading = true;
        try
        {
            if (_account?.Resident != null)
            {
                ApartmentNumber = "A-1203";
                BuildingBlock = "A";
                FloorNumber = "12";
                ResidencyType = "Thường trú";
                RegisteredDate = "10/01/2024";
                ResidencyStatus = "Đang cư trú";
                HasApartment = true;
            }
            else
            {
                ApartmentNumber = "A-1203";
                BuildingBlock = "A";
                FloorNumber = "12";
                ResidencyType = "Thường trú";
                RegisteredDate = "10/01/2024";
                ResidencyStatus = "Đang cư trú";
                HasApartment = true;
            }

            InitMockData();
            ErrorMessage = string.Empty;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Lỗi khi tải thông tin hồ sơ cư trú: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }

        return Task.CompletedTask;
    }
}
