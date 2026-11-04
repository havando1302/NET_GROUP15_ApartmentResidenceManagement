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
using ApartmentResidenceManagement.Application.Common;
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

public class PageItem
{
    public int PageNumber { get; set; }
    public bool IsActive { get; set; }
}

public class FilterOptionItem<T>
{
    public string DisplayText { get; set; } = string.Empty;
    public T? Value { get; set; }

    public override string ToString() => DisplayText;
}

public class ApartmentDisplayItem
{
    public int DisplayIndex { get; set; }
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
    private FilterOptionItem<ApartmentStatus?>? _selectedStatusFilterItem;
    private FilterOptionItem<int?>? _selectedFloorFilterItem;
    public ObservableCollection<FilterOptionItem<int?>> FloorFilterItems { get; } = new();
    public ObservableCollection<FilterOptionItem<ApartmentStatus?>> StatusFilterItems { get; } = new();

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
    private ObservableCollection<PageItem> _pageItems = new();
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

    public ObservableCollection<int?> AvailableFloors => 
        new ObservableCollection<int?>(FloorFilterItems.Select(f => f.Value));

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

    public FilterOptionItem<ApartmentStatus?>? SelectedStatusFilterItem
    {
        get => _selectedStatusFilterItem;
        set
        {
            if (SetProperty(ref _selectedStatusFilterItem, value))
            {
                CurrentPage = 1;
                ApplyFilters();
                OnPropertyChanged(nameof(SelectedStatusFilter));
            }
        }
    }

    public FilterOptionItem<int?>? SelectedFloorFilterItem
    {
        get => _selectedFloorFilterItem;
        set
        {
            if (SetProperty(ref _selectedFloorFilterItem, value))
            {
                CurrentPage = 1;
                ApplyFilters();
                OnPropertyChanged(nameof(SelectedFloorFilter));
            }
        }
    }

    public ApartmentStatus? SelectedStatusFilter
    {
        get => _selectedStatusFilterItem?.Value;
        set => SelectedStatusFilterItem = StatusFilterItems.FirstOrDefault(s => s.Value == value) ?? StatusFilterItems.FirstOrDefault();
    }

