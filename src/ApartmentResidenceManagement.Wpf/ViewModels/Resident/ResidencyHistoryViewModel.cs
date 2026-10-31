using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using ApartmentResidenceManagement.Domain.Entities;
using ApartmentResidenceManagement.Wpf.Commands;

namespace ApartmentResidenceManagement.Wpf.ViewModels;

public class ResidencyHistoryEventItem
{
    public int STT { get; set; }
    public string EventTime { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string ApartmentInfo { get; set; } = "Căn hộ A-1203";
    public string Status { get; set; } = "Đã duyệt";
    public string StatusForeground { get; set; } = "#16A34A";
    public string StatusBackground { get; set; } = "#DCFCE7";
}

public class ResidencyHistoryViewModel : ViewModelBase
{
    private UserAccount? _account;
    private string _residentName = "Nguyễn Văn Minh";
    private string _residentCode = "CD-0001";
    private string _currentApartment = "A-1203";
    private bool _isLoading;
    private string _errorMessage = string.Empty;

    #region Properties
    public string ResidentName
    {
        get => _residentName;
        set => SetProperty(ref _residentName, value);
    }

    public string ResidentCode
    {
        get => _residentCode;
        set => SetProperty(ref _residentCode, value);
    }

    public string CurrentApartment
    {
        get => _currentApartment;
        set => SetProperty(ref _currentApartment, value);
    }

    public int TotalEvents => HistoryEvents.Count;

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public ObservableCollection<ResidencyHistoryEventItem> HistoryEvents { get; } = new();
    public bool HasHistory => HistoryEvents.Count > 0;
    #endregion

    #region Commands
    public ICommand RefreshCommand { get; }
    #endregion

    public ResidencyHistoryViewModel()
    {
        RefreshCommand = new AsyncRelayCommand(_ => LoadHistoryAsync());
        InitDefaultHistory();
    }

    public void Initialize(UserAccount account)
    {
        _account = account;
        _ = LoadHistoryAsync();
    }

    public Task LoadHistoryAsync()
    {
        IsLoading = true;
        try
        {
            if (_account?.Resident != null)
            {
                ResidentName = _account.Resident.FullName;
                ResidentCode = $"CD-{_account.Resident.Id:D4}";
            }
            else if (_account != null)
            {
                ResidentName = _account.Username;
                ResidentCode = $"CD-{_account.Id:D4}";
            }

            InitDefaultHistory();
            ErrorMessage = string.Empty;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Lỗi khi tải lịch sử cư trú: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }

        return Task.CompletedTask;
    }

    private void InitDefaultHistory()
    {
        HistoryEvents.Clear();
        HistoryEvents.Add(new ResidencyHistoryEventItem
        {
            STT = 1,
            EventTime = "10/01/2024",
            Content = "Đăng ký thường trú tại căn hộ A-1203",
            ApartmentInfo = "A-1203 (Tòa A, Tầng 12)",
            Status = "Đã duyệt",
            StatusForeground = "#16A34A",
            StatusBackground = "#DCFCE7"
        });
        HistoryEvents.Add(new ResidencyHistoryEventItem
        {
            STT = 2,
            EventTime = "05/06/2024",
            Content = "Bổ sung thành viên hộ khẩu: Nguyễn Minh Quân",
            ApartmentInfo = "A-1203 (Tòa A, Tầng 12)",
            Status = "Đã duyệt",
            StatusForeground = "#16A34A",
            StatusBackground = "#DCFCE7"
        });

        OnPropertyChanged(nameof(TotalEvents));
        OnPropertyChanged(nameof(HasHistory));
    }
}
