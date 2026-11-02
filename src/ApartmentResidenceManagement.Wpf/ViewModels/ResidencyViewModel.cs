using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using ApartmentResidenceManagement.Domain.Entities;
using ApartmentResidenceManagement.Domain.Enums;
using ApartmentResidenceManagement.Application.Services;
using ApartmentResidenceManagement.Wpf.Commands;

namespace ApartmentResidenceManagement.Wpf.ViewModels;

#region Display Item Models
public class ResidenceDisplayItemViewModel : ViewModelBase
{
    public ResidenceHistory History { get; }
    public int Stt { get; set; }
    public int Id => History.Id;
    public int ResidentId => History.ResidentId;
    public string ResidentCode => $"CD{History.ResidentId:D3}";
    public string ResidentName => History.Resident?.FullName ?? "Chưa rõ";
    public string ApartmentNumber => History.Apartment?.ApartmentNumber ?? "Chưa rõ";
    public int ApartmentId => History.ApartmentId;
    public DateTime StartDate => History.StartDate;
    public DateTime? EndDate => History.EndDate;
    public bool IsActive => History.IsActive;
    public RelationshipType RelationshipType => History.RelationshipType;

    public string RelationshipText => History.RelationshipType switch
    {
        RelationshipType.Owner => "Chủ hộ",
        RelationshipType.FamilyMember => "Thành viên gia đình",
        RelationshipType.Tenant => "Người thuê",
        RelationshipType.Temporary => "Tạm trú",
        _ => "Cư dân"
    };

    public string FormattedStartDate => History.StartDate.ToString("dd/MM/yyyy");
    public string FormattedEndDate => History.EndDate?.ToString("dd/MM/yyyy") ?? "Hiện tại";

    public string ResidenceDuration
    {
        get
        {
            var end = History.EndDate ?? DateTime.Today;
            var diff = (end.Date - History.StartDate.Date).Days;
            if (diff < 0) diff = 0;
            return $"{diff} ngày";
        }
    }

    public string IdentityCard => History.Resident?.IdentityCard ?? "Chưa cập nhật";
    public string PhoneNumber => History.Resident?.PhoneNumber ?? "Chưa cập nhật";
    public string HomeTown => History.Resident?.HomeTown ?? "Chưa cập nhật";
    public string Email => History.Resident?.Email ?? "Chưa cập nhật";

    public ResidenceDisplayItemViewModel(ResidenceHistory history, int stt = 1)
    {
        History = history;
        Stt = stt;
    }
}

public class ResidenceTimelineItem
{
    public string ApartmentNumber { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string StatusText => IsActive ? "Đang cư trú" : "Đã kết thúc";
    public string DateRangeText { get; set; } = string.Empty;
    public bool IsLast { get; set; }
    public bool ShowLine => !IsLast;
}

public class PaginationPageItem
{
    public int PageNumber { get; set; }
    public string DisplayText { get; set; } = string.Empty;
    public bool IsCurrent { get; set; }
    public bool IsEllipsis { get; set; }
}
#endregion

public class ResidencyViewModel : ViewModelBase
{
    private readonly ResidenceService _residenceService;
    private readonly ApartmentService _apartmentService;
    private readonly ResidentService _residentService;
    private readonly MemberRequestService? _memberRequestService;

    private List<ResidenceHistory> _allResidences = new();
    private List<MemberRegistrationRequestItemViewModel> _allRequests = new();

    // Tab: 0 = "Đang cư trú", 1 = "Lịch sử cư trú", 2 = "Yêu cầu thêm thành viên"
    private int _selectedTabIndex = 0;

    // Filters
    private string _searchText = string.Empty;
    private string _selectedApartmentFilter = "Tất cả";
    private string _selectedSortOrder = "Ngày bắt đầu (mới nhất)";

    // Pagination
    private int _currentPage = 1;
    private int _pageSize = 8;
    private int _totalFilteredCount = 0;

    // Collections
    private ObservableCollection<ResidenceDisplayItemViewModel> _pagedResidences = new();
    private ObservableCollection<string> _apartmentFilterOptions = new() { "Tất cả" };
    private ObservableCollection<int> _pageNumbers = new();
    private ObservableCollection<PaginationPageItem> _paginationPages = new();
    private ObservableCollection<MemberRegistrationRequestItemViewModel> _requests = new();
    private List<Apartment> _availableApartments = new();
    private List<Resident> _availableResidents = new();
    private List<ResidenceHistory> _activeResidencesForSelection = new();

    // Detail Panel (Mặc định luôn mở để thấy rõ bao quát thông tin)
    private bool _isDetailPanelOpen = true;
    private ResidenceDisplayItemViewModel? _selectedResidenceItem;
    private ObservableCollection<ResidenceTimelineItem> _selectedResidentTimeline = new();

