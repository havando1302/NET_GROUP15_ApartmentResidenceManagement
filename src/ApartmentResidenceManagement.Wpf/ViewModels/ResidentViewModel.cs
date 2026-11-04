using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using ApartmentResidenceManagement.Application.Services;
using ApartmentResidenceManagement.Domain.Entities;
using ApartmentResidenceManagement.Domain.Enums;
using ApartmentResidenceManagement.Domain.Exceptions;
using ApartmentResidenceManagement.Wpf.Commands;
using ApartmentResidenceManagement.Wpf.Services;

namespace ApartmentResidenceManagement.Wpf.ViewModels;

#region Display Item Models

/// <summary>
/// ViewModel hiển thị từng dòng trong lịch sử cư trú của cư dân.
/// </summary>
public class ResidenceHistoryItemViewModel : ViewModelBase
{
    public int Stt { get; set; }
    public string ApartmentNumber { get; set; } = string.Empty;
    public string BuildingAndFloor { get; set; } = string.Empty;
    public string RelationshipText { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string StartDateFormatted => StartDate.ToString("dd/MM/yyyy");
    public string EndDateFormatted => EndDate.HasValue ? EndDate.Value.ToString("dd/MM/yyyy") : "Hiện tại";
    public bool IsActive { get; set; }

    public string StatusText => IsActive ? "Đang cư trú" : "Đã kết thúc";
    public string StatusBg => IsActive ? "#DCFCE7" : "#F1F5F9";
    public string StatusFg => IsActive ? "#15803D" : "#475569";
    public string StatusBorder => IsActive ? "#BBF7D0" : "#E2E8F0";
}

/// <summary>
/// ViewModel hiển thị cư dân trên bảng danh sách và thẻ chi tiết (Chuẩn UI 2026).
/// </summary>
public class ResidentListItemViewModel : ViewModelBase
{
    public Resident Resident { get; }
    public int Stt { get; set; }

    public int Id => Resident.Id;
    public string ResidentCode
    {
        get => $"CD{Resident.Id:D3}";
        set { }
    }

    public string FullName
    {
        get => Resident.FullName;
        set { }
    }

    public DateTime DateOfBirth => Resident.DateOfBirth;
    public string DateOfBirthFormatted
    {
        get => Resident.DateOfBirth.ToString("dd/MM/yyyy");
        set { }
    }

    public GenderType Gender => Resident.Gender;
    public string GenderText
    {
        get => Resident.Gender switch
        {
            GenderType.Male => "Nam",
            GenderType.Female => "Nữ",
            _ => "Khác"
        };
        set { }
    }

    public string IdentityCard
    {
        get => string.IsNullOrWhiteSpace(Resident.IdentityCard) ? "-" : Resident.IdentityCard;
        set { }
    }

    public string PhoneNumber
    {
        get => string.IsNullOrWhiteSpace(Resident.PhoneNumber) ? "-" : Resident.PhoneNumber;
        set { }
    }

    public string Email
    {
        get => string.IsNullOrWhiteSpace(Resident.Email) ? "-" : Resident.Email;
        set { }
    }

    public string HomeTown
    {
        get => string.IsNullOrWhiteSpace(Resident.HomeTown) ? "Chưa cập nhật" : Resident.HomeTown;
        set { }
    }

    // Cư trú hiện tại
    private string _apartmentNumber = "-";
    public string ApartmentNumber
    {
        get => _apartmentNumber;
        set => SetProperty(ref _apartmentNumber, value);
    }

    public string ApartmentFloor { get; set; } = string.Empty;
    public string RelationshipText { get; set; } = string.Empty;
    public int FamilyMembersCount { get; set; } = 0;

    private int? _apartmentId;
    public int? ApartmentId
    {
        get => _apartmentId;
        set => SetProperty(ref _apartmentId, value);
    }

    private DateTime? _residencyStartDate;
    public DateTime? ResidencyStartDate
    {
        get => _residencyStartDate;
        set
        {
            if (SetProperty(ref _residencyStartDate, value))
            {
                OnPropertyChanged(nameof(ResidencyStartDateFormatted));
            }
        }
    }

    public string ResidencyStartDateFormatted
    {
        get => ResidencyStartDate.HasValue
            ? ResidencyStartDate.Value.ToString("dd/MM/yyyy")
            : "Chưa có thông tin";
        set { }
    }

    // Phương tiện & Tài khoản
    public int VehiclesCount { get; set; } = 0;
    public string VehiclesSummary { get; set; } = "Chưa đăng ký phương tiện";
    public bool HasAccount { get; set; } = false;
    public string AccountUsername { get; set; } = "---";
    public string AccountStatusText { get; set; } = "Chưa có tài khoản";

    // Trạng thái cư trú: "Đang cư trú", "Đã chuyển đi", "Chưa đăng ký"
    private string _statusText = "Chưa đăng ký";
    public string StatusText
    {
        get => _statusText;
        set
        {
            if (SetProperty(ref _statusText, value))
            {
                UpdateStatusColors();
            }
        }
    }

    public string StatusDotColor { get; private set; } = "#D97706";
    public string StatusBgColor { get; private set; } = "#FEF3C7";
    public string StatusFgColor { get; private set; } = "#B45309";
    public string StatusBorderColor { get; private set; } = "#FDE68A";

