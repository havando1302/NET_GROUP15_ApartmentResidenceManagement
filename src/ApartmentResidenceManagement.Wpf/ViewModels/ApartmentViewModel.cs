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

namespace ApartmentResidenceManagement.Wpf.ViewModels;

public class ResidentMemberItem
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Relationship { get; set; } = string.Empty;
}

public class ApartmentDisplayItem
{
    public int Id { get; set; }
    public string ApartmentNumber { get; set; } = string.Empty;
    public int Floor { get; set; }
    public double Area { get; set; }
    public ApartmentStatus Status { get; set; }
    public int ResidentCount { get; set; }
    public List<ResidentMemberItem> Residents { get; set; } = new();
    public DateTime? LastUpdated { get; set; }
    public Apartment RawApartment { get; set; } = null!;
}

public class ApartmentViewModel : ViewModelBase
{
    private readonly ApartmentService _apartmentService;
    private readonly ResidenceService? _residenceService;
    private List<Apartment> _allApartments = new();
    private List<ApartmentDisplayItem> _allDisplayItems = new();
    private List<ApartmentDisplayItem> _filteredDisplayItems = new();

    // KPI Stat properties
    private int _totalApartmentsCount;
    private int _occupiedApartmentsCount;
    private int _emptyApartmentsCount;
    private int _totalFloorsCount;

    // Search và Filter properties
    private string _searchText = string.Empty;
    private ApartmentStatus? _selectedStatusFilter;
    private int? _selectedFloorFilter;
    private ObservableCollection<int?> _availableFloors = new();

    // DataGrid & Details properties
    private ObservableCollection<ApartmentDisplayItem> _pagedApartments = new();
    private ApartmentDisplayItem? _selectedApartmentItem;
    private Apartment _editingApartment = new();
    private bool _isFormOpen;
    private string _formTitle = "THÊM CĂN HỘ MỚI";
    private string _errorMessage = string.Empty;
    private bool _hasError;
    private bool _isLoading;

    // Pagination properties
    private int _currentPage = 1;
    private int _pageSize = 8;
    private int _totalPages = 1;
    private ObservableCollection<int> _pageNumbers = new();
    public List<int> PageSizeOptions { get; } = new() { 5, 8, 10, 15, 20 };

    #region Binding Properties
    public int TotalApartmentsCount
    {
        get => _totalApartmentsCount;
        set => SetProperty(ref _totalApartmentsCount, value);
    }

    public int OccupiedApartmentsCount
    {
        get => _occupiedApartmentsCount;
        set => SetProperty(ref _occupiedApartmentsCount, value);
    }

    public int EmptyApartmentsCount
    {
        get => _emptyApartmentsCount;
        set => SetProperty(ref _emptyApartmentsCount, value);
    }

    public int TotalFloorsCount
    {
        get => _totalFloorsCount;
        set => SetProperty(ref _totalFloorsCount, value);
    }

    public ObservableCollection<ApartmentDisplayItem> PagedApartments
    {
        get => _pagedApartments;
        set => SetProperty(ref _pagedApartments, value);
    }

    // Giữ thuộc tính Apartments để tương thích nếu cần
    public ObservableCollection<ApartmentDisplayItem> Apartments => PagedApartments;

    public ObservableCollection<int?> AvailableFloors
    {
        get => _availableFloors;
        set => SetProperty(ref _availableFloors, value);
    }

    public ApartmentDisplayItem? SelectedApartmentItem
    {
        get => _selectedApartmentItem;
        set => SetProperty(ref _selectedApartmentItem, value);
    }

    // Thuộc tính tương thích cho SelectedApartment
    public object? SelectedApartment
    {
        get => _selectedApartmentItem;
        set
        {
            if (value is ApartmentDisplayItem displayItem)
            {
                SelectedApartmentItem = displayItem;
            }
            else if (value is Apartment rawApt)
            {
                SelectedApartmentItem = _allDisplayItems.FirstOrDefault(d => d.Id == rawApt.Id);
            }
            else
            {
                SelectedApartmentItem = null;
            }
        }
    }

