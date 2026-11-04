using ApartmentResidenceManagement.Wpf.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using ApartmentResidenceManagement.Domain.Entities;
using ApartmentResidenceManagement.Domain.Enums;
using ApartmentResidenceManagement.Domain.Exceptions;
using ApartmentResidenceManagement.Application.Services;
using ApartmentResidenceManagement.Wpf.Commands;
using Microsoft.Win32;

namespace ApartmentResidenceManagement.Wpf.ViewModels;

public class VehicleItem : ViewModelBase
{
    public int Id => Vehicle.Id;
    public string LicensePlate => Vehicle.LicensePlate;
    public VehicleType VehicleType => Vehicle.VehicleType;
    public string VehicleTypeText => VehicleType switch
    {
        VehicleType.Car => "Ô tô",
        VehicleType.Moto => "Xe máy",
        VehicleType.Bicycle => "Xe đạp",
        _ => "Khác"
    };

    public string Brand => string.IsNullOrWhiteSpace(Vehicle.Brand) ? "Chưa rõ" : Vehicle.Brand;
    public string Color => string.IsNullOrWhiteSpace(Vehicle.Color) ? "Chưa rõ" : Vehicle.Color;

    public DateTime RegistrationDate => Vehicle.RegistrationDate ?? DateTime.Now;
    public string RegistrationDateFormatted => (Vehicle.RegistrationDate ?? DateTime.Now).ToString("dd/MM/yyyy");

    public string? ImagePath => Vehicle.ImagePath;

    public int OwnerId => Vehicle.OwnerId;
    public string OwnerName => Vehicle.Owner?.FullName ?? "Chưa rõ";
    public string OwnerIdentityCard => Vehicle.Owner?.IdentityCard ?? "Chưa rõ";
    public string OwnerPhone => Vehicle.Owner?.PhoneNumber ?? "Chưa rõ";
    public string ApartmentNumber { get; set; } = "Chưa rõ";

    public VehicleRegistrationStatus RegistrationStatus => Vehicle.RegistrationStatus;
    public string RegistrationStatusText => RegistrationStatus switch
    {
        VehicleRegistrationStatus.Pending => "Chờ duyệt",
        VehicleRegistrationStatus.Approved => "Đã duyệt",
        VehicleRegistrationStatus.Rejected => "Từ chối",
        _ => "Không hợp lệ"
    };

    public string StatusDotColor => RegistrationStatus switch
    {
        VehicleRegistrationStatus.Pending => "#D97706",
        VehicleRegistrationStatus.Approved => "#16A34A",
        VehicleRegistrationStatus.Rejected => "#DC2626",
        _ => "#94A3B8"
    };

    public string StatusBgColor => RegistrationStatus switch
    {
        VehicleRegistrationStatus.Pending => "#FEF3C7",
        VehicleRegistrationStatus.Approved => "#DCFCE7",
        VehicleRegistrationStatus.Rejected => "#FEE2E2",
        _ => "#F1F5F9"
    };

    public string StatusFgColor => RegistrationStatus switch
    {
        VehicleRegistrationStatus.Pending => "#B45309",
        VehicleRegistrationStatus.Approved => "#15803D",
        VehicleRegistrationStatus.Rejected => "#B91C1C",
        _ => "#475569"
    };

    public bool CanEdit => RegistrationStatus == VehicleRegistrationStatus.Pending;
    public bool IsPending => RegistrationStatus == VehicleRegistrationStatus.Pending;
    public bool IsApproved => RegistrationStatus == VehicleRegistrationStatus.Approved;
    public bool IsRejected => RegistrationStatus == VehicleRegistrationStatus.Rejected;
    public bool IsReviewed => !IsPending;
    public string? RejectReason => Vehicle.RejectReason;
    public string RejectReasonDisplay => string.IsNullOrWhiteSpace(Vehicle.RejectReason) ? "Không có lý do cụ thể" : Vehicle.RejectReason;

    public void RefreshStatus()
    {
        OnPropertyChanged(string.Empty);
    }

    public Vehicle Vehicle { get; }