    // Form modal state
    private bool _isFormOpen;
    private string _formTitle = string.Empty;
    private string _actionType = "Register"; // Register, Terminate, Transfer
    private ResidenceHistory? _selectedResidence;
    private Apartment? _selectedApartmentInput;
    private Resident? _selectedResidentInput;
    private RelationshipType _selectedRelationshipType = RelationshipType.FamilyMember;
    private DateTime _startDateInput = DateTime.Today;
    private DateTime _endDateInput = DateTime.Today;

    // Reject Dialog (for member requests)
    private bool _isRejectDialogOpen;
    private MemberRegistrationRequestItemViewModel? _requestToReject;
    private string _rejectReasonInput = string.Empty;
    private string _requestSuccessMessage = string.Empty;

    // Status / Loading
    private string _errorMessage = string.Empty;
    private bool _hasError;
    private bool _isLoading;

    #region Stats KPI Properties
    public int ActiveResidentsCount => _allResidences.Count(r => r.IsActive);
    public int EndedCount => _allResidences.Count(r => !r.IsActive);
    public int TransferCount
    {
        get
        {
            // Lượt chuyển căn hộ: tổng các hồ sơ đã kết thúc của những cư dân sau đó tiếp tục có căn hộ mới
            return _allResidences.GroupBy(r => r.ResidentId).Count(g => g.Count() > 1);
        }
    }
    #endregion

    #region Tab Properties
    public int SelectedTabIndex
    {
        get => _selectedTabIndex;
        set
        {
            if (SetProperty(ref _selectedTabIndex, value))
            {
                OnPropertyChanged(nameof(IsResidencesTab));
                OnPropertyChanged(nameof(IsActiveTab));
                OnPropertyChanged(nameof(IsHistoryTab));
                OnPropertyChanged(nameof(IsRequestsTab));
                OnPropertyChanged(nameof(IsDetailPanelVisible));
                CurrentPage = 1;
                ApplyFilters();
            }
        }
    }

    public bool IsResidencesTab => SelectedTabIndex == 0 || SelectedTabIndex == 1;
    public bool IsActiveTab
    {
        get => SelectedTabIndex == 0;
        set
        {
            if (value && SelectedTabIndex != 0) SelectedTabIndex = 0;
        }
    }
    public bool IsHistoryTab
    {
        get => SelectedTabIndex == 1;
        set
        {
            if (value && SelectedTabIndex != 1) SelectedTabIndex = 1;
        }
    }
    public bool IsRequestsTab
    {
        get => SelectedTabIndex == 2;
        set
        {
            if (value && SelectedTabIndex != 2) SelectedTabIndex = 2;
        }
    }
    #endregion

    #region Collection Properties
    public ObservableCollection<ResidenceDisplayItemViewModel> PagedResidences
    {
        get => _pagedResidences;
        set => SetProperty(ref _pagedResidences, value);
    }

    // Tương thích ngược cho các view cũ
    public ObservableCollection<ResidenceHistory> Residences { get; } = new();

    public ObservableCollection<MemberRegistrationRequestItemViewModel> Requests
    {
        get => _requests;
        set => SetProperty(ref _requests, value);
    }

    public ObservableCollection<string> ApartmentFilterOptions
    {
        get => _apartmentFilterOptions;
        set => SetProperty(ref _apartmentFilterOptions, value);
    }

    public List<string> SortOrderOptions { get; } = new()
    {
        "Ngày bắt đầu (mới nhất)",
        "Ngày bắt đầu (cũ nhất)",
        "Họ tên (A-Z)",
        "Căn hộ (A-Z)"
    };

    public List<int> PageSizeOptions { get; } = new() { 8, 10, 20, 50 };

    public ObservableCollection<int> PageNumbers
    {
        get => _pageNumbers;
        set => SetProperty(ref _pageNumbers, value);
    }

    public ObservableCollection<PaginationPageItem> PaginationPages
    {
        get => _paginationPages;
        set => SetProperty(ref _paginationPages, value);
    }

    public int PendingRequestsCount => _allRequests.Count(r => r.Status == RequestStatus.Pending);
    public bool HasPendingRequests => PendingRequestsCount > 0;
    public bool HasRequests => Requests.Count > 0;

    public List<Apartment> AvailableApartments
    {
        get => _availableApartments;
        set => SetProperty(ref _availableApartments, value);
    }

    public List<Resident> AvailableResidents
    {
        get => _availableResidents;
        set => SetProperty(ref _availableResidents, value);
    }

    public List<ResidenceHistory> ActiveResidencesForSelection
    {
        get => _activeResidencesForSelection;
        set => SetProperty(ref _activeResidencesForSelection, value);
    }
    #endregion

