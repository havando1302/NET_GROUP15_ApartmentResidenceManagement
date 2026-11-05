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
using Microsoft.Win32;

namespace ApartmentResidenceManagement.Wpf.ViewModels;

public class MyVehiclesViewModel : ViewModelBase
{
    private readonly System.Threading.SemaphoreSlim _loadLock = new(1, 1);
    private UserAccount? _account;
    private readonly VehicleService _vehicleService;
    private readonly ResidenceService _residenceService;

    private List<Vehicle> _allMyVehicles = new();
    private ObservableCollection<Vehicle> _vehicles = new();

    // Lọc & Tìm kiếm
    private string _searchText = string.Empty;
    private string _selectedTypeFilter = "Tất cả";

    // Thao tác Form
    private Vehicle? _selectedVehicle;
    private Vehicle _editingVehicle = new();
    private bool _isFormOpen;
    private string _formTitle = "ĐĂNG KÝ PHƯƠNG TIỆN MỚI";

    // Báo lỗi/Thành công
    private string _errorMessage = string.Empty;
    private bool _hasError;
    private bool _isLoading;
    private bool _hasApartment;

    #region Properties
    public ObservableCollection<Vehicle> Vehicles
    {
        get => _vehicles;
        set => SetProperty(ref _vehicles, value);
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
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
                ApplyFilters();
            }
        }
    }

    public List<string> TypeFilterOptions { get; } = new() { "Tất cả", "Xe máy", "Ô tô", "Xe đạp" };

    public Vehicle? SelectedVehicle
    {
        get => _selectedVehicle;
        set => SetProperty(ref _selectedVehicle, value);
    }

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

    public bool HasApartment
    {
        get => _hasApartment;
        set => SetProperty(ref _hasApartment, value);
    }

    public List<VehicleType> VehicleTypes { get; } = Enum.GetValues(typeof(VehicleType)).Cast<VehicleType>().ToList();

    public string? SelectedImagePath
    {
        get => EditingVehicle?.ImagePath;
        set
        {
            if (EditingVehicle != null)
            {
                EditingVehicle.ImagePath = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasSelectedImage));
                OnPropertyChanged(nameof(EditingVehicle));
            }
        }
    }

    public bool HasSelectedImage => !string.IsNullOrWhiteSpace(SelectedImagePath);

    private bool _isImageZoomOpen;
    public bool IsImageZoomOpen
    {
        get => _isImageZoomOpen;
        set => SetProperty(ref _isImageZoomOpen, value);
    }

    private string? _zoomedImagePath;
    public string? ZoomedImagePath
    {
        get => _zoomedImagePath;
        set => SetProperty(ref _zoomedImagePath, value);
    }

    private string _zoomedImageTitle = "Hình ảnh phương tiện";
    public string ZoomedImageTitle
    {
        get => _zoomedImageTitle;
        set => SetProperty(ref _zoomedImageTitle, value);
    }
    #endregion

    #region Commands
    public ICommand LoadVehiclesCommand { get; }
    public ICommand OpenAddFormCommand { get; }
    public ICommand OpenEditFormCommand { get; }
    public ICommand SaveVehicleCommand { get; }
    public ICommand CancelFormCommand { get; }
    public ICommand DeleteVehicleCommand { get; }
    public ICommand CancelPendingRegistrationCommand { get; }
    public ICommand RequestCancellationCommand { get; }
    public ICommand SelectImageCommand { get; }
    public ICommand ClearSearchCommand { get; }
    public ICommand OpenImageZoomCommand { get; }
    public ICommand CloseImageZoomCommand { get; }
    public ICommand ClearSelectedImageCommand { get; }
    #endregion

    public MyVehiclesViewModel(VehicleService vehicleService, ResidenceService residenceService)
    {
        _vehicleService = vehicleService;
        _residenceService = residenceService;
        LoadVehiclesCommand = new AsyncRelayCommand(_ => LoadMyVehiclesAsync());
        ClearSearchCommand = new RelayCommand(_ => SearchText = string.Empty);
        OpenAddFormCommand = new RelayCommand(_ => OpenAddForm());
        OpenEditFormCommand = new RelayCommand(v => { if (v is Vehicle veh) OpenEditForm(veh); });
        SaveVehicleCommand = new AsyncRelayCommand(_ => SaveVehicleAsync());
        CancelFormCommand = new RelayCommand(_ => CloseForm());
        DeleteVehicleCommand = new AsyncRelayCommand(DeleteVehicleAsync);
        CancelPendingRegistrationCommand = new AsyncRelayCommand(CancelPendingRegistrationAsync);
        RequestCancellationCommand = new AsyncRelayCommand(RequestCancellationAsync);

        OpenImageZoomCommand = new RelayCommand(p =>
        {
            string? path = p as string;
            string title = "Hình ảnh phương tiện";

            if (p is Vehicle v)
            {
                path = v.ImagePath;
                title = $"Hình ảnh xe {v.LicensePlate}";
            }
            else if (string.IsNullOrWhiteSpace(path))
            {
                path = SelectedImagePath;
                if (!string.IsNullOrWhiteSpace(EditingVehicle?.LicensePlate))
                {
                    title = $"Hình ảnh xe {EditingVehicle.LicensePlate}";
                }
            }

            if (!string.IsNullOrWhiteSpace(path))
            {
                ZoomedImagePath = path;
                ZoomedImageTitle = title;
                IsImageZoomOpen = true;
            }
        });

        CloseImageZoomCommand = new RelayCommand(_ =>
        {
            IsImageZoomOpen = false;
            ZoomedImagePath = null;
        });

        ClearSelectedImageCommand = new RelayCommand(_ =>
        {
            SelectedImagePath = null;
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
                string selectedFile = dialog.FileName;
                string finalPath = selectedFile;
                try
                {
                    string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                    string uploadDir = System.IO.Path.Combine(appData, "ApartmentResidenceManagement", "Vehicles");
                    System.IO.Directory.CreateDirectory(uploadDir);
                    string ext = System.IO.Path.GetExtension(selectedFile);
                    string destFile = System.IO.Path.Combine(uploadDir, $"{Guid.NewGuid()}{ext}");
                    System.IO.File.Copy(selectedFile, destFile, true);
                    finalPath = destFile;
                }
                catch { }

                SelectedImagePath = finalPath;
            }
        });
    }

    public void Initialize(UserAccount account)
    {
        _account = account;
    }

    public async Task LoadMyVehiclesAsync()
    {
        if (!await _loadLock.WaitAsync(0)) return;
        if (_account?.Resident == null)
        {
            HasApartment = false;
            return;
        }

        ErrorMessage = string.Empty;
        IsLoading = true;
        try
        {
            var activeResidence = await _residenceService.GetActiveResidenceByResidentIdAsync(_account.Resident.Id);
            if (activeResidence != null && activeResidence.Apartment != null)
            {
                HasApartment = true;

                var list = await _vehicleService.GetVehiclesByOwnerIdAsync(_account.Resident.Id);
                _allMyVehicles = list.ToList();
                ApplyFilters();
            }
            else
            {
                HasApartment = false;
            }
        }
        catch (Exception)
        {
            ErrorMessage = "Đã xảy ra lỗi khi lấy danh sách xe từ database.";
        }
        finally
        {
            IsLoading = false;
            _loadLock.Release();
        }
    }

    private void ApplyFilters()
    {
        var filtered = _allMyVehicles.AsEnumerable();

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

        // Tìm kiếm thông minh tiếng Việt (biển số, hãng xe, màu xe)
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            filtered = filtered.Where(v => VietnameseSearchHelper.MatchesSearch(SearchText,
                v.LicensePlate,
                v.Brand,
                v.Color));
        }

        Vehicles.Clear();
        foreach (var v in filtered)
        {
            Vehicles.Add(v);
        }
    }

    private void OpenAddForm()
    {
        ErrorMessage = string.Empty;
        FormTitle = "ĐĂNG KÝ PHƯƠNG TIỆN MỚI";
        EditingVehicle = new Vehicle
        {
            VehicleType = VehicleType.Moto,
            OwnerId = _account?.Resident?.Id ?? 0,
            RegistrationDate = DateTime.Now
        };
        OnPropertyChanged(nameof(SelectedImagePath));
        OnPropertyChanged(nameof(HasSelectedImage));
        IsFormOpen = true;
    }

    private void OpenEditForm(Vehicle vehicle)
    {
        ErrorMessage = string.Empty;
        FormTitle = $"SỬA PHƯƠNG TIỆN {vehicle.LicensePlate}";
        EditingVehicle = new Vehicle
        {
            Id = vehicle.Id,
            LicensePlate = vehicle.LicensePlate,
            VehicleType = vehicle.VehicleType,
            Brand = vehicle.Brand,
            Color = vehicle.Color,
            ImagePath = vehicle.ImagePath,
            RegistrationDate = vehicle.RegistrationDate,
            OwnerId = vehicle.OwnerId,
            RegistrationStatus = vehicle.RegistrationStatus,
            RejectReason = vehicle.RejectReason
        };
        OnPropertyChanged(nameof(SelectedImagePath));
        OnPropertyChanged(nameof(HasSelectedImage));
        IsFormOpen = true;
    }

    private void CloseForm()
    {
        IsFormOpen = false;
        ErrorMessage = string.Empty;
        SelectedVehicle = null;
    }

    private async Task SaveVehicleAsync()
    {
        ErrorMessage = string.Empty;

        if (_account?.Resident is not { } currentResident)
        {
            ErrorMessage = "Không thể xác định tài khoản cư dân.";
            return;
        }

        if (string.IsNullOrWhiteSpace(EditingVehicle.LicensePlate))
        {
            ErrorMessage = "Vui lòng nhập biển số xe.";
            return;
        }

        if (EditingVehicle.OwnerId == 0)
        {
            ErrorMessage = "Không thể xác định chủ sở hữu phương tiện.";
            return;
        }

        if (string.IsNullOrWhiteSpace(EditingVehicle.ImagePath))
        {
            ErrorMessage = "Vui lòng tải lên hình ảnh phương tiện (danh mục ảnh là bắt buộc khi tạo đơn).";
            return;
        }

        try
        {
            if (EditingVehicle.Id == 0)
            {
                await _vehicleService.SubmitVehicleRegistrationAsync(EditingVehicle);
            }
            else
            {
                await _vehicleService.UpdateVehicleRegistrationAsync(currentResident.Id, EditingVehicle);
            }

            IsFormOpen = false;
            await LoadMyVehiclesAsync();
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

        private async Task CancelPendingRegistrationAsync(object? parameter)
    {
        if (parameter is not Vehicle vehicle) return;
        ErrorMessage = string.Empty;

        if (_account?.Resident is not { } currentResident)
        {
            ErrorMessage = "Không thể xác định tài khoản cư dân.";
            return;
        }

        var result = NotificationService.Show(
            $"Bạn có chắc chắn muốn hủy đơn đăng ký phương tiện biển số {vehicle.LicensePlate} không?",
            "Xác nhận hủy đơn",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Question
        );

        if (result != System.Windows.MessageBoxResult.Yes) return;

        try
        {
            await _vehicleService.CancelPendingVehicleRegistrationAsync(currentResident.Id, vehicle.Id);
            await LoadMyVehiclesAsync();
            NotificationService.Show("Đã hủy đơn đăng ký phương tiện thành công.", "Thành công", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }
        catch (BusinessRuleException ex)
        {
            ErrorMessage = ex.Message;
        }
        catch (Exception)
        {
            ErrorMessage = "Lỗi khi hủy đơn đăng ký phương tiện.";
        }
    }

    private async Task RequestCancellationAsync(object? parameter)
    {
        if (parameter is not Vehicle vehicle) return;
        ErrorMessage = string.Empty;

        if (_account?.Resident is not { } currentResident)
        {
            ErrorMessage = "Không thể xác định tài khoản cư dân.";
            return;
        }

        var result = NotificationService.Show(
            $"Bạn có chắc chắn muốn gửi yêu cầu hủy đăng ký phương tiện biển số {vehicle.LicensePlate} tới Ban quản lý không?\n\nBan quản lý sẽ kiểm tra và hoàn tất thủ tục hủy chỗ đỗ xe cho bạn.",
            "Xác nhận gửi yêu cầu hủy",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Question
        );

        if (result != System.Windows.MessageBoxResult.Yes) return;

        try
        {
            await _vehicleService.RequestVehicleCancellationAsync(currentResident.Id, vehicle.Id, "Cư dân gửi yêu cầu hủy qua cổng cư dân");
            await LoadMyVehiclesAsync();
            NotificationService.Show("Yêu cầu hủy đăng ký phương tiện đã được gửi tới Ban quản lý tòa nhà thành công.", "Đã gửi yêu cầu", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }
        catch (BusinessRuleException ex)
        {
            ErrorMessage = ex.Message;
        }
        catch (Exception)
        {
            ErrorMessage = "Lỗi khi gửi yêu cầu hủy đăng ký phương tiện.";
        }
    }

    private async Task DeleteVehicleAsync(object? parameter)
    {
        if (parameter is not Vehicle vehicle) return;
        ErrorMessage = string.Empty;

        if (_account?.Resident is not { } currentResident)
        {
            ErrorMessage = "Không thể xác định tài khoản cư dân.";
            return;
        }

        if (vehicle.RegistrationStatus == VehicleRegistrationStatus.Approved)
        {
            ErrorMessage = "Phương tiện đã được phê duyệt, cư dân không thể tự xóa. Vui lòng bấm 'Yêu cầu hủy' để gửi yêu cầu tới Ban quản lý.";
            return;
        }

        var result = NotificationService.Show(
            $"Bạn có chắc chắn muốn xóa phương tiện biển số {vehicle.LicensePlate} khỏi danh sách không?",
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
            await _vehicleService.DeleteVehicleByOwnerAsync(currentResident.Id, vehicle.Id);
            await LoadMyVehiclesAsync();
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
}
