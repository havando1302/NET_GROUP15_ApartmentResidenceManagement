using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using ApartmentResidenceManagement.Domain.Entities;
using ApartmentResidenceManagement.Wpf.Commands;

namespace ApartmentResidenceManagement.Wpf.ViewModels;

public class HistorySessionItem : ViewModelBase
{
    private bool _isSelected;

    public string ApartmentNumber { get; set; } = string.Empty;
    public string BuildingBlock { get; set; } = "A";
    public string FloorNumber { get; set; } = "12";
    public string DateRange { get; set; } = string.Empty;
    public string StartDate { get; set; } = string.Empty;
    public string EndDate { get; set; } = "Chưa xác định";
    public string Duration { get; set; } = string.Empty;
    public string Status { get; set; } = "Đang cư trú";
    public bool IsActive { get; set; } = true;

    public string SessionSummaryText => IsActive
        ? $"Đang cư trú tại căn hộ {ApartmentNumber} (Kết thúc: Chưa xác định)"
        : $"Đã hoàn tất cư trú tại căn hộ {ApartmentNumber} ({DateRange})";

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
}

public class ResidencyTimelineEvent
{
    public string EventDate { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = "Đang cư trú";
    public bool IsActive { get; set; } = true;
    public bool HasNextLine { get; set; } = true;
}

public class ResidencyHistoryViewModel : ViewModelBase
{
    private UserAccount? _account;
    private HistorySessionItem? _selectedSession;
    private bool _isLoading;
    private string _errorMessage = string.Empty;

    // Summary stats
    private int _totalResidencyCount = 2;
    private int _distinctApartmentsCount = 1;
    private string _currentDurationText = "20 ngày";

    #region Properties
    public int TotalResidencyCount
    {
        get => _totalResidencyCount;
        set => SetProperty(ref _totalResidencyCount, value);
    }

    public int DistinctApartmentsCount
    {
        get => _distinctApartmentsCount;
        set => SetProperty(ref _distinctApartmentsCount, value);
    }

    public string CurrentDurationText
    {
        get => _currentDurationText;
        set => SetProperty(ref _currentDurationText, value);
    }

    public HistorySessionItem? SelectedSession
    {
        get => _selectedSession;
        set
        {
            if (SetProperty(ref _selectedSession, value))
            {
                foreach (var item in Sessions)
                {
                    item.IsSelected = (item == value);
                }
            }
        }
    }

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

    public ObservableCollection<HistorySessionItem> Sessions { get; } = new();
    public ObservableCollection<ResidencyTimelineEvent> TimelineEvents { get; } = new();
    public bool HasHistory => Sessions.Count > 0;
    #endregion

    #region Commands
    public ICommand RefreshCommand { get; }
    public ICommand SelectSessionCommand { get; }
    #endregion

    public ResidencyHistoryViewModel()
    {
        RefreshCommand = new AsyncRelayCommand(_ => LoadHistoryAsync());
        SelectSessionCommand = new RelayCommand(param =>
        {
            if (param is HistorySessionItem item)
            {
                SelectedSession = item;
            }
        });

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
        Sessions.Clear();

        var startActiveDate = new DateTime(2026, 1, 10);
        int totalDays = Math.Max(1, (int)(DateTime.Today - startActiveDate).TotalDays);
        string activeDuration = $"{totalDays} ngày (tính đến hôm nay)";
        CurrentDurationText = activeDuration;

        var session1 = new HistorySessionItem
        {
            ApartmentNumber = "A-1203",
            BuildingBlock = "A",
            FloorNumber = "12",
            DateRange = "10/01/2026 – Chưa xác định",
            StartDate = "10/01/2026",
            EndDate = "Chưa xác định",
            Duration = activeDuration,
            Status = "Đang cư trú",
            IsActive = true,
            IsSelected = true
        };

        var session2 = new HistorySessionItem
        {
            ApartmentNumber = "A-1008",
            BuildingBlock = "A",
            FloorNumber = "10",
            DateRange = "01/07/2024 – 09/01/2026",
            StartDate = "01/07/2024",
            EndDate = "09/01/2026",
            Duration = "1 năm 6 tháng (557 ngày)",
            Status = "Đã kết thúc",
            IsActive = false,
            IsSelected = false
        };

        Sessions.Add(session1);
        Sessions.Add(session2);
        SelectedSession = session1;

        TimelineEvents.Clear();
        TimelineEvents.Add(new ResidencyTimelineEvent
        {
            EventDate = "10/01/2026 – Nay",
            Title = "Đang cư trú (Hiện tại)",
            Description = $"Đăng ký cư trú tại căn hộ A-1203 từ ngày 10/01/2026. Đã ở {totalDays} ngày và đang tiếp tục cư trú (ngày kết thúc: Chưa xác định).",
            Status = "Đang cư trú",
            IsActive = true,
            HasNextLine = true
        });

        TimelineEvents.Add(new ResidencyTimelineEvent
        {
            EventDate = "09/01/2026",
            Title = "Kết thúc cư trú",
            Description = "Hoàn tất thủ tục chuyển căn hộ và kết thúc cư trú tại căn hộ A-1008 (thời gian ở: 1 năm 6 tháng).",
            Status = "Đã kết thúc",
            IsActive = false,
            HasNextLine = false
        });

        OnPropertyChanged(nameof(HasHistory));
    }
}