    public int? SelectedFloorFilter
    {
        get => _selectedFloorFilterItem?.Value;
        set => SelectedFloorFilterItem = FloorFilterItems.FirstOrDefault(f => f.Value == value) ?? FloorFilterItems.FirstOrDefault();
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

    public ObservableCollection<PageItem> PageItems
    {
        get => _pageItems;
        set => SetProperty(ref _pageItems, value);
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
    public ICommand ClearSearchCommand { get; }
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
        ClearSearchCommand = new RelayCommand(_ => SearchText = string.Empty);
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

        // Khởi tạo các lựa chọn bộ lọc trạng thái
        StatusFilterItems.Clear();
        StatusFilterItems.Add(new FilterOptionItem<ApartmentStatus?> { DisplayText = "Tất cả", Value = null });
        StatusFilterItems.Add(new FilterOptionItem<ApartmentStatus?> { DisplayText = "Đang ở", Value = ApartmentStatus.Occupied });
        StatusFilterItems.Add(new FilterOptionItem<ApartmentStatus?> { DisplayText = "Trống", Value = ApartmentStatus.Empty });
        StatusFilterItems.Add(new FilterOptionItem<ApartmentStatus?> { DisplayText = "Bảo trì", Value = ApartmentStatus.UnderMaintenance });
        _selectedStatusFilterItem = StatusFilterItems[0];

        // Khởi tạo bộ lọc tầng ban đầu
        FloorFilterItems.Clear();
        FloorFilterItems.Add(new FilterOptionItem<int?> { DisplayText = "Tất cả", Value = null });
        _selectedFloorFilterItem = FloorFilterItems[0];
    }

    public async Task LoadDataAsync()
    {
        ErrorMessage = string.Empty;
        IsLoading = true;
        try
        {
            var list = await _apartmentService.GetAllApartmentsAsync();
            _allApartments = list.ToList();

            // Load active residences map từ Aiven trong 1 truy vấn duy nhất
            Dictionary<int, List<ResidentMemberItem>> residentMap = new();
            Dictionary<int, DateTime> lastUpdatedMap = new();
            if (_residenceService != null)
            {
                try
                {
                    var allHistories = await _residenceService.GetAllResidencesAsync();
                    var activeResidences = allHistories.Where(r => r.EndDate == null);

                    foreach (var group in activeResidences.GroupBy(r => r.ApartmentId))
                    {
                        var members = group
                            .Where(r => r.Resident != null)
                            .OrderBy(r => r.RelationshipType) // Chủ hộ (Owner=0) luôn ưu tiên lên đầu
                            .Select(r => new ResidentMemberItem
                            {
                                Id = r.ResidentId,
                                FullName = r.Resident?.FullName ?? "Chưa rõ",
                                PhoneNumber = r.Resident?.PhoneNumber ?? "",
                                Relationship = FormatRelationship(r.RelationshipType)
                            }).ToList();

                        residentMap[group.Key] = members;

                        var latestStart = group.Max(r => r.StartDate);
                        lastUpdatedMap[group.Key] = latestStart;
                    }
                }
                catch
                {
                    // Fallback an toàn nếu có gián đoạn mạng
                }
            }

            // Chuyển đổi thành ApartmentDisplayItem
            _allDisplayItems = _allApartments.Select(a =>
            {
                residentMap.TryGetValue(a.Id, out var residents);
                var resList = residents ?? new List<ResidentMemberItem>();
                lastUpdatedMap.TryGetValue(a.Id, out var lastUpdated);

                // Đồng bộ trạng thái: nếu căn hộ có cư dân thực tế thì hiển thị Đang ở
                var actualStatus = resList.Count > 0 ? ApartmentStatus.Occupied : a.Status;

                return new ApartmentDisplayItem
                {
                    Id = a.Id,
                    ApartmentNumber = a.ApartmentNumber,
                    Floor = a.Floor,
                    Area = a.Area,
                    Status = actualStatus,
                    ResidentCount = resList.Count,
                    Residents = resList,
                    LastUpdated = lastUpdated != default ? lastUpdated : DateTime.Today,
                    RawApartment = a
                };
            }).ToList();

            // Cập nhật thống kê KPI đồng bộ với dữ liệu thực tế
            TotalApartmentsCount = _allApartments.Count;
            OccupiedApartmentsCount = _allDisplayItems.Count(a => a.Status == ApartmentStatus.Occupied);
            EmptyApartmentsCount = _allDisplayItems.Count(a => a.Status == ApartmentStatus.Empty);
            TotalFloorsCount = _allApartments.Select(a => a.Floor).Distinct().Count();

            // Cập nhật danh sách Tầng khả dụng để lọc
            var floors = _allApartments.Select(a => a.Floor).Distinct().OrderBy(f => f).ToList();
            var prevSelectedFloor = SelectedFloorFilterItem?.Value;

            FloorFilterItems.Clear();
            FloorFilterItems.Add(new FilterOptionItem<int?> { DisplayText = "Tất cả", Value = null });
            foreach (var floor in floors)
            {
                FloorFilterItems.Add(new FilterOptionItem<int?> { DisplayText = $"Tầng {floor}", Value = floor });
            }

            _selectedFloorFilterItem = FloorFilterItems.FirstOrDefault(f => f.Value == prevSelectedFloor) ?? FloorFilterItems[0];
            OnPropertyChanged(nameof(SelectedFloorFilterItem));

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

        // 1. Tìm kiếm thông minh tiếng Việt (mã căn, tầng, tên cư dân)
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            filtered = filtered.Where(a => VietnameseSearchHelper.MatchesSearch(SearchText,
                a.ApartmentNumber,
                a.Floor.ToString(),
                $"Tầng {a.Floor}",
                string.Join(" ", a.Residents.Select(r => r.FullName))));
        }

        // 2. Lọc theo trạng thái
        if (SelectedStatusFilterItem?.Value != null)
        {
            filtered = filtered.Where(a => a.Status == SelectedStatusFilterItem.Value.Value);
        }

        // 3. Lọc theo tầng
        if (SelectedFloorFilterItem?.Value != null)
        {
            filtered = filtered.Where(a => a.Floor == SelectedFloorFilterItem.Value.Value);
        }

        _filteredDisplayItems = filtered.OrderBy(a => a.ApartmentNumber).ToList();

        // Tính toán phân trang
        TotalPages = Math.Max(1, (int)Math.Ceiling((double)_filteredDisplayItems.Count / PageSize));
        if (CurrentPage > TotalPages) CurrentPage = TotalPages;
        if (CurrentPage < 1) CurrentPage = 1;

        UpdatePagedItems();
    }

    private void UpdatePagedItems()
    {
        PagedApartments.Clear();
        var pagedList = _filteredDisplayItems
            .Skip((CurrentPage - 1) * PageSize)
            .Take(PageSize)
            .ToList();

        int startIndex = (CurrentPage - 1) * PageSize;
        for (int i = 0; i < pagedList.Count; i++)
        {
            pagedList[i].DisplayIndex = startIndex + i + 1;
            PagedApartments.Add(pagedList[i]);
        }

        // Cập nhật danh sách số trang PageItems
        PageItems.Clear();
        for (int i = 1; i <= TotalPages; i++)
        {
            PageItems.Add(new PageItem { PageNumber = i, IsActive = (i == CurrentPage) });
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
            if (displayItem.ResidentCount > 0)
            {
                NotificationService.Show(
                    $"Không thể xóa căn hộ {displayItem.ApartmentNumber} (Tầng {displayItem.Floor}) vì hiện có {displayItem.ResidentCount} cư dân đang cư trú.\nVui lòng kết thúc cư trú của cư dân trước khi xóa căn hộ.",
                    "Không thể xóa căn hộ",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning
                );
                return;
            }

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
