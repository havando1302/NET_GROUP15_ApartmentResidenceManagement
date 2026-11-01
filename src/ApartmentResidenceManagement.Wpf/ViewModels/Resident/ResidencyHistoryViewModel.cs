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

    private readonly Application.Services.ResidenceService? _residenceService;

    public ResidencyHistoryViewModel() : this(null)
    {
    }

    public ResidencyHistoryViewModel(Application.Services.ResidenceService? residenceService)
    {
        _residenceService = residenceService;
        RefreshCommand = new AsyncRelayCommand(_ => LoadHistoryAsync());
        SelectSessionCommand = new RelayCommand(param =>
        {
            if (param is HistorySessionItem item)
            {
                foreach (var s in Sessions) s.IsSelected = false;
                item.IsSelected = true;
                SelectedSession = item;
            }
        });
    }

    public void Initialize(UserAccount account)
    {
        _account = account;
        _ = LoadHistoryAsync();
    }

    public async Task LoadHistoryAsync()
    {
        IsLoading = true;
        ErrorMessage = string.Empty;

        try
        {
            Sessions.Clear();
            TimelineEvents.Clear();

            if (_residenceService != null && _account?.ResidentId.HasValue == true)
            {
                var historyList = (await _residenceService.GetHistoryByResidentIdAsync(_account.ResidentId.Value)).ToList();
                TotalResidencyCount = historyList.Count;
                DistinctApartmentsCount = historyList.Select(h => h.ApartmentId).Distinct().Count();

                int idx = 0;
                foreach (var h in historyList)
                {
                    var apt = h.Apartment;
                    string aptNum = apt != null ? apt.ApartmentNumber : $"Căn hộ #{h.ApartmentId}";
                    string bBlock = apt != null && apt.ApartmentNumber.Contains('-') ? apt.ApartmentNumber.Split('-')[0] : "A";
                    string fNum = apt != null ? apt.Floor.ToString() : "1";
                    string startStr = h.StartDate.ToString("dd/MM/yyyy");
                    string endStr = h.EndDate.HasValue ? h.EndDate.Value.ToString("dd/MM/yyyy") : "Chưa xác định";
                    string rangeStr = $"{startStr} – {endStr}";

                    int days = Math.Max(1, (int)((h.EndDate ?? DateTime.Today) - h.StartDate.Date).TotalDays);
                    string durStr = h.IsActive ? $"{days} ngày (tính đến hôm nay)" : $"{days} ngày";

                    if (h.IsActive)
                    {
                        CurrentDurationText = durStr;
                    }

                    var sessionItem = new HistorySessionItem
                    {
                        ApartmentNumber = aptNum,
                        BuildingBlock = bBlock,
                        FloorNumber = fNum,
                        DateRange = rangeStr,
                        StartDate = startStr,
                        EndDate = endStr,
                        Duration = durStr,
                        Status = h.IsActive ? "Đang cư trú" : "Đã kết thúc",
                        IsActive = h.IsActive,
                        IsSelected = idx == 0
                    };

                    Sessions.Add(sessionItem);
                    if (idx == 0) SelectedSession = sessionItem;

                    TimelineEvents.Add(new ResidencyTimelineEvent
                    {
                        EventDate = h.IsActive ? $"{startStr} – Nay" : rangeStr,
                        Title = h.IsActive ? "Đang cư trú (Hiện tại)" : $"Cư trú tại căn hộ {aptNum}",
                        Description = h.IsActive
                            ? $"Đăng ký cư trú tại căn hộ {aptNum} từ ngày {startStr}. Đã ở {days} ngày và đang tiếp tục cư trú (ngày kết thúc: Chưa xác định)."
                            : $"Đã hoàn tất cư trú tại căn hộ {aptNum} từ ngày {startStr} đến {endStr} (thời gian ở: {days} ngày).",
                        Status = h.IsActive ? "Đang cư trú" : "Đã kết thúc",
                        IsActive = h.IsActive,
                        HasNextLine = idx < historyList.Count - 1
                    });

                    idx++;
                }

                if (string.IsNullOrEmpty(CurrentDurationText))
                {
                    CurrentDurationText = "0 ngày";
                }
            }
            else
            {
                TotalResidencyCount = 0;
                DistinctApartmentsCount = 0;
                CurrentDurationText = "0 ngày";
            }

            OnPropertyChanged(nameof(HasHistory));
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Lỗi khi tải lịch sử cư trú: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
}