    public Apartment EditingApartment
    {
        get => _editingApartment;
        set => SetProperty(ref _editingApartment, value);
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

    public ApartmentStatus? SelectedStatusFilter
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

    public int? SelectedFloorFilter
    {
        get => _selectedFloorFilter;
        set
        {
            if (SetProperty(ref _selectedFloorFilter, value))
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

    public int TotalFilteredCount => _filteredDisplayItems.Count;

    public string PaginationSummary => $"Hiển thị {PagedApartments.Count} / {TotalApartmentsCount} căn hộ";

    public ObservableCollection<int> PageNumbers
    {
        get => _pageNumbers;
        set => SetProperty(ref _pageNumbers, value);
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

    public List<ApartmentStatus> StatusOptions { get; } = Enum.GetValues(typeof(ApartmentStatus)).Cast<ApartmentStatus>().ToList();

    public List<ApartmentStatus?> StatusFilterOptions { get; } = new List<ApartmentStatus?> { null }
        .Concat(Enum.GetValues(typeof(ApartmentStatus)).Cast<ApartmentStatus>().Select(s => (ApartmentStatus?)s))
        .ToList();
    #endregion

    #region Commands
    public ICommand LoadApartmentsCommand { get; }
    public ICommand OpenAddFormCommand { get; }
    public ICommand OpenEditFormCommand { get; }
    public ICommand SaveApartmentCommand { get; }
    public ICommand CancelFormCommand { get; }
    public ICommand DeleteApartmentCommand { get; }
    public ICommand SelectApartmentCommand { get; }
    public ICommand NextPageCommand { get; }
    public ICommand PreviousPageCommand { get; }
    public ICommand GoToPageCommand { get; }
    #endregion

    public ApartmentViewModel(ApartmentService apartmentService, ResidenceService? residenceService = null)
    {
        _apartmentService = apartmentService;
        _residenceService = residenceService;

        LoadApartmentsCommand = new AsyncRelayCommand(_ => LoadDataAsync());
        OpenAddFormCommand = new RelayCommand(_ => OpenAddForm());
        OpenEditFormCommand = new RelayCommand(p =>
        {
            if (p is ApartmentDisplayItem displayItem)
                OpenEditForm(displayItem);
            else if (p is Apartment apt)
                OpenEditForm(apt);
        });
        SaveApartmentCommand = new AsyncRelayCommand(_ => SaveDataAsync());
        CancelFormCommand = new RelayCommand(_ => CloseForm());
        DeleteApartmentCommand = new AsyncRelayCommand(DeleteDataAsync);
        SelectApartmentCommand = new RelayCommand(p =>
        {
            if (p is ApartmentDisplayItem displayItem)
                SelectedApartmentItem = displayItem;
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
            if (p is int page && page >= 1 && page <= TotalPages)
                CurrentPage = page;
        });

        _selectedStatusFilter = null;
        _selectedFloorFilter = null;
    }

    public async Task LoadDataAsync()
    {
        ErrorMessage = string.Empty;
        IsLoading = true;
        try
        {
            var list = await _apartmentService.GetAllApartmentsAsync();
            _allApartments = list.ToList();

            // Load active residences map nếu có ResidenceService
            Dictionary<int, List<ResidentMemberItem>> residentMap = new();
            if (_residenceService != null)
            {
                foreach (var apt in _allApartments)
                {
                    try
                    {
                        var activeResidences = await _residenceService.GetActiveResidencesByApartmentIdAsync(apt.Id);
                        var members = activeResidences
                            .Where(r => r.Resident != null)
                            .Select(r => new ResidentMemberItem
                            {
                                Id = r.ResidentId,
                                FullName = r.Resident?.FullName ?? "Chưa rõ",
                                PhoneNumber = r.Resident?.PhoneNumber ?? "",
                                Relationship = FormatRelationship(r.RelationshipType)
                            }).ToList();
                        residentMap[apt.Id] = members;
                    }
                    catch
                    {
                        residentMap[apt.Id] = new List<ResidentMemberItem>();
                    }
                }
            }

            // Chuyển đổi thành ApartmentDisplayItem
            _allDisplayItems = _allApartments.Select(a =>
            {
                residentMap.TryGetValue(a.Id, out var residents);
                var resList = residents ?? new List<ResidentMemberItem>();
                return new ApartmentDisplayItem
                {
                    Id = a.Id,
                    ApartmentNumber = a.ApartmentNumber,
                    Floor = a.Floor,
                    Area = a.Area,
                    Status = a.Status,
                    ResidentCount = resList.Count,
                    Residents = resList,
                    LastUpdated = DateTime.Today,
                    RawApartment = a
                };
            }).ToList();

            // Cập nhật thống kê KPI
            TotalApartmentsCount = _allApartments.Count;
            OccupiedApartmentsCount = _allApartments.Count(a => a.Status == ApartmentStatus.Occupied);
            EmptyApartmentsCount = _allApartments.Count(a => a.Status == ApartmentStatus.Empty);
            TotalFloorsCount = _allApartments.Select(a => a.Floor).Distinct().Count();

            // Cập nhật danh sách Tầng khả dụng để lọc
            var floors = _allApartments.Select(a => a.Floor).Distinct().OrderBy(f => f).ToList();
            AvailableFloors.Clear();
            AvailableFloors.Add(null); // Tất cả các tầng
            foreach (var floor in floors)
            {
                AvailableFloors.Add(floor);
            }

            ApplyFilters();
        }
        catch (Exception)
        {
            ErrorMessage = "Lỗi khi lấy dữ liệu căn hộ từ server.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private static string FormatRelationship(RelationshipType rel)
    {
        return rel switch
        {
            RelationshipType.Owner => "Chủ hộ",
            RelationshipType.FamilyMember => "Thành viên gia đình",
            RelationshipType.Tenant => "Khách thuê",
            RelationshipType.Temporary => "Tạm trú",
            _ => "Cư dân"
        };
    }

    private void ApplyFilters()
    {
        var filtered = _allDisplayItems.AsEnumerable();

        // 1. Lọc theo search text (mã căn hoặc tầng)
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var kw = SearchText.Trim();
            filtered = filtered.Where(a => 
                a.ApartmentNumber.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                a.Floor.ToString().Equals(kw, StringComparison.OrdinalIgnoreCase));
        }

        // 2. Lọc theo trạng thái
        if (SelectedStatusFilter.HasValue)
        {
            filtered = filtered.Where(a => a.Status == SelectedStatusFilter.Value);
        }

        // 3. Lọc theo tầng
        if (SelectedFloorFilter.HasValue)
        {
            filtered = filtered.Where(a => a.Floor == SelectedFloorFilter.Value);
        }

        _filteredDisplayItems = filtered.OrderBy(a => a.ApartmentNumber).ToList();

        // Tính toán phân trang
        TotalPages = Math.Max(1, (int)Math.Ceiling((double)_filteredDisplayItems.Count / PageSize));
        if (CurrentPage > TotalPages) CurrentPage = TotalPages;
        if (CurrentPage < 1) CurrentPage = 1;

        // Cập nhật danh sách số trang
        PageNumbers.Clear();
        for (int i = 1; i <= TotalPages; i++)
        {
            PageNumbers.Add(i);
        }

        UpdatePagedItems();
    }

    private void UpdatePagedItems()
    {
        PagedApartments.Clear();
        var pagedList = _filteredDisplayItems
            .Skip((CurrentPage - 1) * PageSize)
            .Take(PageSize)
            .ToList();

        foreach (var item in pagedList)
        {
            PagedApartments.Add(item);
        }

        OnPropertyChanged(nameof(PaginationSummary));

        // Mặc định chọn căn đầu tiên nếu chưa chọn hoặc căn chọn không còn trong trang
        if (SelectedApartmentItem == null || !PagedApartments.Any(p => p.Id == SelectedApartmentItem.Id))
        {
            SelectedApartmentItem = PagedApartments.FirstOrDefault();
        }
    }

    private void OpenAddForm()
    {
        ErrorMessage = string.Empty;
        FormTitle = "THÊM CĂN HỘ MỚI";
        EditingApartment = new Apartment
        {
            Status = ApartmentStatus.Empty,
            Floor = 1
        };
        IsFormOpen = true;
    }

    private void OpenEditForm(ApartmentDisplayItem item)
    {
        ErrorMessage = string.Empty;
        FormTitle = $"CẬP NHẬT CĂN HỘ {item.ApartmentNumber}";

        EditingApartment = new Apartment
        {
            Id = item.Id,
            ApartmentNumber = item.ApartmentNumber,
            Floor = item.Floor,
            Area = item.Area,
            Status = item.Status
        };
        IsFormOpen = true;
    }

    private void OpenEditForm(Apartment apartment)
    {
        ErrorMessage = string.Empty;
        FormTitle = $"CẬP NHẬT CĂN HỘ {apartment.ApartmentNumber}";

        EditingApartment = new Apartment
        {
            Id = apartment.Id,
            ApartmentNumber = apartment.ApartmentNumber,
            Floor = apartment.Floor,
            Area = apartment.Area,
            Status = apartment.Status
        };
        IsFormOpen = true;
    }

    private void CloseForm()
    {
        IsFormOpen = false;
        ErrorMessage = string.Empty;
    }

    private async Task SaveDataAsync()
    {
        ErrorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(EditingApartment.ApartmentNumber))
        {
            ErrorMessage = "Vui lòng nhập số căn hộ.";
            return;
        }

        if (EditingApartment.Floor <= 0)
        {
            ErrorMessage = "Số tầng phải lớn hơn 0.";
            return;
        }

        if (EditingApartment.Area <= 0)
        {
            ErrorMessage = "Diện tích phải lớn hơn 0.";
            return;
        }

        try
        {
            if (EditingApartment.Id == 0)
            {
                await _apartmentService.CreateApartmentAsync(EditingApartment);
            }
            else
            {
                await _apartmentService.UpdateApartmentAsync(EditingApartment);
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
            ErrorMessage = "Đã xảy ra lỗi hệ thống trong quá trình lưu dữ liệu.";
        }
    }

    private async Task DeleteDataAsync(object? parameter)
    {
        int aptId = 0;
        string aptNumber = string.Empty;
        int floor = 0;

        if (parameter is ApartmentDisplayItem displayItem)
        {
            aptId = displayItem.Id;
            aptNumber = displayItem.ApartmentNumber;
            floor = displayItem.Floor;
        }
        else if (parameter is Apartment apt)
        {
            aptId = apt.Id;
            aptNumber = apt.ApartmentNumber;
            floor = apt.Floor;
        }
        else
        {
            return;
        }

        ErrorMessage = string.Empty;

        var result = NotificationService.Show(
            $"Bạn có chắc chắn muốn xóa căn hộ số {aptNumber} (Tầng {floor}) không?",
            "Xác nhận xóa căn hộ",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning
        );

        if (result != System.Windows.MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            await _apartmentService.DeleteApartmentAsync(aptId);
            await LoadDataAsync();
        }
        catch (BusinessRuleException ex)
        {
            ErrorMessage = ex.Message;
        }
        catch (Exception)
        {
            ErrorMessage = "Đã xảy ra lỗi hệ thống khi xóa căn hộ.";
        }
    }
}
