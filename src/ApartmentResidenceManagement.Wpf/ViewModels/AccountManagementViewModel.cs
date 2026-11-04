using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using ApartmentResidenceManagement.Application.Services;
using ApartmentResidenceManagement.Domain.Entities;
using ApartmentResidenceManagement.Domain.Enums;
using ApartmentResidenceManagement.Domain.Exceptions;
using ApartmentResidenceManagement.Wpf.Commands;

namespace ApartmentResidenceManagement.Wpf.ViewModels;

#region Display Item Models
public class AccountDisplayItemViewModel : ViewModelBase
{
    public UserAccount Account { get; }
    public int Stt { get; set; }

    public int Id => Account.Id;
    public string Username => Account.Username;
    public UserRole Role => Account.Role;
    public bool IsAdmin => Account.Role == UserRole.Admin;
    public bool IsResident => Account.Role == UserRole.Resident;

    public int? ResidentId => Account.ResidentId;
    public string ResidentCode => Account.ResidentId.HasValue ? $"CD{Account.ResidentId.Value:D3}" : "---";
    public string AccountCode => $"TK{Account.Id:D3}";

    public string FullName => Account.Role == UserRole.Admin
        ? "Ban quản lý"
        : (Account.Resident?.FullName ?? "Chưa rõ");

    public string RoleText => Account.Role == UserRole.Admin ? "Admin" : "Cư dân";
    public string RoleIcon => Account.Role == UserRole.Admin ? "👑" : "👤";
    public string RoleBadgeBg => Account.Role == UserRole.Admin ? "#F3E8FF" : "#EFF6FF";
    public string RoleBadgeFg => Account.Role == UserRole.Admin ? "#7E22CE" : "#1D4ED8";
    public string RoleBadgeBorder => Account.Role == UserRole.Admin ? "#E9D5FF" : "#DBEAFE";

    private bool _isActive;
    public bool IsActive
    {
        get => _isActive;
        set
        {
            if (SetProperty(ref _isActive, value))
            {
                OnPropertyChanged(nameof(StatusText));
                OnPropertyChanged(nameof(StatusBadgeBg));
                OnPropertyChanged(nameof(StatusBadgeFg));
                OnPropertyChanged(nameof(StatusBadgeBorder));
                OnPropertyChanged(nameof(ToggleLockIcon));
                OnPropertyChanged(nameof(ToggleLockToolTip));
                OnPropertyChanged(nameof(ToggleLockForeground));
                OnPropertyChanged(nameof(ToggleLockBackground));
            }
        }
    }

    public string StatusText => IsActive ? "Hoạt động" : "Đã khóa";
    public string StatusBadgeBg => IsActive ? "#DCFCE7" : "#FEE2E2";
    public string StatusBadgeFg => IsActive ? "#15803D" : "#B91C1C";
    public string StatusBadgeBorder => IsActive ? "#BBF7D0" : "#FECACA";

    public DateTime CreatedAt => Account.CreatedAt;
    public string CreatedAtFormatted => Account.CreatedAt.ToString("dd/MM/yyyy");

    private DateTime? _lastLoginAt;
    public DateTime? LastLoginAt
    {
        get => _lastLoginAt;
        set
        {
            if (SetProperty(ref _lastLoginAt, value))
            {
                OnPropertyChanged(nameof(LastLoginFormatted));
            }
        }
    }
    public string LastLoginFormatted => LastLoginAt.HasValue
        ? LastLoginAt.Value.ToString("dd/MM/yyyy HH:mm")
        : "Chưa ghi nhận";

    public string AvatarPath
    {
        get
        {
            if (Account.Role == UserRole.Admin)
            {
                return "/Assets/resident_avatar.png";
            }
            if (!string.IsNullOrWhiteSpace(Account.Resident?.AvatarPath))
            {
                return Account.Resident.AvatarPath;
            }
            return "/Assets/resident_avatar.png";
        }
    }

    public bool CanToggle => !string.Equals(Username, "admin", StringComparison.OrdinalIgnoreCase);

    public string ToggleLockIcon => IsActive ? "🔒" : "🔓";
    public string ToggleLockToolTip => IsActive ? "Khóa tài khoản" : "Mở khóa tài khoản";
    public string ToggleLockForeground => IsActive ? "#DC2626" : "#16A34A";
    public string ToggleLockBackground => IsActive ? "#FEE2E2" : "#DCFCE7";