    #region Filter & Pagination Properties
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                CurrentPage = 1;
                ApplyFilters();
            }
        }
    }

    public string SelectedApartmentFilter
    {
        get => _selectedApartmentFilter;
        set
        {
            if (SetProperty(ref _selectedApartmentFilter, value))
            {
                CurrentPage = 1;
                ApplyFilters();
            }
        }
    }

    public string SelectedSortOrder
    {
        get => _selectedSortOrder;
        set
        {
            if (SetProperty(ref _selectedSortOrder, value))
            {
                CurrentPage = 1;
                ApplyFilters();
            }
        }
    }

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
                ApplyFilters();
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
                OnPropertyChanged(nameof(PaginationSummary));
                OnPropertyChanged(nameof(CanPreviousPage));
                OnPropertyChanged(nameof(CanNextPage));
            }
        }
    }

    public int TotalPages => Math.Max(1, (int)Math.Ceiling((double)TotalFilteredCount / PageSize));

    public bool CanPreviousPage => CurrentPage > 1;
    public bool CanNextPage => CurrentPage < TotalPages;

    public string PaginationSummary
    {
        get
        {
            if (TotalFilteredCount == 0) return "Hiển thị 0 / 0 bản ghi";
            int start = (CurrentPage - 1) * PageSize + 1;
            int end = Math.Min(CurrentPage * PageSize, TotalFilteredCount);
            return $"Hiển thị {end - start + 1} / {TotalFilteredCount} bản ghi";
        }
    }
    #endregion

    #region Detail Panel Properties
    public bool IsDetailPanelOpen
    {
        get => _isDetailPanelOpen;
        set
        {
            if (SetProperty(ref _isDetailPanelOpen, value))
            {
                OnPropertyChanged(nameof(IsDetailPanelVisible));
            }
        }
    }

    public bool IsDetailPanelVisible => IsDetailPanelOpen && IsResidencesTab;

    public ResidenceDisplayItemViewModel? SelectedResidenceItem
    {
        get => _selectedResidenceItem;
        set
        {
            if (SetProperty(ref _selectedResidenceItem, value))
            {
                OnPropertyChanged(nameof(HasSelectedResidence));
                OnPropertyChanged(nameof(HasNoSelectedResidence));
                LoadTimelineForSelectedResident();
            }
        }
    }

    public bool HasSelectedResidence => SelectedResidenceItem != null;
    public bool HasNoSelectedResidence => SelectedResidenceItem == null;

    public ObservableCollection<ResidenceTimelineItem> SelectedResidentTimeline
    {
        get => _selectedResidentTimeline;
        set => SetProperty(ref _selectedResidentTimeline, value);
    }
    #endregion

    #region Form Modal Properties
    public bool IsFormOpen
    {
        get => _isFormOpen;
        set => SetProperty(ref _isFormOpen, value);
    }

    public string FormTitle
    {
        get => _formTitle;
        set => SetProperty(ref _formTitle, value);
    }

    public string ActionType
    {
        get => _actionType;
        set
        {
            if (SetProperty(ref _actionType, value))
            {
                OnPropertyChanged(nameof(IsRegisterAction));
                OnPropertyChanged(nameof(IsTerminateAction));
                OnPropertyChanged(nameof(IsTransferAction));
            }
        }
    }

    public bool IsRegisterAction => ActionType == "Register";
    public bool IsTerminateAction => ActionType == "Terminate";
    public bool IsTransferAction => ActionType == "Transfer";

    public ResidenceHistory? SelectedResidence
    {
        get => _selectedResidence;
        set
        {
            if (SetProperty(ref _selectedResidence, value))
            {
                if (value != null)
                {
                    SelectedResidentInput = value.Resident;
                    if (IsTransferAction)
                    {
                        // Giữ nguyên quan hệ hiện tại
                        SelectedRelationshipType = value.RelationshipType;
                    }
                }
            }
        }
    }

    public Apartment? SelectedApartmentInput
    {
        get => _selectedApartmentInput;
        set => SetProperty(ref _selectedApartmentInput, value);
    }

    public Resident? SelectedResidentInput
    {
        get => _selectedResidentInput;
        set => SetProperty(ref _selectedResidentInput, value);
    }

    public RelationshipType SelectedRelationshipType
    {
        get => _selectedRelationshipType;
        set => SetProperty(ref _selectedRelationshipType, value);
    }

    public List<RelationshipType> RelationshipTypes { get; } =
        Enum.GetValues(typeof(RelationshipType)).Cast<RelationshipType>().ToList();

    public DateTime StartDateInput
    {
        get => _startDateInput;
        set => SetProperty(ref _startDateInput, value);
    }

    public DateTime EndDateInput
    {
        get => _endDateInput;
        set => SetProperty(ref _endDateInput, value);
    }

    // Reject Dialog Properties
    public bool IsRejectDialogOpen
    {
        get => _isRejectDialogOpen;
        set => SetProperty(ref _isRejectDialogOpen, value);
    }

    public string RejectReasonInput
    {
        get => _rejectReasonInput;
        set => SetProperty(ref _rejectReasonInput, value);
    }

    public string RequestSuccessMessage
    {
        get => _requestSuccessMessage;
        set => SetProperty(ref _requestSuccessMessage, value);
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

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }
    #endregion

    #region Commands
    public ICommand LoadResidencesCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand OpenRegisterFormCommand { get; }
    public ICommand OpenTerminateFormCommand { get; }
    public ICommand OpenTransferFormCommand { get; }
    public ICommand SaveResidencyCommand { get; }
    public ICommand CancelFormCommand { get; }
    public ICommand SelectResidenceCommand { get; }
    public ICommand CloseDetailPanelCommand { get; }

    public ICommand SwitchTabCommand { get; }
    public ICommand NextPageCommand { get; }
    public ICommand PreviousPageCommand { get; }
    public ICommand GoToPageCommand { get; }

    public ICommand ApproveRequestCommand { get; }
    public ICommand OpenRejectDialogCommand { get; }
    public ICommand ConfirmRejectRequestCommand { get; }
    public ICommand CloseRejectDialogCommand { get; }
    #endregion

    public ResidencyViewModel(
        ResidenceService residenceService,
        ApartmentService apartmentService,
        ResidentService residentService,
        MemberRequestService? memberRequestService = null)
    {
        _residenceService = residenceService;
        _apartmentService = apartmentService;
        _residentService = residentService;
        _memberRequestService = memberRequestService;

        LoadResidencesCommand = new AsyncRelayCommand(_ => LoadDataAsync());
        RefreshCommand = new AsyncRelayCommand(_ => LoadDataAsync());
        OpenRegisterFormCommand = new RelayCommand(_ => OpenRegisterForm());
        OpenTerminateFormCommand = new RelayCommand(r => OpenTerminateForm(r));
        OpenTransferFormCommand = new RelayCommand(r => OpenTransferForm(r));
        SaveResidencyCommand = new AsyncRelayCommand(_ => SaveResidencyAsync());
        CancelFormCommand = new RelayCommand(_ => CloseForm());

        SelectResidenceCommand = new RelayCommand(item =>
        {
            if (item is ResidenceDisplayItemViewModel displayItem)
            {
                SelectedResidenceItem = displayItem;
                IsDetailPanelOpen = true;
            }
        });

        CloseDetailPanelCommand = new RelayCommand(_ =>
        {
            IsDetailPanelOpen = false;
        });

        SwitchTabCommand = new RelayCommand(tab =>
        {
            if (tab is string s && int.TryParse(s, out int idx))
            {
                SelectedTabIndex = idx;
            }
            else if (tab is int i)
            {
                SelectedTabIndex = i;
            }
        });

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
            int pageNum = 0;
            if (p is PaginationPageItem pageItem)
            {
                if (!pageItem.IsEllipsis) pageNum = pageItem.PageNumber;
            }
            else if (p is int i) pageNum = i;
            else if (p is string s && int.TryParse(s, out int parsed)) pageNum = parsed;

            if (pageNum >= 1 && pageNum <= TotalPages)
            {
                CurrentPage = pageNum;
            }
        });

        ApproveRequestCommand = new AsyncRelayCommand(async p =>
        {
            if (p is MemberRegistrationRequestItemViewModel req && _memberRequestService != null)
            {
                await ApproveRequestAsync(req);
            }
        });

        OpenRejectDialogCommand = new RelayCommand(p =>
        {
            if (p is MemberRegistrationRequestItemViewModel req)
            {
                _requestToReject = req;
                RejectReasonInput = string.Empty;
                IsRejectDialogOpen = true;
            }
        });

        CloseRejectDialogCommand = new RelayCommand(_ =>
        {
            IsRejectDialogOpen = false;
            _requestToReject = null;
        });

        ConfirmRejectRequestCommand = new AsyncRelayCommand(async _ =>
        {
            if (_requestToReject != null && _memberRequestService != null)
            {
                await RejectRequestAsync(_requestToReject, RejectReasonInput);
            }
        });
    }

    #region Mouse Wheel & Keyboard Paged Navigation
    public void SelectNextItemOrNextPage()
    {
        if (PagedResidences.Count == 0) return;

        if (SelectedResidenceItem == null)
        {
            SelectedResidenceItem = PagedResidences.FirstOrDefault();
            return;
        }

        int currentIndex = PagedResidences.IndexOf(SelectedResidenceItem);
        if (currentIndex >= 0 && currentIndex < PagedResidences.Count - 1)
        {
            SelectedResidenceItem = PagedResidences[currentIndex + 1];
        }
        else if (currentIndex == PagedResidences.Count - 1)
        {
            if (CanNextPage)
            {
                CurrentPage++;
                SelectedResidenceItem = PagedResidences.FirstOrDefault();
            }
        }
    }

    public void SelectPreviousItemOrPreviousPage()
    {
        if (PagedResidences.Count == 0) return;

        if (SelectedResidenceItem == null)
        {
            SelectedResidenceItem = PagedResidences.LastOrDefault();
            return;
        }

        int currentIndex = PagedResidences.IndexOf(SelectedResidenceItem);
        if (currentIndex > 0)
        {
            SelectedResidenceItem = PagedResidences[currentIndex - 1];
        }
        else if (currentIndex == 0)
        {
            if (CanPreviousPage)
            {
                CurrentPage--;
                SelectedResidenceItem = PagedResidences.LastOrDefault();
            }
        }
    }
    #endregion

    public async Task LoadDataAsync()
    {
        ErrorMessage = string.Empty;
        IsLoading = true;
        try
        {
            var list = await _residenceService.GetAllResidencesAsync();
            _allResidences = list.ToList();

            var apartments = await _apartmentService.GetAllApartmentsAsync();
            AvailableApartments = apartments
                .Where(a => a.Status != ApartmentStatus.UnderMaintenance)
                .OrderBy(a => a.ApartmentNumber)
                .ToList();

            // Cập nhật ApartmentFilterOptions
            var distinctApartments = _allResidences
                .Select(r => r.Apartment?.ApartmentNumber)
                .Where(num => !string.IsNullOrEmpty(num))
                .Distinct()
                .OrderBy(num => num)
                .ToList();

            ApartmentFilterOptions.Clear();
            ApartmentFilterOptions.Add("Tất cả");
            foreach (var num in distinctApartments)
            {
                ApartmentFilterOptions.Add(num!);
            }

            var residents = await _residentService.GetAllResidentsAsync();
            var activeResidentIds = _allResidences
                .Where(rh => rh.IsActive)
                .Select(rh => rh.ResidentId)
                .ToHashSet();

            AvailableResidents = residents
                .Where(r => !activeResidentIds.Contains(r.Id))
                .OrderBy(r => r.FullName)
                .ToList();

            ActiveResidencesForSelection = _allResidences
                .Where(rh => rh.IsActive)
                .OrderBy(rh => rh.Resident?.FullName)
                .ToList();

            // Cập nhật stats
            OnPropertyChanged(nameof(ActiveResidentsCount));
            OnPropertyChanged(nameof(EndedCount));
            OnPropertyChanged(nameof(TransferCount));

            ApplyFilters();

            // Load requests
            await LoadRequestsAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Lỗi khi lấy dữ liệu cư trú hoặc danh mục: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private List<ResidenceHistory> _currentFilteredList = new();

    private void ApplyFilters()
    {
        var query = _allResidences.AsEnumerable();

        // Lọc theo Tab: 0 = Đang cư trú, 1 = Lịch sử cư trú
        if (SelectedTabIndex == 0)
        {
            query = query.Where(rh => rh.IsActive);
        }
        else if (SelectedTabIndex == 1)
        {
            query = query.Where(rh => !rh.IsActive);
        }

        // Lọc theo Căn hộ
        if (!string.IsNullOrWhiteSpace(SelectedApartmentFilter) && SelectedApartmentFilter != "Tất cả")
        {
            query = query.Where(rh => rh.Apartment?.ApartmentNumber == SelectedApartmentFilter);
        }

        // Tìm kiếm theo từ khóa
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            string keyword = SearchText.Trim().ToLower();
            query = query.Where(rh =>
                $"cd{rh.ResidentId:D3}".Contains(keyword) ||
                (rh.Resident != null && rh.Resident.FullName.ToLower().Contains(keyword)) ||
                (rh.Resident != null && rh.Resident.IdentityCard != null && rh.Resident.IdentityCard.ToLower().Contains(keyword)) ||
                (rh.Apartment != null && rh.Apartment.ApartmentNumber.ToLower().Contains(keyword)));
        }

        // Sắp xếp
        query = SelectedSortOrder switch
        {
            "Ngày bắt đầu (cũ nhất)" => query.OrderBy(rh => rh.StartDate),
            "Họ tên (A-Z)" => query.OrderBy(rh => rh.Resident?.FullName),
            "Căn hộ (A-Z)" => query.OrderBy(rh => rh.Apartment?.ApartmentNumber),
            _ => query.OrderByDescending(rh => rh.StartDate)
        };

        _currentFilteredList = query.ToList();
        TotalFilteredCount = _currentFilteredList.Count;

        // Cập nhật danh sách cũ để tương thích ngược
        Residences.Clear();
        foreach (var item in _currentFilteredList)
        {
            Residences.Add(item);
        }

        ApplyPagination();
    }

    private void ApplyPagination()
    {
        int total = _currentFilteredList.Count;
        int maxPages = Math.Max(1, (int)Math.Ceiling((double)total / PageSize));
        if (CurrentPage > maxPages) _currentPage = maxPages;
        if (CurrentPage < 1) _currentPage = 1;

        int skip = (CurrentPage - 1) * PageSize;
        var pagedItems = _currentFilteredList.Skip(skip).Take(PageSize).ToList();

        PagedResidences.Clear();
        int stt = skip + 1;
        foreach (var rh in pagedItems)
        {
            PagedResidences.Add(new ResidenceDisplayItemViewModel(rh, stt++));
        }

        // Nếu panel chi tiết đang mở mà mục hiện tại không còn trong trang, chọn mục đầu tiên
        if (IsDetailPanelOpen)
        {
            if (SelectedResidenceItem == null || !PagedResidences.Any(p => p.Id == SelectedResidenceItem.Id))
            {
                SelectedResidenceItem = PagedResidences.FirstOrDefault();
            }
        }

        // Cập nhật PageNumbers (số nguyên)
        PageNumbers.Clear();
        for (int i = 1; i <= maxPages; i++)
        {
            PageNumbers.Add(i);
        }

        // Cập nhật PaginationPages (dạng thông minh cho UI)
        PaginationPages.Clear();
        if (maxPages <= 7)
        {
            for (int i = 1; i <= maxPages; i++)
            {
                PaginationPages.Add(new PaginationPageItem
                {
                    PageNumber = i,
                    DisplayText = i.ToString(),
                    IsCurrent = (i == CurrentPage),
                    IsEllipsis = false
                });
            }
        }
        else
        {
            // Trang đầu
            PaginationPages.Add(new PaginationPageItem
            {
                PageNumber = 1,
                DisplayText = "1",
                IsCurrent = (CurrentPage == 1),
                IsEllipsis = false
            });

            if (CurrentPage > 4)
            {
                PaginationPages.Add(new PaginationPageItem { DisplayText = "...", IsEllipsis = true });
            }

            int start = Math.Max(2, CurrentPage - 1);
            int end = Math.Min(maxPages - 1, CurrentPage + 1);

            if (CurrentPage <= 4)
            {
                start = 2;
                end = 4;
            }
            else if (CurrentPage >= maxPages - 3)
            {
                start = maxPages - 3;
                end = maxPages - 1;
            }

            for (int i = start; i <= end; i++)
            {
                PaginationPages.Add(new PaginationPageItem
                {
                    PageNumber = i,
                    DisplayText = i.ToString(),
                    IsCurrent = (i == CurrentPage),
                    IsEllipsis = false
                });
            }

            if (CurrentPage < maxPages - 3)
            {
                PaginationPages.Add(new PaginationPageItem { DisplayText = "...", IsEllipsis = true });
            }

            // Trang cuối
            PaginationPages.Add(new PaginationPageItem
            {
                PageNumber = maxPages,
                DisplayText = maxPages.ToString(),
                IsCurrent = (CurrentPage == maxPages),
                IsEllipsis = false
            });
        }

        OnPropertyChanged(nameof(PaginationSummary));
        OnPropertyChanged(nameof(CanPreviousPage));
        OnPropertyChanged(nameof(CanNextPage));
    }

    private void LoadTimelineForSelectedResident()
    {
        SelectedResidentTimeline.Clear();
        if (SelectedResidenceItem == null) return;

        int resId = SelectedResidenceItem.ResidentId;
        var residentHistories = _allResidences
            .Where(r => r.ResidentId == resId)
            .OrderByDescending(r => r.StartDate)
            .ToList();

        for (int i = 0; i < residentHistories.Count; i++)
        {
            var rh = residentHistories[i];
            string aptNo = rh.Apartment?.ApartmentNumber ?? $"Căn {rh.ApartmentId}";
            string range = rh.IsActive
                ? $"Từ {rh.StartDate:dd/MM/yyyy} – Hiện tại"
                : $"{rh.StartDate:dd/MM/yyyy} – {rh.EndDate:dd/MM/yyyy}";

            SelectedResidentTimeline.Add(new ResidenceTimelineItem
            {
                ApartmentNumber = aptNo,
                IsActive = rh.IsActive,
                DateRangeText = range,
                IsLast = (i == residentHistories.Count - 1)
            });
        }
    }

    public async Task LoadRequestsAsync()
    {
        _allRequests.Clear();
        Requests.Clear();

        if (_memberRequestService != null)
        {
            var reqs = await _memberRequestService.GetAllRequestsWithDetailsAsync();
            foreach (var req in reqs)
            {
                string relText = req.RelationshipType switch
                {
                    RelationshipType.Owner => "Chủ hộ",
                    RelationshipType.FamilyMember => "Thành viên gia đình",
                    RelationshipType.Tenant => "Người thuê",
                    RelationshipType.Temporary => "Tạm trú",
                    _ => "Cư dân"
                };

                string statusText = req.Status switch
                {
                    RequestStatus.Pending => "Chưa duyệt",
                    RequestStatus.Approved => "Đã duyệt",
                    RequestStatus.Rejected => "Từ chối",
                    _ => "Chưa duyệt"
                };

                string bg = req.Status switch
                {
                    RequestStatus.Pending => "#FEF3C7",
                    RequestStatus.Approved => "#DCFCE7",
                    RequestStatus.Rejected => "#FEE2E2",
                    _ => "#F1F5F9"
                };

                string fg = req.Status switch
                {
                    RequestStatus.Pending => "#D97706",
                    RequestStatus.Approved => "#16A34A",
                    RequestStatus.Rejected => "#DC2626",
                    _ => "#475569"
                };

                string border = req.Status switch
                {
                    RequestStatus.Pending => "#FDE68A",
                    RequestStatus.Approved => "#BBF7D0",
                    RequestStatus.Rejected => "#FECACA",
                    _ => "#CBD5E1"
                };

                var item = new MemberRegistrationRequestItemViewModel
                {
                    Id = req.Id,
                    ApartmentId = req.ApartmentId,
                    ApartmentNumber = req.Apartment?.ApartmentNumber ?? $"P.{req.ApartmentId}",
                    RequesterResidentId = req.RequesterResidentId,
                    RequesterName = req.RequesterResident?.FullName ?? "Cư dân",
                    FullName = req.FullName,
                    DateOfBirth = req.DateOfBirth,
                    IdentityCard = req.IdentityCard,
                    PhoneNumber = req.PhoneNumber,
                    Email = req.Email,
                    RelationshipType = req.RelationshipType,
                    RelationshipText = relText,
                    Status = req.Status,
                    StatusText = statusText,
                    StatusBadgeBg = bg,
                    StatusBadgeFg = fg,
                    StatusBadgeBorder = border,
                    CreatedAt = req.CreatedAt,
                    ProcessedAt = req.ProcessedAt,
                    RejectReason = req.RejectReason
                };

                _allRequests.Add(item);
                Requests.Add(item);
            }
        }

        OnPropertyChanged(nameof(PendingRequestsCount));
        OnPropertyChanged(nameof(HasPendingRequests));
        OnPropertyChanged(nameof(HasRequests));
    }

    private async Task ApproveRequestAsync(MemberRegistrationRequestItemViewModel req)
    {
        if (_memberRequestService == null) return;

        IsLoading = true;
        ErrorMessage = string.Empty;
        RequestSuccessMessage = string.Empty;

        try
        {
            var res = await _memberRequestService.ApproveRequestAsync(req.Id);
            if (res.IsSuccess)
            {
                RequestSuccessMessage = $"Đã phê duyệt và thêm thành viên '{req.FullName}' vào căn hộ {req.ApartmentNumber} thành công!";
                await LoadDataAsync();
            }
            else
            {
                ErrorMessage = res.ErrorMessage ?? "Không thể phê duyệt yêu cầu.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Lỗi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task RejectRequestAsync(MemberRegistrationRequestItemViewModel req, string? reason)
    {
        if (_memberRequestService == null) return;

        IsLoading = true;
        ErrorMessage = string.Empty;
        RequestSuccessMessage = string.Empty;

        try
        {
            var res = await _memberRequestService.RejectRequestAsync(req.Id, reason);
            if (res.IsSuccess)
            {
                IsRejectDialogOpen = false;
                _requestToReject = null;
                RequestSuccessMessage = $"Đã từ chối yêu cầu của '{req.FullName}'.";
                await LoadRequestsAsync();
            }
            else
            {
                ErrorMessage = res.ErrorMessage ?? "Không thể từ chối yêu cầu.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Lỗi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void OpenRegisterForm()
    {
        ErrorMessage = string.Empty;
        SelectedResidence = null;
        SelectedApartmentInput = null;
        SelectedResidentInput = null;
        SelectedRelationshipType = RelationshipType.FamilyMember;
        StartDateInput = DateTime.Today;
        ActionType = "Register";
        FormTitle = "ĐĂNG KÝ CƯ TRÚ MỚI";
        IsFormOpen = true;
    }

    private void OpenTerminateForm(object? param)
    {
        ErrorMessage = string.Empty;
        ResidenceHistory? rh = null;

        if (param is ResidenceDisplayItemViewModel displayItem)
        {
            rh = displayItem.History;
        }
        else if (param is ResidenceHistory history)
        {
            rh = history;
        }
        else if (SelectedResidenceItem != null && SelectedResidenceItem.IsActive)
        {
            rh = SelectedResidenceItem.History;
        }
        else
        {
            rh = ActiveResidencesForSelection.FirstOrDefault();
        }

        SelectedResidence = rh;
        SelectedApartmentInput = rh?.Apartment;
        SelectedResidentInput = rh?.Resident;
        SelectedRelationshipType = rh?.RelationshipType ?? RelationshipType.FamilyMember;
        StartDateInput = rh?.StartDate ?? DateTime.Today;
        EndDateInput = DateTime.Today;
        ActionType = "Terminate";
        FormTitle = rh != null
            ? $"KẾT THÚC CƯ TRÚ: {rh.Resident?.FullName} ({rh.Apartment?.ApartmentNumber})"
            : "KẾT THÚC CƯ TRÚ";
        IsFormOpen = true;
    }

    private void OpenTransferForm(object? param)
    {
        ErrorMessage = string.Empty;
        ResidenceHistory? rh = null;

        if (param is ResidenceDisplayItemViewModel displayItem)
        {
            rh = displayItem.History;
        }
        else if (param is ResidenceHistory history)
        {
            rh = history;
        }
        else if (SelectedResidenceItem != null && SelectedResidenceItem.IsActive)
        {
            rh = SelectedResidenceItem.History;
        }
        else
        {
            rh = ActiveResidencesForSelection.FirstOrDefault();
        }

        SelectedResidence = rh;
        SelectedResidentInput = rh?.Resident;
        SelectedApartmentInput = null;
        SelectedRelationshipType = rh?.RelationshipType ?? RelationshipType.FamilyMember;
        StartDateInput = DateTime.Today;
        ActionType = "Transfer";
        FormTitle = rh != null
            ? $"CHUYỂN CĂN HỘ: {rh.Resident?.FullName} (Hiện tại: {rh.Apartment?.ApartmentNumber})"
            : "CHUYỂN CĂN HỘ";
        IsFormOpen = true;
    }

    private void CloseForm()
    {
        IsFormOpen = false;
        ErrorMessage = string.Empty;
    }

    private async Task SaveResidencyAsync()
    {
        ErrorMessage = string.Empty;
        IsLoading = true;
        try
        {
            if (ActionType == "Register")
            {
                if (SelectedApartmentInput == null)
                {
                    ErrorMessage = "Vui lòng chọn căn hộ.";
                    return;
                }
                if (SelectedResidentInput == null)
                {
                    ErrorMessage = "Vui lòng chọn cư dân.";
                    return;
                }

                var res = await _residenceService.RegisterResidenceAsync(
                    SelectedApartmentInput.Id,
                    SelectedResidentInput.Id,
                    SelectedRelationshipType,
                    StartDateInput
                );

                if (!res.IsSuccess)
                {
                    ErrorMessage = res.ErrorMessage ?? "Đăng ký cư trú thất bại.";
                    return;
                }
            }
            else if (ActionType == "Terminate")
            {
                if (SelectedResidence == null)
                {
                    ErrorMessage = "Không tìm thấy hồ sơ cư trú cần kết thúc.";
                    return;
                }

                var res = await _residenceService.TerminateResidenceAsync(SelectedResidence.Id, EndDateInput);
                if (!res.IsSuccess)
                {
                    ErrorMessage = res.ErrorMessage ?? "Kết thúc cư trú thất bại.";
                    return;
                }
            }
            else if (ActionType == "Transfer")
            {
                if (SelectedResidence == null)
                {
                    ErrorMessage = "Không tìm thấy hồ sơ cư trú hiện tại.";
                    return;
                }
                if (SelectedApartmentInput == null)
                {
                    ErrorMessage = "Vui lòng chọn căn hộ mới cần chuyển đến.";
                    return;
                }

                var res = await _residenceService.TransferApartmentAsync(
                    SelectedResidence.ResidentId,
                    SelectedApartmentInput.Id,
                    SelectedRelationshipType,
                    StartDateInput
                );

                if (!res.IsSuccess)
                {
                    ErrorMessage = res.ErrorMessage ?? "Chuyển căn hộ thất bại.";
                    return;
                }
            }

            CloseForm();
            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Đã xảy ra lỗi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
}