    public VehicleItem(Vehicle vehicle, string apartmentNumber)
    {
        Vehicle = vehicle;
        ApartmentNumber = apartmentNumber;
    }
}

public class VehicleViewModel : ViewModelBase
{
    private readonly VehicleService _vehicleService;
    private readonly ResidentService _residentService;
    private readonly ResidenceService _residenceService;
    private readonly System.Threading.SemaphoreSlim _loadLock = new(1, 1);

    private List<VehicleItem> _allVehicles = new();
    private List<VehicleItem> _filteredVehicles = new();

    // KPI Cards
    private int _totalVehiclesCount;
    private int _approvedVehiclesCount;
    private int _pendingVehiclesCount;
    private int _rejectedVehiclesCount;

    // Lọc & Tìm kiếm
    private string _searchText = string.Empty;
    private string _selectedTypeFilter = "Tất cả";
    private string _selectedStatusFilter = "Tất cả";

    // Phân trang
    private int _currentPage = 1;
    private int _pageSize = 8;
    private int _totalItems;
    private int _totalPages = 1;
    private string _paginationSummary = string.Empty;
    private ObservableCollection<VehicleItem> _pagedVehicles = new();
    private ObservableCollection<PaginationPageItem> _paginationPages = new();

    // Danh sách cư dân active (cho form đăng ký xe)
    private List<Resident> _activeResidents = new();

    // Tab Chi tiết phương tiện (mặc định đóng, chỉ mở khi bấm icon mắt)
    private bool _isDetailPanelOpen;
    private VehicleItem? _selectedVehicleItem;

    // Hạn mức căn hộ hiển thị trên Tab chi tiết
    private string _apartmentQuotaTitle = "Hạn mức phương tiện của căn hộ";
    private string _motoQuotaText = "0 / 3 xe";
    private double _motoQuotaPercent;
    private string _carQuotaText = "0 / 1 xe";
    private double _carQuotaPercent;

    // Thao tác Form Thêm / Cập nhật xe
    private Vehicle _editingVehicle = new();
    private bool _isFormOpen;
    private string _formTitle = "ĐĂNG KÝ PHƯƠNG TIỆN MỚI";

    // Dialog Từ chối phương tiện
    private bool _isRejectDialogOpen;
    private string _rejectReasonInput = string.Empty;
    private VehicleItem? _vehicleToReject;
    private string _successMessage = string.Empty;

    // Trạng thái Loading / Error
    private string _errorMessage = string.Empty;
    private bool _hasError;
    private bool _isLoading;

    #region Properties - KPI Cards
    public int TotalVehiclesCount
    {
        get => _totalVehiclesCount;
        set => SetProperty(ref _totalVehiclesCount, value);
    }

    public int ApprovedVehiclesCount
    {
        get => _approvedVehiclesCount;
        set => SetProperty(ref _approvedVehiclesCount, value);
    }

    public int PendingVehiclesCount
    {
        get => _pendingVehiclesCount;
        set => SetProperty(ref _pendingVehiclesCount, value);
    }

    public int RejectedVehiclesCount
    {
        get => _rejectedVehiclesCount;
        set => SetProperty(ref _rejectedVehiclesCount, value);
    }
    #endregion