    public AccountDisplayItemViewModel(UserAccount account, int stt = 1)
    {
        Account = account;
        Stt = stt;
        _isActive = account.IsActive;
        _lastLoginAt = account.LastLoginAt;
    }
}
#endregion

public class AccountManagementViewModel : ViewModelBase
{
    private readonly AccountService _accountService;
    private readonly ResidentService _residentService;

    private List<AccountDisplayItemViewModel> _allAccounts = new();
    private ObservableCollection<AccountDisplayItemViewModel> _pagedAccounts = new();
    private AccountDisplayItemViewModel? _selectedAccount;
    private bool _isDetailPanelOpen;

    // Filters
    private string _searchText = string.Empty;
    private string _selectedRoleFilter = "Tất cả";
    private string _selectedStatusFilter = "Tất cả";

    // Pagination
    private int _currentPage = 1;
    private int _pageSize = 8;
    private int _totalItems;
    private int _totalPages = 1;
    private ObservableCollection<PaginationPageItem> _paginationPages = new();

    // Stats
    private int _totalAccountsCount;
    private int _activeAccountsCount;
    private int _lockedAccountsCount;

    // Loading / Messages
    private bool _isLoading;
    private string _errorMessage = string.Empty;
    private string _successMessage = string.Empty;

    // Reset Password Modal
    private bool _isResetPasswordDialogOpen;
    private AccountDisplayItemViewModel? _accountToReset;
    private string _newPasswordInput = string.Empty;
    private string _resetPasswordErrorMessage = string.Empty;

    // Create Account Modal
    private bool _isCreateAccountDialogOpen;
    private string _newUsernameInput = string.Empty;
    private string _newAccountPasswordInput = string.Empty;
    private UserRole _newAccountRole = UserRole.Resident;
    private Resident? _selectedResidentForNewAccount;
    private ObservableCollection<Resident> _availableResidentsWithoutAccount = new();
    private string _createAccountErrorMessage = string.Empty;

    #region Properties
    public ObservableCollection<AccountDisplayItemViewModel> PagedAccounts
    {
        get => _pagedAccounts;
        set => SetProperty(ref _pagedAccounts, value);
    }

    public AccountDisplayItemViewModel? SelectedAccount
    {
        get => _selectedAccount;
        set => SetProperty(ref _selectedAccount, value);
    }

    public bool IsDetailPanelOpen
    {
        get => _isDetailPanelOpen;
        set => SetProperty(ref _isDetailPanelOpen, value);
    }

    public int TotalAccountsCount
    {
        get => _totalAccountsCount;
        set => SetProperty(ref _totalAccountsCount, value);
    }

    public int ActiveAccountsCount
    {
        get => _activeAccountsCount;
        set => SetProperty(ref _activeAccountsCount, value);
    }

    public int LockedAccountsCount
    {
        get => _lockedAccountsCount;
        set => SetProperty(ref _lockedAccountsCount, value);
    }

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