    public bool IsCurrentlyResiding => StatusText == "Đang cư trú";

    private void UpdateStatusColors()
    {
        if (StatusText == "Đang cư trú")
        {
            StatusDotColor = "#16A34A";
            StatusBgColor = "#DCFCE7";
            StatusFgColor = "#15803D";
            StatusBorderColor = "#BBF7D0";
        }
        else if (StatusText == "Đã chuyển đi")
        {
            StatusDotColor = "#64748B";
            StatusBgColor = "#F1F5F9";
            StatusFgColor = "#475569";
            StatusBorderColor = "#E2E8F0";
        }
        else
        {
            // Chưa đăng ký
            StatusDotColor = "#D97706";
            StatusBgColor = "#FEF3C7";
            StatusFgColor = "#B45309";
            StatusBorderColor = "#FDE68A";
        }

        OnPropertyChanged(nameof(StatusDotColor));
        OnPropertyChanged(nameof(StatusBgColor));
        OnPropertyChanged(nameof(StatusFgColor));
        OnPropertyChanged(nameof(StatusBorderColor));
        OnPropertyChanged(nameof(IsCurrentlyResiding));
    }

    public string AvatarPath
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(Resident.AvatarPath))
            {
                return Resident.AvatarPath;
            }

            return Resident.Gender == GenderType.Female
                ? "/Assets/avatar_young_woman.png"
                : "/Assets/resident_avatar.png";
        }
    }

    public List<ResidenceHistoryItemViewModel> Histories { get; set; } = new();

    public ResidentListItemViewModel(Resident resident, int stt = 1)
    {
        Resident = resident;
        Stt = stt;
        UpdateStatusColors();
    }
}
#endregion

public class ResidentViewModel : ViewModelBase
{
    private readonly ResidentService _residentService;
    private readonly AccountService _accountService;
    private readonly ResidenceService? _residenceService;
    private readonly ApartmentService? _apartmentService;
    private readonly VehicleService? _vehicleService;
    private readonly SemaphoreSlim _loadLock = new(1, 1);

    // Dữ liệu gốc
    private List<ResidentListItemViewModel> _allResidentItems = new();
    private ObservableCollection<ResidentListItemViewModel> _pagedResidents = new();
    private ResidentListItemViewModel? _selectedResidentItem;
    private bool _isDetailPanelOpen = true;

    // Backward compatibility
    private ObservableCollection<Resident> _residents = new();
    private Resident? _selectedResident;

    // KPI Thống kê
    private int _totalResidentsCount;
    private int _activeResidentsCount;
    private int _movedOutResidentsCount;
    private int _unregisteredResidentsCount;

    // Lọc & Tìm kiếm
    private string _searchText = string.Empty;
    private string _selectedStatusFilter = "Tất cả";
    private string _selectedApartmentFilter = "Tất cả";
    private ObservableCollection<string> _apartmentFilterOptions = new() { "Tất cả" };

    // Phân trang
    private int _currentPage = 1;
    private int _pageSize = 8;
    private int _totalItems;
    private int _totalPages = 1;
    private ObservableCollection<PaginationPageItem> _paginationPages = new();

    // Form Thêm / Cập nhật Cư dân
    private Resident _editingResident = new();
    private bool _isFormOpen;
    private string _formTitle = "THÊM CƯ DÂN MỚI";

    // Tùy chọn đăng ký cư trú nhanh khi thêm cư dân mới
    private bool _isQuickResidenceRegistration;
    private ObservableCollection<Apartment> _availableApartments = new();
    private Apartment? _selectedApartmentForRegistration;
    private RelationshipType _registrationRelationship = RelationshipType.FamilyMember;
    private DateTime _registrationStartDate = DateTime.Today;

    // Quản lý Modal Lịch sử cư trú của cư dân
    private bool _isHistoryDialogOpen;
    private ObservableCollection<ResidenceHistoryItemViewModel> _residentHistoryList = new();

    // Quản lý Panel cấp tài khoản (Backward compatibility)
    private bool _isAccountPanelOpen;
    private string _accountUsername = string.Empty;
    private string _accountPassword = string.Empty;
    private bool _selectedResidentHasAccount;
    private UserAccount? _selectedResidentAccount;
    private string _accountStatusText = string.Empty;

    // Trạng thái / Thông báo
    private string _errorMessage = string.Empty;
    private string _successMessage = string.Empty;
    private bool _hasError;
    private bool _isLoading;

    #region Properties
    public ObservableCollection<ResidentListItemViewModel> PagedResidents
    {
        get => _pagedResidents;
        set => SetProperty(ref _pagedResidents, value);
    }

    public ResidentListItemViewModel? SelectedResidentItem
    {
        get => _selectedResidentItem;
        set
        {
            if (SetProperty(ref _selectedResidentItem, value))
            {
                SelectedResident = value?.Resident;
                OnPropertyChanged(nameof(SelectedResidentItem));
            }
        }
    }

    public bool IsDetailPanelOpen
    {
        get => _isDetailPanelOpen;
        set => SetProperty(ref _isDetailPanelOpen, value);
    }