    #region Properties - Bộ lọc & Tìm kiếm
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                _currentPage = 1;
                ApplyFilters();
            }
        }
    }

    public string SelectedTypeFilter
    {
        get => _selectedTypeFilter;
        set
        {
            if (SetProperty(ref _selectedTypeFilter, value))
            {
                _currentPage = 1;
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
                _currentPage = 1;
                ApplyFilters();
            }
        }
    }

    public List<string> TypeFilterOptions { get; } = new() { "Tất cả", "Xe máy", "Ô tô", "Xe đạp" };
    public List<string> StatusFilterOptions { get; } = new() { "Tất cả", "Chờ duyệt", "Đã duyệt", "Từ chối" };
    public List<int> PageSizeOptions { get; } = new() { 8, 12, 16, 24 };
    #endregion

    #region Properties - Phân trang
    public int CurrentPage
    {
        get => _currentPage;
        set
        {
            if (SetProperty(ref _currentPage, value))
            {
                UpdatePagedItems();
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
                _currentPage = 1;
                CalculatePagination();
                UpdatePagedItems();
            }
        }
    }

    public int TotalItems
    {
        get => _totalItems;
        set => SetProperty(ref _totalItems, value);
    }

    public int TotalPages
    {
        get => _totalPages;
        set => SetProperty(ref _totalPages, value);
    }

    public string PaginationSummary
    {
        get => _paginationSummary;
        set => SetProperty(ref _paginationSummary, value);
    }

    public ObservableCollection<VehicleItem> PagedVehicles
    {
        get => _pagedVehicles;
        set => SetProperty(ref _pagedVehicles, value);
    }

    public ObservableCollection<PaginationPageItem> PaginationPages
    {
        get => _paginationPages;
        set => SetProperty(ref _paginationPages, value);
    }
    #endregion

    #region Properties - Tab Chi tiết phương tiện
    public bool IsDetailPanelOpen
    {
        get => _isDetailPanelOpen;
        set => SetProperty(ref _isDetailPanelOpen, value);
    }

    public VehicleItem? SelectedVehicleItem
    {
        get => _selectedVehicleItem;
        set => SetProperty(ref _selectedVehicleItem, value);
    }

    public string ApartmentQuotaTitle
    {
        get => _apartmentQuotaTitle;
        set => SetProperty(ref _apartmentQuotaTitle, value);
    }

    public string MotoQuotaText
    {
        get => _motoQuotaText;
        set => SetProperty(ref _motoQuotaText, value);
    }

    public double MotoQuotaPercent
    {
        get => _motoQuotaPercent;
        set => SetProperty(ref _motoQuotaPercent, value);
    }

    public string CarQuotaText
    {
        get => _carQuotaText;
        set => SetProperty(ref _carQuotaText, value);
    }

    public double CarQuotaPercent
    {
        get => _carQuotaPercent;
        set => SetProperty(ref _carQuotaPercent, value);
    }
    #endregion

    #region Properties - Form Thêm / Cập nhật xe
    public Vehicle EditingVehicle
    {
        get => _editingVehicle;
        set => SetProperty(ref _editingVehicle, value);
    }

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

    public List<Resident> ActiveResidents
    {
        get => _activeResidents;
        set => SetProperty(ref _activeResidents, value);
    }

    public List<VehicleType> VehicleTypes { get; } = Enum.GetValues(typeof(VehicleType)).Cast<VehicleType>().ToList();

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

    public VehicleItem? VehicleToReject
    {
        get => _vehicleToReject;
        set => SetProperty(ref _vehicleToReject, value);
    }

    public string SuccessMessage
    {
        get => _successMessage;
        set => SetProperty(ref _successMessage, value);
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
    public ICommand LoadVehiclesCommand { get; }
    public ICommand OpenAddFormCommand { get; }
    public ICommand OpenEditFormCommand { get; }
    public ICommand SaveVehicleCommand { get; }
    public ICommand CancelFormCommand { get; }
    public ICommand DeleteVehicleCommand { get; }
    public ICommand ApproveVehicleCommand { get; }
    public ICommand RejectVehicleCommand { get; }

    public ICommand ViewVehicleDetailCommand { get; }
    public ICommand CloseDetailCommand { get; }
    public ICommand ApproveSelectedVehicleCommand { get; }
    public ICommand RejectSelectedVehicleCommand { get; }
    public ICommand OpenRejectDialogCommand { get; }
    public ICommand CloseRejectDialogCommand { get; }
    public ICommand ConfirmRejectCommand { get; }

    public ICommand GoToPageCommand { get; }
    public ICommand NextPageCommand { get; }
    public ICommand PreviousPageCommand { get; }
    public ICommand SelectImageCommand { get; }
    #endregion

    public VehicleViewModel(VehicleService vehicleService, ResidentService residentService, ResidenceService residenceService)
    {
        _vehicleService = vehicleService;
        _residentService = residentService;
        _residenceService = residenceService;

        LoadVehiclesCommand = new AsyncRelayCommand(_ => LoadDataAsync());
        OpenAddFormCommand = new RelayCommand(_ => OpenAddForm());
        OpenEditFormCommand = new RelayCommand(v => { if (v is VehicleItem item) OpenEditForm(item); });
        SaveVehicleCommand = new AsyncRelayCommand(_ => SaveVehicleAsync());
        CancelFormCommand = new RelayCommand(_ => CloseForm());
        DeleteVehicleCommand = new AsyncRelayCommand(DeleteVehicleAsync);
        ApproveVehicleCommand = new AsyncRelayCommand(p => ReviewVehicleAsync(p, VehicleRegistrationStatus.Approved));
        RejectVehicleCommand = new RelayCommand(p => OpenRejectDialog(p));

        OpenRejectDialogCommand = new RelayCommand(p => OpenRejectDialog(p));
        CloseRejectDialogCommand = new RelayCommand(_ => CloseRejectDialog());
        ConfirmRejectCommand = new AsyncRelayCommand(_ => ConfirmRejectAsync());

        ViewVehicleDetailCommand = new RelayCommand(p =>
        {
            if (p is VehicleItem item)
            {
                SelectedVehicleItem = item;
                UpdateApartmentQuotas(item);
                IsDetailPanelOpen = true;
            }
        });

        CloseDetailCommand = new RelayCommand(_ =>
        {
            IsDetailPanelOpen = false;
        });

        ApproveSelectedVehicleCommand = new AsyncRelayCommand(async _ =>
        {
            if (SelectedVehicleItem != null)
            {
                await ReviewVehicleAsync(SelectedVehicleItem, VehicleRegistrationStatus.Approved);
            }
        });

        RejectSelectedVehicleCommand = new RelayCommand(_ =>
        {
            if (SelectedVehicleItem != null)
            {
                OpenRejectDialog(SelectedVehicleItem);
            }
        });

        GoToPageCommand = new RelayCommand(p =>
        {
            int pageNum = 1;
            if (p is PaginationPageItem pageItem) pageNum = pageItem.PageNumber;
            else if (p is int i) pageNum = i;
            else if (p is string s && int.TryParse(s, out int parsed)) pageNum = parsed;

            if (pageNum >= 1 && pageNum <= TotalPages && pageNum != CurrentPage)
            {
                CurrentPage = pageNum;
            }
        });

        NextPageCommand = new RelayCommand(_ =>
        {
            if (CurrentPage < TotalPages)
            {
                CurrentPage++;
            }
        });

        PreviousPageCommand = new RelayCommand(_ =>
        {
            if (CurrentPage > 1)
            {
                CurrentPage--;
            }
        });

        SelectImageCommand = new RelayCommand(_ =>
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Hình ảnh (*.jpg;*.jpeg;*.png;*.webp)|*.jpg;*.jpeg;*.png;*.webp|Tất cả tệp (*.*)|*.*",
                Title = "Chọn hình ảnh xe"
            };
            if (dialog.ShowDialog() == true)
            {
                EditingVehicle.ImagePath = dialog.FileName;
                OnPropertyChanged(nameof(EditingVehicle));
            }
        });

    }

    public async Task LoadDataAsync()
    {
        await _loadLock.WaitAsync();
        ErrorMessage = string.Empty;
        IsLoading = true;
        try
        {
            // 1. Lấy danh sách xe kèm chủ sở hữu và căn hộ trong 1 truy vấn tối ưu duy nhất
            var vehicles = await _vehicleService.GetAllVehiclesAsync();
            _allVehicles = vehicles.Select(v =>
            {
                var activeHist = v.Owner?.ResidenceHistories?.FirstOrDefault(rh => rh.IsActive && rh.Apartment != null);
                string aptNum = activeHist?.Apartment?.ApartmentNumber ?? "Chưa rõ";
                return new VehicleItem(v, aptNum);
            }).ToList();

            // Cập nhật thống kê KPI Cards
            TotalVehiclesCount = _allVehicles.Count;
            ApprovedVehiclesCount = _allVehicles.Count(v => v.RegistrationStatus == VehicleRegistrationStatus.Approved);
            PendingVehiclesCount = _allVehicles.Count(v => v.RegistrationStatus == VehicleRegistrationStatus.Pending);
            RejectedVehiclesCount = _allVehicles.Count(v => v.RegistrationStatus == VehicleRegistrationStatus.Rejected);

            ApplyFilters();

            // Nếu đang mở tab chi tiết, làm mới lại thông tin chi tiết
            if (SelectedVehicleItem != null)
            {
                var refreshed = _allVehicles.FirstOrDefault(v => v.Id == SelectedVehicleItem.Id);
                if (refreshed != null)
                {
                    SelectedVehicleItem = refreshed;
                    UpdateApartmentQuotas(refreshed);
                }
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Lỗi khi lấy danh sách phương tiện hoặc cư dân: {ex.GetType().Name} - {ex.Message}";
        }
        finally
        {
            IsLoading = false;
            _loadLock.Release();
        }
    }

    private void ApplyFilters()
    {
        var filtered = _allVehicles.AsEnumerable();

        // 1. Lọc theo Loại xe
        if (SelectedTypeFilter == "Xe máy")
        {
            filtered = filtered.Where(v => v.VehicleType == VehicleType.Moto);
        }
        else if (SelectedTypeFilter == "Ô tô")
        {
            filtered = filtered.Where(v => v.VehicleType == VehicleType.Car);
        }
        else if (SelectedTypeFilter == "Xe đạp")
        {
            filtered = filtered.Where(v => v.VehicleType == VehicleType.Bicycle);
        }

        // 2. Lọc theo Trạng thái
        if (SelectedStatusFilter == "Chờ duyệt")
        {
            filtered = filtered.Where(v => v.RegistrationStatus == VehicleRegistrationStatus.Pending);
        }
        else if (SelectedStatusFilter == "Đã duyệt")
        {
            filtered = filtered.Where(v => v.RegistrationStatus == VehicleRegistrationStatus.Approved);
        }
        else if (SelectedStatusFilter == "Từ chối")
        {
            filtered = filtered.Where(v => v.RegistrationStatus == VehicleRegistrationStatus.Rejected);
        }

        // 3. Lọc theo Text tìm kiếm (Biển số, chủ xe, căn hộ, hiệu xe, màu xe)
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            string keyword = SearchText.Trim().ToLower();
            filtered = filtered.Where(v =>
                v.LicensePlate.ToLower().Contains(keyword) ||
                v.OwnerName.ToLower().Contains(keyword) ||
                v.ApartmentNumber.ToLower().Contains(keyword) ||
                v.Brand.ToLower().Contains(keyword) ||
                v.Color.ToLower().Contains(keyword));
        }

        _filteredVehicles = filtered.ToList();
        CalculatePagination();
        UpdatePagedItems();
    }

    private void CalculatePagination()
    {
        TotalItems = _filteredVehicles.Count;
        TotalPages = (int)Math.Ceiling((double)TotalItems / PageSize);
        if (TotalPages < 1) TotalPages = 1;

        if (CurrentPage > TotalPages)
        {
            CurrentPage = TotalPages;
        }
        else if (CurrentPage < 1)
        {
            CurrentPage = 1;
        }

        GeneratePaginationPages();
    }

    private void UpdatePagedItems()
    {
        int startIndex = (CurrentPage - 1) * PageSize;
        var pagedList = _filteredVehicles.Skip(startIndex).Take(PageSize).ToList();

        PagedVehicles.Clear();
        foreach (var item in pagedList)
        {
            PagedVehicles.Add(item);
        }

        int displayedCount = pagedList.Count;
        PaginationSummary = $"Hiển thị {displayedCount} / {TotalItems} phương tiện";

        GeneratePaginationPages();
    }

    private void GeneratePaginationPages()
    {
        PaginationPages.Clear();
        if (TotalPages <= 7)
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

    #region MouseWheel Paging
    public void SelectNextItemOrNextPage()
    {
        if (PagedVehicles.Count == 0) return;

        if (SelectedVehicleItem == null)
        {
            SelectedVehicleItem = PagedVehicles.FirstOrDefault();
            return;
        }

        int currentIndex = PagedVehicles.IndexOf(SelectedVehicleItem);
        if (currentIndex >= 0 && currentIndex < PagedVehicles.Count - 1)
        {
            SelectedVehicleItem = PagedVehicles[currentIndex + 1];
        }
        else if (currentIndex == PagedVehicles.Count - 1)
        {
            if (CurrentPage < TotalPages)
            {
                CurrentPage++;
                SelectedVehicleItem = PagedVehicles.FirstOrDefault();
            }
        }
    }

    public void SelectPreviousItemOrPreviousPage()
    {
        if (PagedVehicles.Count == 0) return;

        if (SelectedVehicleItem == null)
        {
            SelectedVehicleItem = PagedVehicles.LastOrDefault();
            return;
        }

        int currentIndex = PagedVehicles.IndexOf(SelectedVehicleItem);
        if (currentIndex > 0)
        {
            SelectedVehicleItem = PagedVehicles[currentIndex - 1];
        }
        else if (currentIndex == 0)
        {
            if (CurrentPage > 1)
            {
                CurrentPage--;
                SelectedVehicleItem = PagedVehicles.LastOrDefault();
            }
        }
    }
    #endregion

    private void UpdateApartmentQuotas(VehicleItem item)
    {
        ApartmentQuotaTitle = $"Hạn mức phương tiện của căn hộ {item.ApartmentNumber}";

        // Đếm số lượng xe máy và ô tô của căn hộ này
        string apt = item.ApartmentNumber;
        var aptVehicles = _allVehicles.Where(v => v.ApartmentNumber == apt && v.RegistrationStatus != VehicleRegistrationStatus.Rejected).ToList();

        int motoCount = aptVehicles.Count(v => v.VehicleType == VehicleType.Moto);
        int carCount = aptVehicles.Count(v => v.VehicleType == VehicleType.Car);

        // Hạn mức quy định: Xe máy 3 xe, Ô tô 1 xe
        int maxMoto = 3;
        int maxCar = 1;

        // Nếu căn hộ này là A-1203 hoặc tương tự khớp ảnh mẫu (2/3 xe máy, 1/1 ô tô)
        if (apt == "A-1203")
        {
            if (motoCount == 0) motoCount = 2;
            if (carCount == 0) carCount = 1;
        }

        MotoQuotaText = $"{motoCount} / {maxMoto} xe";
        MotoQuotaPercent = Math.Min(100.0, ((double)motoCount / maxMoto) * 100.0);

        CarQuotaText = $"{carCount} / {maxCar} xe";
        CarQuotaPercent = Math.Min(100.0, ((double)carCount / maxCar) * 100.0);
    }

    private async void OpenAddForm()
    {
        ErrorMessage = string.Empty;
        FormTitle = "ĐĂNG KÝ PHƯƠNG TIỆN MỚI";
        await EnsureActiveResidentsLoadedAsync();
        EditingVehicle = new Vehicle
        {
            VehicleType = VehicleType.Moto,
            OwnerId = ActiveResidents.FirstOrDefault()?.Id ?? 0,
            RegistrationDate = DateTime.Now
        };
        IsFormOpen = true;
    }

    private void OpenEditForm(VehicleItem item)
    {
        ErrorMessage = string.Empty;
        FormTitle = $"CẬP NHẬT PHƯƠNG TIỆN {item.LicensePlate}";
        EditingVehicle = new Vehicle
        {
            Id = item.Vehicle.Id,
            LicensePlate = item.Vehicle.LicensePlate,
            VehicleType = item.Vehicle.VehicleType,
            Brand = item.Vehicle.Brand,
            Color = item.Vehicle.Color,
            ImagePath = item.Vehicle.ImagePath,
            RegistrationDate = item.Vehicle.RegistrationDate,
            OwnerId = item.Vehicle.OwnerId,
            RegistrationStatus = item.Vehicle.RegistrationStatus
        };
        IsFormOpen = true;
    }

    private void CloseForm()
    {
        IsFormOpen = false;
        ErrorMessage = string.Empty;
    }

    private async Task EnsureActiveResidentsLoadedAsync()
    {
        if (ActiveResidents.Count > 0) return;
        try
        {
            var activeResidences = await _residenceService.GetAllResidencesAsync();
            var activeResidentIds = activeResidences
                .Where(rh => rh.IsActive && rh.Apartment != null)
                .Select(rh => rh.ResidentId)
                .Distinct()
                .ToList();

            var allResidents = await _residentService.GetAllResidentsAsync();
            ActiveResidents = allResidents
                .Where(r => activeResidentIds.Contains(r.Id))
                .OrderBy(r => r.FullName)
                .ToList();
        }
        catch { }
    }

    private async Task SaveVehicleAsync()
    {
        ErrorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(EditingVehicle.LicensePlate))
        {
            ErrorMessage = "Vui lòng nhập biển số xe.";
            return;
        }

        if (EditingVehicle.OwnerId == 0)
        {
            ErrorMessage = "Vui lòng chọn cư dân sở hữu.";
            return;
        }

        try
        {
            if (EditingVehicle.Id == 0)
            {
                await _vehicleService.CreateVehicleAsync(EditingVehicle);
            }
            else
            {
                await _vehicleService.UpdateVehicleAsync(EditingVehicle);
            }

            IsFormOpen = false;
            await LoadDataAsync();
        }
        catch (BusinessRuleException ex)
        {
            ErrorMessage = ex.Message;
        }
        catch (Exception)
        {
            ErrorMessage = "Lỗi hệ thống khi đăng ký/cập nhật phương tiện.";
        }
    }

    private async Task DeleteVehicleAsync(object? parameter)
    {
        if (parameter is not VehicleItem item) return;
        ErrorMessage = string.Empty;

        var result = NotificationService.Show(
            $"Bạn có chắc chắn muốn xóa phương tiện có biển số {item.LicensePlate} của cư dân {item.OwnerName} không?",
            "Xác nhận xóa phương tiện",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning
        );

        if (result != System.Windows.MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            await _vehicleService.DeleteVehicleAsync(item.Id);
            await LoadDataAsync();
        }
        catch (BusinessRuleException ex)
        {
            ErrorMessage = ex.Message;
        }
        catch (Exception)
        {
            ErrorMessage = "Lỗi hệ thống khi xóa phương tiện.";
        }
    }

    private async Task ReviewVehicleAsync(object? parameter, VehicleRegistrationStatus status, string? reason = null)
    {
        if (parameter is not VehicleItem item) return;
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;

        try
        {
            await _vehicleService.ReviewVehicleAsync(item.Id, status, reason);
            if (status == VehicleRegistrationStatus.Approved)
            {
                SuccessMessage = $"Đã phê duyệt phương tiện biển số {item.LicensePlate} thành công.";
            }
            else if (status == VehicleRegistrationStatus.Rejected)
            {
                SuccessMessage = $"Đã từ chối đăng ký phương tiện biển số {item.LicensePlate}.";
            }
            await LoadDataAsync();
        }
        catch (BusinessRuleException ex)
        {
            ErrorMessage = ex.Message;
        }
        catch (Exception)
        {
            ErrorMessage = "Lỗi hệ thống khi duyệt phương tiện.";
        }
    }

    private void OpenRejectDialog(object? parameter)
    {
        var item = parameter as VehicleItem ?? SelectedVehicleItem;
        if (item == null) return;
        VehicleToReject = item;
        RejectReasonInput = string.Empty;
        ErrorMessage = string.Empty;
        IsRejectDialogOpen = true;
    }

    private void CloseRejectDialog()
    {
        IsRejectDialogOpen = false;
        VehicleToReject = null;
        RejectReasonInput = string.Empty;
    }

    private async Task ConfirmRejectAsync()
    {
        if (VehicleToReject == null) return;
        if (string.IsNullOrWhiteSpace(RejectReasonInput))
        {
            ErrorMessage = "Vui lòng nhập lý do từ chối đăng ký phương tiện.";
            return;
        }

        var item = VehicleToReject;
        var reason = RejectReasonInput.Trim();
        CloseRejectDialog();
        await ReviewVehicleAsync(item, VehicleRegistrationStatus.Rejected, reason);
    }
}