    public string SelectedRoleFilter
    {
        get => _selectedRoleFilter;
        set
        {
            if (SetProperty(ref _selectedRoleFilter, value))
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

    public List<string> RoleFilterOptions { get; } = new() { "Tất cả", "Admin", "Cư dân" };
    public List<string> StatusFilterOptions { get; } = new() { "Tất cả", "Hoạt động", "Đã khóa" };
    public List<int> PageSizeOptions { get; } = new() { 8, 10, 20, 50 };

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
                if (!string.IsNullOrEmpty(value))
                {
                    _successMessage = string.Empty;
                    OnPropertyChanged(nameof(SuccessMessage));
                }
            }
        }
    }

    public string SuccessMessage
    {
        get => _successMessage;
        set
        {
            if (SetProperty(ref _successMessage, value))
            {
                if (!string.IsNullOrEmpty(value))
                {
                    _errorMessage = string.Empty;
                    OnPropertyChanged(nameof(ErrorMessage));
                }
            }
        }
    }

    // Reset Password Modal Properties
    public bool IsResetPasswordDialogOpen
    {
        get => _isResetPasswordDialogOpen;
        set => SetProperty(ref _isResetPasswordDialogOpen, value);
    }

    public AccountDisplayItemViewModel? AccountToReset
    {
        get => _accountToReset;
        set => SetProperty(ref _accountToReset, value);
    }

    public string NewPasswordInput
    {
        get => _newPasswordInput;
        set => SetProperty(ref _newPasswordInput, value);
    }

    public string ResetPasswordErrorMessage
    {
        get => _resetPasswordErrorMessage;
        set
        {
            if (SetProperty(ref _resetPasswordErrorMessage, value))
            {
                if (!string.IsNullOrEmpty(value))
                {
                    ErrorMessage = value;
                }
            }
        }
    }

    // Create Account Modal Properties
    public bool IsCreateAccountDialogOpen
    {
        get => _isCreateAccountDialogOpen;
        set => SetProperty(ref _isCreateAccountDialogOpen, value);
    }

    public string NewUsernameInput
    {
        get => _newUsernameInput;
        set => SetProperty(ref _newUsernameInput, value);
    }

    public string NewAccountPasswordInput
    {
        get => _newAccountPasswordInput;
        set => SetProperty(ref _newAccountPasswordInput, value);
    }

    public UserRole NewAccountRole
    {
        get => _newAccountRole;
        set
        {
            if (SetProperty(ref _newAccountRole, value))
            {
                OnPropertyChanged(nameof(IsCreatingResident));
            }
        }
    }

    public bool IsCreatingResident => NewAccountRole == UserRole.Resident;

    public Resident? SelectedResidentForNewAccount
    {
        get => _selectedResidentForNewAccount;
        set => SetProperty(ref _selectedResidentForNewAccount, value);
    }

    public ObservableCollection<Resident> AvailableResidentsWithoutAccount
    {
        get => _availableResidentsWithoutAccount;
        set => SetProperty(ref _availableResidentsWithoutAccount, value);
    }

    public string CreateAccountErrorMessage
    {
        get => _createAccountErrorMessage;
        set
        {
            if (SetProperty(ref _createAccountErrorMessage, value))
            {
                if (!string.IsNullOrEmpty(value))
                {
                    ErrorMessage = value;
                }
            }
        }
    }
    #endregion

    #region Commands
    public ICommand RefreshCommand { get; }
    public ICommand SelectAccountCommand { get; }
    public ICommand CloseDetailPanelCommand { get; }
    public ICommand ToggleAccountStatusCommand { get; }
    public ICommand NextPageCommand { get; }
    public ICommand PreviousPageCommand { get; }
    public ICommand GoToPageCommand { get; }

    public ICommand OpenResetPasswordDialogCommand { get; }
    public ICommand CloseResetPasswordDialogCommand { get; }
    public ICommand ConfirmResetPasswordCommand { get; }

    public ICommand OpenCreateAccountDialogCommand { get; }
    public ICommand CloseCreateAccountDialogCommand { get; }
    public ICommand ConfirmCreateAccountCommand { get; }
    #endregion

    public AccountManagementViewModel(AccountService accountService, ResidentService residentService)
    {
        _accountService = accountService;
        _residentService = residentService;

        RefreshCommand = new AsyncRelayCommand(async _ => await LoadDataAsync());

        SelectAccountCommand = new RelayCommand(p =>
        {
            if (p is AccountDisplayItemViewModel item)
            {
                SelectedAccount = item;
                IsDetailPanelOpen = true;
            }
        });

        CloseDetailPanelCommand = new RelayCommand(_ =>
        {
            IsDetailPanelOpen = false;
        });

        ToggleAccountStatusCommand = new AsyncRelayCommand(async p =>
        {
            var target = p as AccountDisplayItemViewModel ?? SelectedAccount;
            if (target != null)
            {
                await ToggleStatusAsync(target);
            }
        });

        NextPageCommand = new RelayCommand(_ =>
        {
            if (CurrentPage < TotalPages) CurrentPage++;
        });

        PreviousPageCommand = new RelayCommand(_ =>
        {
            if (CurrentPage > 1) CurrentPage--;
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

        OpenResetPasswordDialogCommand = new RelayCommand(p =>
        {
            var target = p as AccountDisplayItemViewModel ?? SelectedAccount;
            if (target != null)
            {
                AccountToReset = target;
                NewPasswordInput = string.Empty;
                ResetPasswordErrorMessage = string.Empty;
                IsResetPasswordDialogOpen = true;
            }
        });

        CloseResetPasswordDialogCommand = new RelayCommand(_ =>
        {
            IsResetPasswordDialogOpen = false;
            AccountToReset = null;
        });

        ConfirmResetPasswordCommand = new AsyncRelayCommand(async _ =>
        {
            await ConfirmResetPasswordAsync();
        });

        OpenCreateAccountDialogCommand = new AsyncRelayCommand(async _ =>
        {
            await OpenCreateAccountDialogAsync();
        });

        CloseCreateAccountDialogCommand = new RelayCommand(_ =>
        {
            IsCreateAccountDialogOpen = false;
        });

        ConfirmCreateAccountCommand = new AsyncRelayCommand(async _ =>
        {
            await ConfirmCreateAccountAsync();
        });

        _ = LoadDataAsync();
    }

    #region MouseWheel Paging
    public void SelectNextItemOrNextPage()
    {
        if (PagedAccounts.Count == 0) return;

        if (SelectedAccount == null)
        {
            SelectedAccount = PagedAccounts.FirstOrDefault();
            return;
        }

        int currentIndex = PagedAccounts.IndexOf(SelectedAccount);
        if (currentIndex >= 0 && currentIndex < PagedAccounts.Count - 1)
        {
            SelectedAccount = PagedAccounts[currentIndex + 1];
        }
        else if (currentIndex == PagedAccounts.Count - 1)
        {
            if (CurrentPage < TotalPages)
            {
                CurrentPage++;
                SelectedAccount = PagedAccounts.FirstOrDefault();
            }
        }
    }

    public void SelectPreviousItemOrPreviousPage()
    {
        if (PagedAccounts.Count == 0) return;

        if (SelectedAccount == null)
        {
            SelectedAccount = PagedAccounts.LastOrDefault();
            return;
        }

        int currentIndex = PagedAccounts.IndexOf(SelectedAccount);
        if (currentIndex > 0)
        {
            SelectedAccount = PagedAccounts[currentIndex - 1];
        }
        else if (currentIndex == 0)
        {
            if (CurrentPage > 1)
            {
                CurrentPage--;
                SelectedAccount = PagedAccounts.LastOrDefault();
            }
        }
    }
    #endregion

    #region Data Loading & Filtering
    public async Task LoadDataAsync()
    {
        IsLoading = true;
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;

        try
        {
            var accounts = await _accountService.GetAllAccountsWithResidentAsync();
            var list = accounts.ToList();

            TotalAccountsCount = list.Count;
            ActiveAccountsCount = list.Count(a => a.IsActive);
            LockedAccountsCount = list.Count(a => !a.IsActive);

            int stt = 1;
            _allAccounts = list.Select(a => new AccountDisplayItemViewModel(a, stt++)).ToList();

            ApplyFilters();

            if (SelectedAccount != null)
            {
                SelectedAccount = _allAccounts.FirstOrDefault(a => a.Id == SelectedAccount.Id)
                                  ?? PagedAccounts.FirstOrDefault();
            }
            else
            {
                SelectedAccount = PagedAccounts.FirstOrDefault();
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Lỗi khi tải danh sách tài khoản: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ApplyFilters()
    {
        var filtered = _allAccounts.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            string keyword = SearchText.Trim().ToLower();
            filtered = filtered.Where(a =>
                a.Username.ToLower().Contains(keyword) ||
                a.FullName.ToLower().Contains(keyword) ||
                a.ResidentCode.ToLower().Contains(keyword) ||
                a.AccountCode.ToLower().Contains(keyword));
        }

        if (SelectedRoleFilter == "Admin")
        {
            filtered = filtered.Where(a => a.Role == UserRole.Admin);
        }
        else if (SelectedRoleFilter == "Cư dân")
        {
            filtered = filtered.Where(a => a.Role == UserRole.Resident);
        }

        if (SelectedStatusFilter == "Hoạt động")
        {
            filtered = filtered.Where(a => a.IsActive);
        }
        else if (SelectedStatusFilter == "Đã khóa")
        {
            filtered = filtered.Where(a => !a.IsActive);
        }

        var resultList = filtered.ToList();
        TotalItems = resultList.Count;

        TotalPages = (int)Math.Ceiling((double)TotalItems / PageSize);
        if (TotalPages < 1) TotalPages = 1;
        if (CurrentPage > TotalPages) CurrentPage = TotalPages;
        if (CurrentPage < 1) CurrentPage = 1;

        int skip = (CurrentPage - 1) * PageSize;
        var pagedList = resultList.Skip(skip).Take(PageSize).ToList();

        // Cập nhật lại số thứ tự trang
        for (int i = 0; i < pagedList.Count; i++)
        {
            pagedList[i].Stt = skip + i + 1;
        }

        PagedAccounts.Clear();
        foreach (var item in pagedList)
        {
            PagedAccounts.Add(item);
        }

        UpdatePaginationNumbers();
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

        // Luôn có trang 1
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

        // Luôn có trang cuối
        PaginationPages.Add(new PaginationPageItem
        {
            PageNumber = TotalPages,
            DisplayText = TotalPages.ToString(),
            IsCurrent = (CurrentPage == TotalPages)
        });
    }
    #endregion

    #region Action Methods
    private async Task ToggleStatusAsync(AccountDisplayItemViewModel target)
    {
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;

        try
        {
            await _accountService.ToggleAccountStatusAsync(target.Id);
            target.IsActive = !target.IsActive;

            // Cập nhật lại số liệu thống kê
            ActiveAccountsCount = _allAccounts.Count(a => a.IsActive);
            LockedAccountsCount = _allAccounts.Count(a => !a.IsActive);

            SuccessMessage = target.IsActive
                ? $"Đã mở khóa tài khoản '{target.Username}' thành công!"
                : $"Đã khóa tài khoản '{target.Username}' thành công!";
        }
        catch (BusinessRuleException ex)
        {
            ErrorMessage = ex.Message;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Đã xảy ra lỗi: {ex.Message}";
        }
    }

    private async Task ConfirmResetPasswordAsync()
    {
        if (AccountToReset == null) return;

        ResetPasswordErrorMessage = string.Empty;
        if (string.IsNullOrWhiteSpace(NewPasswordInput) || NewPasswordInput.Length < 6)
        {
            ResetPasswordErrorMessage = "Mật khẩu mới phải có ít nhất 6 ký tự.";
            return;
        }

        try
        {
            await _accountService.ResetPasswordAsync(AccountToReset.Id, NewPasswordInput.Trim());
            IsResetPasswordDialogOpen = false;
            SuccessMessage = $"Đã đặt lại mật khẩu cho tài khoản '{AccountToReset.Username}' thành công!";
            AccountToReset = null;
        }
        catch (BusinessRuleException ex)
        {
            ResetPasswordErrorMessage = ex.Message;
        }
        catch (Exception ex)
        {
            ResetPasswordErrorMessage = $"Lỗi: {ex.Message}";
        }
    }

    private async Task OpenCreateAccountDialogAsync()
    {
        NewUsernameInput = string.Empty;
        NewAccountPasswordInput = "123456";
        NewAccountRole = UserRole.Resident;
        CreateAccountErrorMessage = string.Empty;
        SelectedResidentForNewAccount = null;

        try
        {
            var residents = await _residentService.GetAllResidentsAsync();
            var existingAccounts = await _accountService.GetAllAccountsWithResidentAsync();
            var assignedResidentIds = new HashSet<int>(existingAccounts.Where(a => a.ResidentId.HasValue).Select(a => a.ResidentId!.Value));

            var unassigned = residents.Where(r => !assignedResidentIds.Contains(r.Id)).OrderBy(r => r.FullName).ToList();
            AvailableResidentsWithoutAccount.Clear();
            foreach (var r in unassigned)
            {
                AvailableResidentsWithoutAccount.Add(r);
            }
            SelectedResidentForNewAccount = AvailableResidentsWithoutAccount.FirstOrDefault();

            IsCreateAccountDialogOpen = true;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Lỗi khi chuẩn bị tạo tài khoản: {ex.Message}";
        }
    }

    private async Task ConfirmCreateAccountAsync()
    {
        CreateAccountErrorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(NewUsernameInput))
        {
            CreateAccountErrorMessage = "Vui lòng nhập tên đăng nhập.";
            return;
        }

        if (string.IsNullOrWhiteSpace(NewAccountPasswordInput) || NewAccountPasswordInput.Length < 6)
        {
            CreateAccountErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự.";
            return;
        }

        int? residentId = null;
        if (NewAccountRole == UserRole.Resident)
        {
            if (SelectedResidentForNewAccount == null)
            {
                CreateAccountErrorMessage = "Vui lòng chọn cư dân liên kết với tài khoản này.";
                return;
            }
            residentId = SelectedResidentForNewAccount.Id;
        }

        try
        {
            await _accountService.CreateAccountAsync(
                NewUsernameInput.Trim(),
                NewAccountPasswordInput,
                NewAccountRole,
                residentId);

            IsCreateAccountDialogOpen = false;
            SuccessMessage = $"Đã tạo tài khoản '{NewUsernameInput.Trim()}' thành công!";
            await LoadDataAsync();
        }
        catch (BusinessRuleException ex)
        {
            CreateAccountErrorMessage = ex.Message;
        }
        catch (Exception ex)
        {
            CreateAccountErrorMessage = $"Lỗi: {ex.Message}";
        }
    }
    #endregion
}