    // Backward compatibility
    public ObservableCollection<Resident> Residents
    {
        get => _residents;
        set => SetProperty(ref _residents, value);
    }

    public Resident? SelectedResident
    {
        get => _selectedResident;
        set => SetProperty(ref _selectedResident, value);
    }

    public Resident EditingResident
    {
        get => _editingResident;
        set => SetProperty(ref _editingResident, value);
    }

    // KPI
    public int TotalResidentsCount
    {
        get => _totalResidentsCount;
        set => SetProperty(ref _totalResidentsCount, value);
    }

    public int ActiveResidentsCount
    {
        get => _activeResidentsCount;
        set => SetProperty(ref _activeResidentsCount, value);
    }

    public int MovedOutResidentsCount
    {
        get => _movedOutResidentsCount;
        set => SetProperty(ref _movedOutResidentsCount, value);
    }

    public int UnregisteredResidentsCount
    {
        get => _unregisteredResidentsCount;
        set => SetProperty(ref _unregisteredResidentsCount, value);
    }

    // Filters
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

    public string SelectedStatusFilter
    {
        get => _selectedStatusFilter;
        set
        {
            if (SetProperty(ref _selectedStatusFilter, value))
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

    public List<string> StatusFilterOptions { get; } = new() { "Tất cả", "Đang cư trú", "Đã chuyển đi", "Chưa đăng ký" };

    public ObservableCollection<string> ApartmentFilterOptions
    {
        get => _apartmentFilterOptions;
        set => SetProperty(ref _apartmentFilterOptions, value);
    }

    // Pagination
    public int CurrentPage
    {
        get => _currentPage;
        set
        {
            if (SetProperty(ref _currentPage, value))
            {
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

    public List<int> PageSizeOptions { get; } = new() { 8, 10, 15, 20, 50 };

    public int TotalPages
    {
        get => _totalPages;
        set => SetProperty(ref _totalPages, value);
    }

    public int TotalItems
    {
        get => _totalItems;
        set => SetProperty(ref _totalItems, value);
    }

    public ObservableCollection<PaginationPageItem> PaginationPages
    {
        get => _paginationPages;
        set => SetProperty(ref _paginationPages, value);
    }

    // Form
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

    public bool IsQuickResidenceRegistration
    {
        get => _isQuickResidenceRegistration;
        set => SetProperty(ref _isQuickResidenceRegistration, value);
    }

    public ObservableCollection<Apartment> AvailableApartments
    {
        get => _availableApartments;
        set => SetProperty(ref _availableApartments, value);
    }

    public Apartment? SelectedApartmentForRegistration
    {
        get => _selectedApartmentForRegistration;
        set => SetProperty(ref _selectedApartmentForRegistration, value);
    }

    public RelationshipType RegistrationRelationship
    {
        get => _registrationRelationship;
        set => SetProperty(ref _registrationRelationship, value);
    }

    public List<RelationshipType> RelationshipOptions { get; } = Enum.GetValues(typeof(RelationshipType)).Cast<RelationshipType>().ToList();

    public DateTime RegistrationStartDate
    {
        get => _registrationStartDate;
        set => SetProperty(ref _registrationStartDate, value);
    }

    // Lịch sử cư trú modal
    public bool IsHistoryDialogOpen
    {
        get => _isHistoryDialogOpen;
        set => SetProperty(ref _isHistoryDialogOpen, value);
    }

    public ObservableCollection<ResidenceHistoryItemViewModel> ResidentHistoryList
    {
        get => _residentHistoryList;
        set => SetProperty(ref _residentHistoryList, value);
    }

    // Tài khoản properties
    public bool IsAccountPanelOpen
    {
        get => _isAccountPanelOpen;
        set => SetProperty(ref _isAccountPanelOpen, value);
    }

    public string AccountUsername
    {
        get => _accountUsername;
        set => SetProperty(ref _accountUsername, value);
    }

    public string AccountPassword
    {
        get => _accountPassword;
        set => SetProperty(ref _accountPassword, value);
    }

    public bool SelectedResidentHasAccount
    {
        get => _selectedResidentHasAccount;
        set => SetProperty(ref _selectedResidentHasAccount, value);
    }

    public string AccountStatusText
    {
        get => _accountStatusText;
        set => SetProperty(ref _accountStatusText, value);
    }

    // Báo lỗi & Loading
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

    public string SuccessMessage
    {
        get => _successMessage;
        set => SetProperty(ref _successMessage, value);
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

    // Giới tính options
    public List<GenderType> GenderOptions { get; } = Enum.GetValues(typeof(GenderType)).Cast<GenderType>().ToList();
    #endregion

    #region Commands
    public ICommand LoadResidentsCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand ViewResidentDetailCommand { get; }
    public ICommand CloseDetailCommand { get; }
    public ICommand ToggleDetailCommand { get; }

    public ICommand OpenAddFormCommand { get; }
    public ICommand OpenEditFormCommand { get; }
    public ICommand SaveResidentCommand { get; }
    public ICommand CancelFormCommand { get; }
    public ICommand DeleteResidentCommand { get; }

    // Phân trang Commands
    public ICommand PreviousPageCommand { get; }
    public ICommand NextPageCommand { get; }
    public ICommand GoToPageCommand { get; }

    // Lịch sử cư trú Commands
    public ICommand OpenResidencyHistoryModalCommand { get; }
    public ICommand CloseResidencyHistoryModalCommand { get; }

    // Account Commands
    public ICommand OpenAccountPanelCommand { get; }
    public ICommand CreateAccountCommand { get; }
    public ICommand ToggleAccountStatusCommand { get; }
    public ICommand CloseAccountPanelCommand { get; }
    #endregion

    public ResidentViewModel(ResidentService residentService, AccountService accountService)
        : this(residentService, accountService, null, null, null)
    {
    }

    public ResidentViewModel(
        ResidentService residentService,
        AccountService accountService,
        ResidenceService? residenceService,
        ApartmentService? apartmentService)
        : this(residentService, accountService, residenceService, apartmentService, null)
    {
    }

    public ResidentViewModel(
        ResidentService residentService,
        AccountService accountService,
        ResidenceService? residenceService,
        ApartmentService? apartmentService,
        VehicleService? vehicleService)
    {
        _residentService = residentService;
        _accountService = accountService;
        _residenceService = residenceService;
        _apartmentService = apartmentService;
        _vehicleService = vehicleService;

        LoadResidentsCommand = new AsyncRelayCommand(_ => LoadDataAsync());
        RefreshCommand = new AsyncRelayCommand(_ => LoadDataAsync());

        ViewResidentDetailCommand = new RelayCommand(p =>
        {
            if (p is ResidentListItemViewModel item)
            {
                SelectedResidentItem = item;
                IsDetailPanelOpen = true;
            }
        });

        CloseDetailCommand = new RelayCommand(_ =>
        {
            IsDetailPanelOpen = false;
        });

        ToggleDetailCommand = new RelayCommand(_ =>
        {
            IsDetailPanelOpen = !IsDetailPanelOpen;
        });

        OpenAddFormCommand = new AsyncRelayCommand(_ => OpenAddFormAsync());
        OpenEditFormCommand = new RelayCommand(r =>
        {
            if (r is ResidentListItemViewModel resItem)
            {
                OpenEditForm(resItem.Resident);
            }
            else if (r is Resident res)
            {
                OpenEditForm(res);
            }
        });

        SaveResidentCommand = new AsyncRelayCommand(_ => SaveDataAsync());
        CancelFormCommand = new RelayCommand(_ => CloseForm());
        DeleteResidentCommand = new AsyncRelayCommand(DeleteDataAsync);

        // Pagination
        PreviousPageCommand = new RelayCommand(_ =>
        {
            if (CurrentPage > 1) CurrentPage--;
        });

        NextPageCommand = new RelayCommand(_ =>
        {
            if (CurrentPage < TotalPages) CurrentPage++;
        });

        GoToPageCommand = new RelayCommand(p =>
        {
            int pageNum = 1;
            if (p is PaginationPageItem pageItem) pageNum = pageItem.PageNumber;
            else if (p is int i) pageNum = i;
            else if (p is string s && int.TryParse(s, out int parsed)) pageNum = parsed;

            if (pageNum >= 1 && pageNum <= TotalPages)
            {
                CurrentPage = pageNum;
            }
        });

        // History modal
        OpenResidencyHistoryModalCommand = new RelayCommand(_ => OpenResidencyHistoryModal());
        CloseResidencyHistoryModalCommand = new RelayCommand(_ => { IsHistoryDialogOpen = false; });

        // Account
        OpenAccountPanelCommand = new AsyncRelayCommand(async r =>
        {
            int resId = 0;
            if (r is ResidentListItemViewModel item)
            {
                SelectedResidentItem = item;
                resId = item.Id;
            }
            else if (r is Resident res)
            {
                SelectedResident = res;
                resId = res.Id;
            }

            if (resId > 0)
            {
                await LoadAccountInfoAsync(resId);
            }
        });

        CreateAccountCommand = new AsyncRelayCommand(_ => CreateAccountAsync());
        ToggleAccountStatusCommand = new AsyncRelayCommand(_ => ToggleAccountStatusAsync());
        CloseAccountPanelCommand = new RelayCommand(_ =>
        {
            IsAccountPanelOpen = false;
        });

        _ = LoadDataAsync();
    }

    #region MouseWheel Paging
    public void SelectNextItemOrNextPage()
    {
        if (PagedResidents.Count == 0) return;

        if (SelectedResidentItem == null)
        {
            SelectedResidentItem = PagedResidents.FirstOrDefault();
            return;
        }

        int currentIndex = PagedResidents.IndexOf(SelectedResidentItem);
        if (currentIndex >= 0 && currentIndex < PagedResidents.Count - 1)
        {
            SelectedResidentItem = PagedResidents[currentIndex + 1];
        }
        else if (currentIndex == PagedResidents.Count - 1)
        {
            if (CurrentPage < TotalPages)
            {
                CurrentPage++;
                SelectedResidentItem = PagedResidents.FirstOrDefault();
            }
        }
    }

    public void SelectPreviousItemOrPreviousPage()
    {
        if (PagedResidents.Count == 0) return;

        if (SelectedResidentItem == null)
        {
            SelectedResidentItem = PagedResidents.LastOrDefault();
            return;
        }

        int currentIndex = PagedResidents.IndexOf(SelectedResidentItem);
        if (currentIndex > 0)
        {
            SelectedResidentItem = PagedResidents[currentIndex - 1];
        }
        else if (currentIndex == 0)
        {
            if (CurrentPage > 1)
            {
                CurrentPage--;
                SelectedResidentItem = PagedResidents.LastOrDefault();
            }
        }
    }
    #endregion

    #region Data Loading & Filtering
    public async Task LoadDataAsync()
    {
        await _loadLock.WaitAsync();
        try
        {
            ErrorMessage = string.Empty;
            SuccessMessage = string.Empty;
            IsLoading = true;

            var residentList = (await _residentService.GetAllResidentsAsync()).ToList();

            // 1. Lấy danh sách lịch sử cư trú và căn hộ
            List<ResidenceHistory> historyList = new();
            if (_residenceService != null)
            {
                try
                {
                    historyList = (await _residenceService.GetAllResidencesAsync()).ToList();
                }
                catch { }
            }

            List<Apartment> apartmentList = new();
            if (_apartmentService != null)
            {
                try
                {
                    apartmentList = (await _apartmentService.GetAllApartmentsAsync()).ToList();
                }
                catch { }
            }

            // 2. Lấy danh sách phương tiện
            List<Vehicle> vehicleList = new();
            if (_vehicleService != null)
            {
                try
                {
                    vehicleList = (await _vehicleService.GetAllVehiclesAsync()).ToList();
                }
                catch { }
            }

            // 3. Lấy danh sách tài khoản
            List<UserAccount> accountList = new();
            if (_accountService != null)
            {
                try
                {
                    accountList = (await _accountService.GetAllAccountsWithResidentAsync()).ToList();
                }
                catch { }
            }

            // Gom nhóm lịch sử cư trú theo ResidentId
            var historyGroup = historyList
                .GroupBy(h => h.ResidentId)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(h => h.StartDate).ToList());

            // Đếm số thành viên đang cư trú trong từng căn hộ
            var activeAptMemberCounts = historyList
                .Where(h => h.IsActive)
                .GroupBy(h => h.ApartmentId)
                .ToDictionary(g => g.Key, g => g.Select(x => x.ResidentId).Distinct().Count());

            // Gom nhóm phương tiện theo OwnerId
            var vehiclesByResident = vehicleList
                .GroupBy(v => v.OwnerId)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Gom nhóm tài khoản theo ResidentId
            var accountsByResident = accountList
                .Where(a => a.ResidentId.HasValue)
                .ToDictionary(a => a.ResidentId!.Value, a => a);

            var items = new List<ResidentListItemViewModel>();
            int stt = 1;

            foreach (var r in residentList.OrderBy(x => x.Id))
            {
                var item = new ResidentListItemViewModel(r, stt++);

                // Cư trú & Lịch sử
                if (historyGroup.TryGetValue(r.Id, out var rHistories) && rHistories.Count > 0)
                {
                    int hStt = 1;
                    item.Histories = rHistories.Select(h => new ResidenceHistoryItemViewModel
                    {
                        Stt = hStt++,
                        ApartmentNumber = h.Apartment?.ApartmentNumber ?? $"Căn hộ {h.ApartmentId}",
                        BuildingAndFloor = h.Apartment != null ? $"Tầng {h.Apartment.Floor}" : "",
                        RelationshipText = GetRelationshipVietnamese(h.RelationshipType),
                        StartDate = h.StartDate,
                        EndDate = h.EndDate,
                        IsActive = h.IsActive
                    }).ToList();

                    var activeResidence = rHistories.FirstOrDefault(h => h.IsActive);
                    if (activeResidence != null)
                    {
                        item.ApartmentNumber = activeResidence.Apartment?.ApartmentNumber ?? $"Căn hộ {activeResidence.ApartmentId}";
                        item.ApartmentId = activeResidence.ApartmentId;
                        item.ApartmentFloor = activeResidence.Apartment != null ? $"Tầng {activeResidence.Apartment.Floor}" : "";
                        item.RelationshipText = GetRelationshipVietnamese(activeResidence.RelationshipType);
                        item.ResidencyStartDate = activeResidence.StartDate;
                        item.StatusText = "Đang cư trú";

                        if (activeAptMemberCounts.TryGetValue(activeResidence.ApartmentId, out int mCount))
                        {
                            item.FamilyMembersCount = mCount;
                        }
                    }
                    else
                    {
                        item.ApartmentNumber = "-";
                        item.ApartmentId = null;
                        item.ResidencyStartDate = null;
                        item.StatusText = "Đã chuyển đi";
                    }
                }
                else
                {
                    item.ApartmentNumber = "-";
                    item.ApartmentId = null;
                    item.ResidencyStartDate = null;
                    item.StatusText = "Chưa đăng ký";
                }

                // Phương tiện
                if (vehiclesByResident.TryGetValue(r.Id, out var rVehicles) && rVehicles.Count > 0)
                {
                    item.VehiclesCount = rVehicles.Count;
                    item.VehiclesSummary = string.Join(", ", rVehicles.Select(v => $"{v.LicensePlate} ({GetVehicleTypeName(v.VehicleType)})"));
                }
                else
                {
                    item.VehiclesCount = 0;
                    item.VehiclesSummary = "Chưa đăng ký";
                }

                // Tài khoản
                if (accountsByResident.TryGetValue(r.Id, out var rAccount))
                {
                    item.HasAccount = true;
                    item.AccountUsername = rAccount.Username;
                    item.AccountStatusText = rAccount.IsActive ? "Đang hoạt động" : "Đã bị khóa";
                }
                else
                {
                    item.HasAccount = false;
                    item.AccountUsername = "---";
                    item.AccountStatusText = "Chưa cấp tài khoản";
                }

                items.Add(item);
            }

            _allResidentItems = items;

            // Tính toán 4 KPI
            TotalResidentsCount = _allResidentItems.Count;
            ActiveResidentsCount = _allResidentItems.Count(r => r.StatusText == "Đang cư trú");
            MovedOutResidentsCount = _allResidentItems.Count(r => r.StatusText == "Đã chuyển đi");
            UnregisteredResidentsCount = _allResidentItems.Count(r => r.StatusText == "Chưa đăng ký");

            // Cập nhật danh sách căn hộ cho bộ lọc
            UpdateApartmentFilterOptions();

            // Đồng bộ bộ sưu tập Residents cho backward compatibility
            Residents.Clear();
            foreach (var r in residentList)
            {
                Residents.Add(r);
            }

            // Áp dụng bộ lọc và phân trang
            ApplyFilters();

            // Khôi phục lựa chọn cư dân
            if (SelectedResidentItem != null)
            {
                SelectedResidentItem = _allResidentItems.FirstOrDefault(x => x.Id == SelectedResidentItem.Id)
                                      ?? PagedResidents.FirstOrDefault();
            }
            else
            {
                SelectedResidentItem = PagedResidents.FirstOrDefault();
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Đã xảy ra lỗi khi lấy danh sách cư dân từ database: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
            _loadLock.Release();
        }
    }

    private static string GetVehicleTypeName(VehicleType type)
    {
        return type switch
        {
            VehicleType.Car => "Ô tô",
            VehicleType.Moto => "Xe máy",
            VehicleType.Bicycle => "Xe đạp",
            _ => "Khác"
        };
    }

    private void UpdateApartmentFilterOptions()
    {
        var aptNumbers = _allResidentItems
            .Select(r => r.ApartmentNumber)
            .Where(apt => !string.IsNullOrWhiteSpace(apt) && apt != "-")
            .Distinct()
            .OrderBy(apt => apt)
            .ToList();

        string currentSelected = SelectedApartmentFilter;

        ApartmentFilterOptions.Clear();
        ApartmentFilterOptions.Add("Tất cả");
        foreach (var apt in aptNumbers)
        {
            ApartmentFilterOptions.Add(apt);
        }

        if (ApartmentFilterOptions.Contains(currentSelected))
        {
            SelectedApartmentFilter = currentSelected;
        }
        else
        {
            SelectedApartmentFilter = "Tất cả";
        }
    }

    private void ApplyFilters()
    {
        var filtered = _allResidentItems.AsEnumerable();

        // 1. Tìm kiếm theo mã, họ tên, số định danh, số điện thoại
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            string keyword = SearchText.Trim().ToLower();
            filtered = filtered.Where(r =>
                r.ResidentCode.ToLower().Contains(keyword) ||
                r.FullName.ToLower().Contains(keyword) ||
                (r.IdentityCard != null && r.IdentityCard.ToLower().Contains(keyword)) ||
                (r.PhoneNumber != null && r.PhoneNumber.ToLower().Contains(keyword)));
        }

        // 2. Lọc theo trạng thái
        if (!string.IsNullOrWhiteSpace(SelectedStatusFilter) && SelectedStatusFilter != "Tất cả")
        {
            filtered = filtered.Where(r => r.StatusText == SelectedStatusFilter);
        }

        // 3. Lọc theo căn hộ
        if (!string.IsNullOrWhiteSpace(SelectedApartmentFilter) && SelectedApartmentFilter != "Tất cả")
        {
            filtered = filtered.Where(r => r.ApartmentNumber == SelectedApartmentFilter);
        }

        var resultList = filtered.ToList();
        TotalItems = resultList.Count;

        TotalPages = (int)Math.Ceiling((double)TotalItems / PageSize);
        if (TotalPages < 1) TotalPages = 1;
        if (CurrentPage > TotalPages) CurrentPage = TotalPages;
        if (CurrentPage < 1) CurrentPage = 1;

        int skip = (CurrentPage - 1) * PageSize;
        var pagedList = resultList.Skip(skip).Take(PageSize).ToList();

        // Cập nhật số thứ tự (#)
        for (int i = 0; i < pagedList.Count; i++)
        {
            pagedList[i].Stt = skip + i + 1;
        }

        PagedResidents.Clear();
        foreach (var item in pagedList)
        {
            PagedResidents.Add(item);
        }

        UpdatePaginationNumbers();

        // Nếu SelectedResidentItem không còn trong trang hiện tại, chọn dòng đầu tiên
        if (SelectedResidentItem == null || !PagedResidents.Any(r => r.Id == SelectedResidentItem.Id))
        {
            SelectedResidentItem = PagedResidents.FirstOrDefault();
        }
    }

    private void ApplyPagination()
    {
        ApplyFilters();
    }

    private void UpdatePaginationNumbers()
    {
        PaginationPages.Clear();
        int maxVisible = 5;

        if (TotalPages <= maxVisible + 2)
        {
            for (int i = 1; i <= TotalPages; i++)
            {
                PaginationPages.Add(new PaginationPageItem
                {
                    PageNumber = i,
                    DisplayText = i.ToString(),
                    IsCurrent = (i == CurrentPage)
                });
            }
            return;
        }

        // Trang đầu (1)
        PaginationPages.Add(new PaginationPageItem
        {
            PageNumber = 1,
            DisplayText = "1",
            IsCurrent = (CurrentPage == 1)
        });

        int start = Math.Max(2, CurrentPage - 1);
        int end = Math.Min(TotalPages - 1, CurrentPage + 1);

        if (CurrentPage <= 3)
        {
            start = 2;
            end = 4;
        }
        else if (CurrentPage >= TotalPages - 2)
        {
            start = TotalPages - 3;
            end = TotalPages - 1;
        }

        if (start > 2)
        {
            PaginationPages.Add(new PaginationPageItem { PageNumber = 0, DisplayText = "...", IsEllipsis = true });
        }

        for (int i = start; i <= end; i++)
        {
            PaginationPages.Add(new PaginationPageItem
            {
                PageNumber = i,
                DisplayText = i.ToString(),
                IsCurrent = (i == CurrentPage)
            });
        }

        if (end < TotalPages - 1)
        {
            PaginationPages.Add(new PaginationPageItem { PageNumber = 0, DisplayText = "...", IsEllipsis = true });
        }

        // Trang cuối
        PaginationPages.Add(new PaginationPageItem
        {
            PageNumber = TotalPages,
            DisplayText = TotalPages.ToString(),
            IsCurrent = (CurrentPage == TotalPages)
        });
    }
    #endregion

    #region Form Thêm / Cập nhật Cư dân
    private async Task OpenAddFormAsync()
    {
        ErrorMessage = string.Empty;
        FormTitle = "THÊM CƯ DÂN MỚI";
        EditingResident = new Resident
        {
            Gender = GenderType.Male,
            DateOfBirth = DateTime.Now.AddYears(-20)
        };

        IsQuickResidenceRegistration = false;
        RegistrationRelationship = RelationshipType.FamilyMember;
        RegistrationStartDate = DateTime.Today;

        if (_apartmentService != null)
        {
            try
            {
                var apartments = await _apartmentService.GetAllApartmentsAsync();
                AvailableApartments = new ObservableCollection<Apartment>(apartments.OrderBy(a => a.ApartmentNumber));
                SelectedApartmentForRegistration = AvailableApartments.FirstOrDefault();
            }
            catch
            {
                AvailableApartments.Clear();
            }
        }

        IsFormOpen = true;
        IsAccountPanelOpen = false;
        IsHistoryDialogOpen = false;
    }

    private void OpenEditForm(Resident resident)
    {
        ErrorMessage = string.Empty;
        FormTitle = $"CẬP NHẬT CƯ DÂN: {resident.FullName}";

        // Clone đối tượng để tránh sửa trực tiếp trước khi lưu
        EditingResident = new Resident
        {
            Id = resident.Id,
            FullName = resident.FullName,
            DateOfBirth = resident.DateOfBirth,
            Gender = resident.Gender,
            IdentityCard = resident.IdentityCard,
            PhoneNumber = resident.PhoneNumber,
            HomeTown = resident.HomeTown,
            Email = resident.Email,
            AvatarPath = resident.AvatarPath
        };

        IsQuickResidenceRegistration = false;
        IsFormOpen = true;
        IsAccountPanelOpen = false;
        IsHistoryDialogOpen = false;
    }

    private void CloseForm()
    {
        IsFormOpen = false;
        ErrorMessage = string.Empty;
        IsAccountPanelOpen = false;
    }

    private async Task SaveDataAsync()
    {
        ErrorMessage = string.Empty;

        // Validation cơ bản
        if (string.IsNullOrWhiteSpace(EditingResident.FullName))
        {
            ErrorMessage = "Vui lòng nhập họ và tên cư dân.";
            return;
        }

        try
        {
            if (EditingResident.Id == 0)
            {
                // Thêm mới cư dân vào CSDL
                var createdResident = await _residentService.CreateResidentAsync(EditingResident);

                // Nếu có chọn đăng ký cư trú ngay
                if (IsQuickResidenceRegistration && SelectedApartmentForRegistration != null && _residenceService != null)
                {
                    try
                    {
                        var regResult = await _residenceService.RegisterResidenceAsync(
                            SelectedApartmentForRegistration.Id,
                            createdResident.Id,
                            RegistrationRelationship,
                            RegistrationStartDate);

                        if (!regResult.IsSuccess)
                        {
                            ErrorMessage = $"Đã tạo cư dân nhưng đăng ký cư trú thất bại: {regResult.ErrorMessage}";
                            await LoadDataAsync();
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        ErrorMessage = $"Đã tạo cư dân nhưng lỗi đăng ký cư trú: {ex.Message}";
                        await LoadDataAsync();
                        return;
                    }
                }

                SuccessMessage = $"Đã thêm mới cư dân '{createdResident.FullName}' thành công!";
            }
            else
            {
                // Cập nhật cư dân hiện có
                await _residentService.UpdateResidentAsync(EditingResident);
                SuccessMessage = $"Đã cập nhật thông tin cư dân '{EditingResident.FullName}' thành công!";
            }

            IsFormOpen = false;
            await LoadDataAsync();
        }
        catch (BusinessRuleException ex)
        {
            ErrorMessage = ex.Message;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Lỗi hệ thống trong quá trình lưu dữ liệu: {ex.Message}";
        }
    }

    private async Task DeleteDataAsync(object? parameter)
    {
        Resident? targetResident = null;
        if (parameter is ResidentListItemViewModel item)
        {
            targetResident = item.Resident;
        }
        else if (parameter is Resident res)
        {
            targetResident = res;
        }

        if (targetResident == null) return;

        ErrorMessage = string.Empty;

        var result = NotificationService.Show(
            $"Bạn có chắc chắn muốn xóa cư dân {targetResident.FullName} (CCCD: {targetResident.IdentityCard ?? "---"}) không?",
            "Xác nhận xóa cư dân",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning
        );

        if (result != System.Windows.MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            await _residentService.DeleteResidentAsync(targetResident.Id);
            SuccessMessage = $"Đã xóa cư dân '{targetResident.FullName}' khỏi hệ thống.";
            await LoadDataAsync();
        }
        catch (BusinessRuleException ex)
        {
            ErrorMessage = ex.Message;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Lỗi hệ thống trong quá trình xóa cư dân: {ex.Message}";
        }
    }
    #endregion

    #region Lịch sử cư trú Modal Logic
    private void OpenResidencyHistoryModal()
    {
        if (SelectedResidentItem == null) return;

        ResidentHistoryList = new ObservableCollection<ResidenceHistoryItemViewModel>(SelectedResidentItem.Histories);
        IsHistoryDialogOpen = true;
    }

    private static string GetRelationshipVietnamese(RelationshipType relationship)
    {
        return relationship switch
        {
            RelationshipType.Owner => "Chủ hộ",
            RelationshipType.FamilyMember => "Thành viên gia đình",
            RelationshipType.Tenant => "Khách thuê",
            RelationshipType.Temporary => "Tạm trú",
            _ => "Khác"
        };
    }
    #endregion

    #region Account Management Logic (Backward Compatibility)
    private async Task LoadAccountInfoAsync(int residentId)
    {
        ErrorMessage = string.Empty;
        AccountUsername = string.Empty;
        AccountPassword = string.Empty;

        try
        {
            _selectedResidentAccount = await _accountService.GetAccountByResidentIdAsync(residentId);

            if (_selectedResidentAccount != null)
            {
                SelectedResidentHasAccount = true;
                AccountUsername = _selectedResidentAccount.Username;
                AccountStatusText = _selectedResidentAccount.IsActive ? "ĐANG HOẠT ĐỘNG" : "ĐÃ BỊ KHÓA";
            }
            else
            {
                SelectedResidentHasAccount = false;
                AccountStatusText = "CHƯA CÓ TÀI KHOẢN";
            }

            IsAccountPanelOpen = true;
        }
        catch (Exception)
        {
            ErrorMessage = "Lỗi khi lấy thông tin tài khoản cư dân.";
        }
    }

    private async Task CreateAccountAsync()
    {
        if (SelectedResident == null) return;
        ErrorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(AccountUsername))
        {
            ErrorMessage = "Tên đăng nhập không được để trống.";
            return;
        }

        if (string.IsNullOrWhiteSpace(AccountPassword) || AccountPassword.Length < 6)
        {
            ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự.";
            return;
        }

        try
        {
            var newAcc = await _accountService.CreateResidentAccountAsync(SelectedResident.Id, AccountUsername, AccountPassword);
            if (newAcc != null)
            {
                await LoadAccountInfoAsync(SelectedResident.Id);
            }
        }
        catch (BusinessRuleException ex)
        {
            ErrorMessage = ex.Message;
        }
        catch (Exception)
        {
            ErrorMessage = "Lỗi hệ thống khi cấp tài khoản.";
        }
    }

    private async Task ToggleAccountStatusAsync()
    {
        if (_selectedResidentAccount == null || SelectedResident == null) return;
        ErrorMessage = string.Empty;

        try
        {
            await _accountService.ToggleAccountStatusAsync(_selectedResidentAccount.Id);
            await LoadAccountInfoAsync(SelectedResident.Id);
        }
        catch (BusinessRuleException ex)
        {
            ErrorMessage = ex.Message;
        }
        catch (Exception)
        {
            ErrorMessage = "Lỗi khi thay đổi trạng thái tài khoản.";
        }
    }
    #endregion
}
