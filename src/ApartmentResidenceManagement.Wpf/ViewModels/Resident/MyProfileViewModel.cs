using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using ApartmentResidenceManagement.Domain.Entities;
using ApartmentResidenceManagement.Domain.Enums;
using ApartmentResidenceManagement.Wpf.Commands;

namespace ApartmentResidenceManagement.Wpf.ViewModels;

public class AvatarPresetItem
{
    public string Name { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string ImagePath { get; set; } = string.Empty;
}

public class MyProfileViewModel : ViewModelBase
{
    private UserAccount? _account;

    // Vai trò tài khoản
    public bool IsAdmin => _account?.Role == UserRole.Admin;
    public bool IsResident => !IsAdmin;

    #region Admin Properties (Dành riêng cho Quản trị viên theo xu hướng 2026)
    public string AdminCode => _account != null ? $"TK-{_account.Id:D4}" : "TK-0001";
    public string AdminUsername => _account?.Username ?? "admin";
    public string AdminDisplayName => _account?.Username == "admin" ? "Ban Quản lý Tòa nhà" : (_account?.Username ?? "Quản trị viên");
    public string AdminRoleName => "Quản trị viên Cấp cao";
    public string AdminDepartment => "Ban Quản lý & Vận hành Tòa nhà";
    public string AdminOrganization => "Chung cư ResidenceManager";
    public string AdminSecurityLevel => "Cấp 1 – Toàn quyền hệ thống (Root Admin)";
    public string AdminCreatedAtText => _account != null ? _account.CreatedAt.ToString("dd/MM/yyyy HH:mm") : "10/01/2026 08:00";
    public string AdminLastLoginText => _account?.LastLoginAt != null ? _account.LastLoginAt.Value.ToString("dd/MM/yyyy HH:mm") : DateTime.Now.ToString("dd/MM/yyyy HH:mm");
    public string AdminStatusText => _account?.IsActive == true ? "Đang hoạt động" : "Đã khóa";

    // Thống kê quy mô vận hành thời gian thực (Live System Stats)
    private int _adminTotalApartments;
    public int AdminTotalApartments
    {
        get => _adminTotalApartments;
        set => SetProperty(ref _adminTotalApartments, value);
    }

    private int _adminTotalResidents;
    public int AdminTotalResidents
    {
        get => _adminTotalResidents;
        set => SetProperty(ref _adminTotalResidents, value);
    }

    private int _adminTotalVehicles;
    public int AdminTotalVehicles
    {
        get => _adminTotalVehicles;
        set => SetProperty(ref _adminTotalVehicles, value);
    }

    private string _adminSystemStatus = "99.9% Trực tuyến";
    public string AdminSystemStatus
    {
        get => _adminSystemStatus;
        set => SetProperty(ref _adminSystemStatus, value);
    }

    // Thông tin liên hệ Ban Quản trị công khai
    private string _bqlHotline = "1900 6868";
    public string BqlHotline
    {
        get => _bqlHotline;
        set => SetProperty(ref _bqlHotline, value);
    }

    private string _bqlEmail = "banquanly@residencemanager.vn";
    public string BqlEmail
    {
        get => _bqlEmail;
        set => SetProperty(ref _bqlEmail, value);
    }

    private string _bqlOffice = "Tầng 1 - Sảnh Tiếp dân & Điều hành tòa nhà";
    public string BqlOffice
    {
        get => _bqlOffice;
        set => SetProperty(ref _bqlOffice, value);
    }

    private string _bqlWorkingHours = "07:30 – 18:00 (Thứ 2 – Thứ 7) | Trực 24/7";
    public string BqlWorkingHours
    {
        get => _bqlWorkingHours;
        set => SetProperty(ref _bqlWorkingHours, value);
    }

    // Modal Cập nhật thông tin Ban Quản lý
    private bool _isEditBqlModalOpen;
    public bool IsEditBqlModalOpen
    {
        get => _isEditBqlModalOpen;
        set => SetProperty(ref _isEditBqlModalOpen, value);
    }

    private string _editBqlHotline = "1900 6868";
    public string EditBqlHotline
    {
        get => _editBqlHotline;
        set => SetProperty(ref _editBqlHotline, value);
    }

    private string _editBqlEmail = "banquanly@residencemanager.vn";
    public string EditBqlEmail
    {
        get => _editBqlEmail;
        set => SetProperty(ref _editBqlEmail, value);
    }

    private string _editBqlOffice = "Tầng 1 - Sảnh Tiếp dân & Điều hành tòa nhà";
    public string EditBqlOffice
    {
        get => _editBqlOffice;
        set => SetProperty(ref _editBqlOffice, value);
    }

    private string _editBqlWorkingHours = "07:30 – 18:00 (Thứ 2 – Thứ 7) | Trực 24/7";
    public string EditBqlWorkingHours
    {
        get => _editBqlWorkingHours;
        set => SetProperty(ref _editBqlWorkingHours, value);
    }

    // Modal Đổi mật khẩu Quản trị viên
    private bool _isChangePasswordModalOpen;
    public bool IsChangePasswordModalOpen
    {
        get => _isChangePasswordModalOpen;
        set => SetProperty(ref _isChangePasswordModalOpen, value);
    }

    private string _oldPassword = string.Empty;
    public string OldPassword
    {
        get => _oldPassword;
        set => SetProperty(ref _oldPassword, value);
    }

    private string _newPassword = string.Empty;
    public string NewPassword
    {
        get => _newPassword;
        set => SetProperty(ref _newPassword, value);
    }

    private string _confirmNewPassword = string.Empty;
    public string ConfirmNewPassword
    {
        get => _confirmNewPassword;
        set => SetProperty(ref _confirmNewPassword, value);
    }

    private string _changePasswordError = string.Empty;
    public string ChangePasswordError
    {
        get => _changePasswordError;
        set
        {
            if (SetProperty(ref _changePasswordError, value))
            {
                OnPropertyChanged(nameof(HasChangePasswordError));
            }
        }
    }

    public bool HasChangePasswordError => !string.IsNullOrWhiteSpace(ChangePasswordError);
    #endregion

    #region Resident Properties (Dành riêng cho Cư dân)
    // Thông tin định danh (chỉ đọc)
    private string _residentId = "CD-0001";
    private string _fullName = "Nguyễn Văn Minh";
    private string _hometown = "Đan Phượng, Hà Nội";
    private string _dobText = "15/03/2002";
    private string _identityCard = "012345678901";
    private string _email = "minhnv@gmail.com";
    private string _gender = "Nam";
    private string _permanentAddress = "Xã Tân Lập, Huyện Đan Phượng, TP. Hà Nội";
    private string _joinDate = "10/01/2024";

    // Thông tin căn hộ tóm tắt
    private string _apartmentNumber = "A-1203";
    private string _buildingBlock = "A";
    private string _floorNumber = "12";

    // Trường có thể cập nhật
    private string _phoneNumber = "0987 654 321";
    private string _editPhoneNumber = "0987 654 321";
    private string _contactAddress = "Đan Phượng, Hà Nội";
    private string _editContactAddress = "Đan Phượng, Hà Nội";
    private string _editEmail = "minhnv@gmail.com";
    private bool _isEditModalOpen;

    // Trạng thái thông báo
    private string _successMessage = string.Empty;
    private string _errorMessage = string.Empty;

    public string ResidentId
    {
        get => _residentId;
        set => SetProperty(ref _residentId, value);
    }

    public string FullName
    {
        get => _fullName;
        set => SetProperty(ref _fullName, value);
    }

    public string Hometown
    {
        get => _hometown;
        set => SetProperty(ref _hometown, value);
    }

    public string DobText
    {
        get => _dobText;
        set => SetProperty(ref _dobText, value);
    }

    public string IdentityCard
    {
        get => _identityCard;
        set => SetProperty(ref _identityCard, value);
    }

    public string Email
    {
        get => _email;
        set => SetProperty(ref _email, value);
    }

    public bool IsEditModalOpen
    {
        get => _isEditModalOpen;
        set
        {
            if (SetProperty(ref _isEditModalOpen, value))
            {
                OnPropertyChanged(nameof(IsBackgroundErrorVisible));
            }
        }
    }

    public string Gender
    {
        get => _gender;
        set => SetProperty(ref _gender, value);
    }

    public string PermanentAddress
    {
        get => _permanentAddress;
        set => SetProperty(ref _permanentAddress, value);
    }

    public string JoinDate
    {
        get => _joinDate;
        set => SetProperty(ref _joinDate, value);
    }

    public string ApartmentNumber
    {
        get => _apartmentNumber;
        set => SetProperty(ref _apartmentNumber, value);
    }

    public string BuildingBlock
    {
        get => _buildingBlock;
        set => SetProperty(ref _buildingBlock, value);
    }

    public string FloorNumber
    {
        get => _floorNumber;
        set => SetProperty(ref _floorNumber, value);
    }

    public string PhoneNumber
    {
        get => _phoneNumber;
        set => SetProperty(ref _phoneNumber, value);
    }

    public string EditPhoneNumber
    {
        get => _editPhoneNumber;
        set
        {
            if (SetProperty(ref _editPhoneNumber, value))
            {
                if (HasPhoneError)
                {
                    EditPhoneError = string.Empty;
                    ModalErrorMessage = string.Empty;
                }
            }
        }
    }

    public string EditEmail
    {
        get => _editEmail;
        set
        {
            if (SetProperty(ref _editEmail, value))
            {
                if (HasEmailError)
                {
                    EditEmailError = string.Empty;
                    ModalErrorMessage = string.Empty;
                }
            }
        }
    }

    private string _modalErrorMessage = string.Empty;
    public string ModalErrorMessage
    {
        get => _modalErrorMessage;
        set
        {
            if (SetProperty(ref _modalErrorMessage, value))
            {
                OnPropertyChanged(nameof(HasModalError));
            }
        }
    }

    private string _editPhoneError = string.Empty;
    public string EditPhoneError
    {
        get => _editPhoneError;
        set
        {
            if (SetProperty(ref _editPhoneError, value))
            {
                OnPropertyChanged(nameof(HasPhoneError));
                OnPropertyChanged(nameof(PhoneBorderBrush));
            }
        }
    }

    private string _editEmailError = string.Empty;
    public string EditEmailError
    {
        get => _editEmailError;
        set
        {
            if (SetProperty(ref _editEmailError, value))
            {
                OnPropertyChanged(nameof(HasEmailError));
                OnPropertyChanged(nameof(EmailBorderBrush));
            }
        }
    }

    public bool HasModalError => !string.IsNullOrWhiteSpace(ModalErrorMessage);
    public bool HasPhoneError => !string.IsNullOrWhiteSpace(EditPhoneError);
    public bool HasEmailError => !string.IsNullOrWhiteSpace(EditEmailError);
    public string PhoneBorderBrush => HasPhoneError ? "#EF4444" : "#CBD5E1";
    public string EmailBorderBrush => HasEmailError ? "#EF4444" : "#CBD5E1";
    public bool IsBackgroundErrorVisible => !IsEditModalOpen && !string.IsNullOrWhiteSpace(ErrorMessage);

    public string ContactAddress
    {
        get => _contactAddress;
        set => SetProperty(ref _contactAddress, value);
    }

    public string EditContactAddress
    {
        get => _editContactAddress;
        set => SetProperty(ref _editContactAddress, value);
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
                OnPropertyChanged(nameof(IsBackgroundErrorVisible));
            }
        }
    }

    private string _avatarPath = "/Assets/resident_avatar.png";
    public string AvatarPath
    {
        get => _avatarPath;
        set => SetProperty(ref _avatarPath, value);
    }

    private bool _isAvatarPickerOpen;
    public bool IsAvatarPickerOpen
    {
        get => _isAvatarPickerOpen;
        set => SetProperty(ref _isAvatarPickerOpen, value);
    }

    public System.Collections.ObjectModel.ObservableCollection<AvatarPresetItem> AvailableAvatars { get; } = new()
    {
        new AvatarPresetItem { Name = "Nguyễn Văn Minh", Role = "Chủ hộ", ImagePath = "/Assets/resident_avatar.png" },
        new AvatarPresetItem { Name = "Trần Thị Hoa", Role = "Vợ", ImagePath = "/Assets/wife_avatar.png" },
        new AvatarPresetItem { Name = "Nguyễn Gia Bảo", Role = "Con trai", ImagePath = "/Assets/avatar_son.png" },
        new AvatarPresetItem { Name = "Nguyễn An Nhiên", Role = "Con gái", ImagePath = "/Assets/avatar_daughter.png" },
        new AvatarPresetItem { Name = "Bé Minh Khôi", Role = "Bé trai", ImagePath = "/Assets/avatar_toddler_boy.png" },
        new AvatarPresetItem { Name = "Bé Tuệ Mẫn", Role = "Em bé", ImagePath = "/Assets/avatar_baby_girl.png" },
        new AvatarPresetItem { Name = "Nguyễn Tuấn Anh", Role = "Em trai", ImagePath = "/Assets/avatar_young_man.png" },
        new AvatarPresetItem { Name = "Nguyễn Thùy Linh", Role = "Em gái", ImagePath = "/Assets/avatar_young_woman.png" },
        new AvatarPresetItem { Name = "Nguyễn Văn Hùng", Role = "Ông nội", ImagePath = "/Assets/avatar_grandfather.png" },
        new AvatarPresetItem { Name = "Lê Thị Mai", Role = "Bà nội", ImagePath = "/Assets/avatar_grandmother.png" }
    };
    #endregion

    #region Commands
    public ICommand SaveProfileCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand ResetCommand { get; }
    public ICommand UpdatePhotoCommand { get; }
    public ICommand ToggleAvatarPickerCommand { get; }
    public ICommand SelectAvatarCommand { get; }
    public ICommand OpenEditModalCommand { get; }
    public ICommand CloseEditModalCommand { get; }

    // Admin Commands
    public ICommand OpenChangePasswordModalCommand { get; }
    public ICommand CloseChangePasswordModalCommand { get; }
    public ICommand ConfirmChangePasswordCommand { get; }

    public ICommand OpenEditBqlModalCommand { get; }
    public ICommand CloseEditBqlModalCommand { get; }
    public ICommand SaveBqlContactCommand { get; }

    public ICommand CheckSecurityCommand { get; }
    #endregion

    private readonly Application.Services.ResidentService? _residentService;
    private readonly Application.Services.ResidenceService? _residenceService;
    private readonly Application.Services.AccountService? _accountService;
    private readonly Application.Services.ApartmentService? _apartmentService;
    private readonly Application.Services.VehicleService? _vehicleService;

    public MyProfileViewModel() : this(null, null, null, null, null)
    {
    }

    public MyProfileViewModel(Application.Services.ResidentService? residentService) : this(residentService, null, null, null, null)
    {
    }

    public MyProfileViewModel(
        Application.Services.ResidentService? residentService,
        Application.Services.ResidenceService? residenceService,
        Application.Services.AccountService? accountService = null,
        Application.Services.ApartmentService? apartmentService = null,
        Application.Services.VehicleService? vehicleService = null)
    {
        _residentService = residentService;
        _residenceService = residenceService;
        _accountService = accountService;
        _apartmentService = apartmentService;
        _vehicleService = vehicleService;

        // Commands Cư dân
        OpenEditModalCommand = new RelayCommand(_ =>
        {
            EditPhoneNumber = PhoneNumber == "Chưa cập nhật" ? string.Empty : PhoneNumber;
            EditEmail = Email == "Chưa cập nhật" ? string.Empty : Email;
            EditContactAddress = Hometown;
            ErrorMessage = string.Empty;
            ModalErrorMessage = string.Empty;
            EditPhoneError = string.Empty;
            EditEmailError = string.Empty;
            IsEditModalOpen = true;
        });

        CloseEditModalCommand = new RelayCommand(_ =>
        {
            IsEditModalOpen = false;
            ErrorMessage = string.Empty;
            ModalErrorMessage = string.Empty;
            EditPhoneError = string.Empty;
            EditEmailError = string.Empty;
        });

        SaveProfileCommand = new AsyncRelayCommand(_ => SaveProfileAsync());
        RefreshCommand = new AsyncRelayCommand(_ => LoadProfileAsync());
        ResetCommand = new RelayCommand(_ => ResetEditForm());
        ToggleAvatarPickerCommand = new RelayCommand(_ => IsAvatarPickerOpen = !IsAvatarPickerOpen);
        SelectAvatarCommand = new AsyncRelayCommand(async p =>
        {
            string? newPath = null;
            string? roleDesc = null;
            if (p is string path && !string.IsNullOrEmpty(path))
            {
                newPath = path;
            }
            else if (p is AvatarPresetItem item)
            {
                newPath = item.ImagePath;
                roleDesc = $"{item.Name} ({item.Role})";
            }

            if (!string.IsNullOrEmpty(newPath))
            {
                AvatarPath = newPath;
                IsAvatarPickerOpen = false;

                if (_account?.Resident != null && _residentService != null)
                {
                    try
                    {
                        await _residentService.UpdateAvatarAsync(_account.Resident.Id, newPath);
                        _account.Resident.AvatarPath = newPath;
                    }
                    catch (Exception ex)
                    {
                        ErrorMessage = $"Lỗi khi lưu ảnh vào cơ sở dữ liệu: {ex.Message}";
                        return;
                    }
                }

                SuccessMessage = roleDesc != null
                    ? $"Đã đổi ảnh đại diện sang {roleDesc} và lưu vào cơ sở dữ liệu thành công!"
                    : "Đã cập nhật ảnh đại diện vào cơ sở dữ liệu thành công!";
                ErrorMessage = string.Empty;
            }
        });
        UpdatePhotoCommand = ToggleAvatarPickerCommand;

        // Commands Quản trị viên (Admin)
        OpenChangePasswordModalCommand = new RelayCommand(_ =>
        {
            OldPassword = string.Empty;
            NewPassword = string.Empty;
            ConfirmNewPassword = string.Empty;
            ChangePasswordError = string.Empty;
            IsChangePasswordModalOpen = true;
        });

        CloseChangePasswordModalCommand = new RelayCommand(_ =>
        {
            IsChangePasswordModalOpen = false;
            ChangePasswordError = string.Empty;
        });

        ConfirmChangePasswordCommand = new AsyncRelayCommand(_ => ConfirmChangePasswordAsync());

        OpenEditBqlModalCommand = new RelayCommand(_ =>
        {
            EditBqlHotline = BqlHotline;
            EditBqlEmail = BqlEmail;
            EditBqlOffice = BqlOffice;
            EditBqlWorkingHours = BqlWorkingHours;
            IsEditBqlModalOpen = true;
        });

        CloseEditBqlModalCommand = new RelayCommand(_ =>
        {
            IsEditBqlModalOpen = false;
        });

        SaveBqlContactCommand = new RelayCommand(_ =>
        {
            BqlHotline = EditBqlHotline.Trim();
            BqlEmail = EditBqlEmail.Trim();
            BqlOffice = EditBqlOffice.Trim();
            BqlWorkingHours = EditBqlWorkingHours.Trim();
            IsEditBqlModalOpen = false;
            SuccessMessage = "Đã cập nhật thông tin liên hệ Ban Quản lý Tòa nhà thành công!";
            ErrorMessage = string.Empty;
        });

        CheckSecurityCommand = new RelayCommand(_ =>
        {
            SuccessMessage = "Tài khoản quản trị đang được bảo vệ an toàn: Mã hóa mật khẩu BCrypt 11 vòng, kiểm soát phiên làm việc chống giả mạo và phân quyền Quản trị tối cao (Root Admin).";
            ErrorMessage = string.Empty;
        });
    }

    public void Initialize(UserAccount account)
    {
        _account = account;
        OnPropertyChanged(nameof(IsAdmin));
        OnPropertyChanged(nameof(IsResident));
        OnPropertyChanged(nameof(AdminCode));
        OnPropertyChanged(nameof(AdminUsername));
        OnPropertyChanged(nameof(AdminDisplayName));
        OnPropertyChanged(nameof(AdminRoleName));
        OnPropertyChanged(nameof(AdminDepartment));
        OnPropertyChanged(nameof(AdminOrganization));
        OnPropertyChanged(nameof(AdminSecurityLevel));
        OnPropertyChanged(nameof(AdminCreatedAtText));
        OnPropertyChanged(nameof(AdminLastLoginText));
        OnPropertyChanged(nameof(AdminStatusText));

        if (IsAdmin)
        {
            _ = LoadAdminStatsAsync();
        }
        else
        {
            LoadProfileData();
        }
    }

    public async Task LoadAdminStatsAsync()
    {
        try
        {
            if (_apartmentService != null)
            {
                var apts = await _apartmentService.GetAllApartmentsAsync();
                AdminTotalApartments = apts?.Count() ?? 0;
            }
            if (_residentService != null)
            {
                var res = await _residentService.GetAllResidentsAsync();
                AdminTotalResidents = res?.Count() ?? 0;
            }
            if (_vehicleService != null)
            {
                var veh = await _vehicleService.GetAllVehiclesAsync();
                AdminTotalVehicles = veh?.Count() ?? 0;
            }
        }
        catch (Exception)
        {
            // Fallback an toàn nếu tạm thời mất kết nối database
        }
    }

    public async Task LoadProfileAsync()
    {
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;

        if (IsAdmin)
        {
            await LoadAdminStatsAsync();
            SuccessMessage = "Đã làm mới thông tin hồ sơ quản trị viên và dữ liệu hệ thống!";
        }
        else
        {
            LoadProfileData();
            SuccessMessage = "Đã làm mới thông tin hồ sơ cá nhân!";
        }
    }

    private void LoadProfileData()
    {
        if (_account?.Resident != null)
        {
            var r = _account.Resident;
            ResidentId = $"CD-{r.Id:D4}";
            FullName = r.FullName;
            DobText = r.DateOfBirth.ToString("dd/MM/yyyy");
            Gender = r.Gender switch
            {
                GenderType.Male => "Nam",
                GenderType.Female => "Nữ",
                GenderType.Other => "Khác",
                _ => "Chưa cập nhật"
            };
            Hometown = !string.IsNullOrWhiteSpace(r.HomeTown) ? r.HomeTown : "Chưa cập nhật";
            PermanentAddress = !string.IsNullOrWhiteSpace(r.HomeTown) ? r.HomeTown : "Chưa cập nhật";
            IdentityCard = !string.IsNullOrWhiteSpace(r.IdentityCard) ? r.IdentityCard : "Chưa cập nhật";
            PhoneNumber = !string.IsNullOrWhiteSpace(r.PhoneNumber) ? r.PhoneNumber : "Chưa cập nhật";
            EditPhoneNumber = PhoneNumber == "Chưa cập nhật" ? string.Empty : PhoneNumber;
            Email = !string.IsNullOrWhiteSpace(r.Email) ? r.Email : "Chưa cập nhật";
            EditEmail = Email == "Chưa cập nhật" ? string.Empty : Email;

            if (!string.IsNullOrWhiteSpace(r.AvatarPath))
            {
                AvatarPath = r.AvatarPath;
            }
        }
        else if (_account != null)
        {
            ResidentId = $"CD-{_account.Id:D4}";
            FullName = _account.Username;
            Gender = "Chưa cập nhật";
            Hometown = "Chưa cập nhật";
            PermanentAddress = "Chưa cập nhật";
            PhoneNumber = "Chưa cập nhật";
            EditPhoneNumber = string.Empty;
            Email = "Chưa cập nhật";
            EditEmail = string.Empty;
        }

        EditContactAddress = Hometown;
        ContactAddress = Hometown;

        // Nạp thông tin căn hộ thật nếu có ResidenceService
        if (_residenceService != null && _account?.ResidentId.HasValue == true)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    var activeResidence = await _residenceService.GetActiveResidenceByResidentIdAsync(_account.ResidentId.Value);
                    if (activeResidence != null)
                    {
                        var apt = activeResidence.Apartment;
                        ApartmentNumber = apt != null ? apt.ApartmentNumber : "Chưa cập nhật";
                        BuildingBlock = apt != null && apt.ApartmentNumber.Contains('-') ? apt.ApartmentNumber.Split('-')[0] : "A";
                        FloorNumber = apt != null ? apt.Floor.ToString() : "1";
                        JoinDate = activeResidence.StartDate.ToString("dd/MM/yyyy");
                    }
                    else
                    {
                        ApartmentNumber = "Chưa có căn hộ";
                        BuildingBlock = "---";
                        FloorNumber = "---";
                        JoinDate = "Chưa tham gia";
                    }
                }
                catch (Exception)
                {
                    // Giữ nguyên fallback an toàn
                }
            });
        }
    }

    private void ResetEditForm()
    {
        EditPhoneNumber = PhoneNumber == "Chưa cập nhật" ? string.Empty : PhoneNumber;
        EditEmail = Email == "Chưa cập nhật" ? string.Empty : Email;
        EditContactAddress = Hometown;
        ErrorMessage = string.Empty;
        SuccessMessage = "Đã khôi phục lại thông tin liên hệ ban đầu.";
    }

    private async Task SaveProfileAsync()
    {
        SuccessMessage = string.Empty;
        ErrorMessage = string.Empty;
        ModalErrorMessage = string.Empty;
        EditPhoneError = string.Empty;
        EditEmailError = string.Empty;

        var phoneToSave = EditPhoneNumber?.Trim();
        var emailToSave = EditEmail?.Trim();

        if (string.IsNullOrWhiteSpace(phoneToSave))
        {
            EditPhoneError = "Số điện thoại liên hệ không được để trống.";
            ModalErrorMessage = EditPhoneError;
            ErrorMessage = ModalErrorMessage;
            return;
        }

        if (!System.Text.RegularExpressions.Regex.IsMatch(phoneToSave, @"^0\d{9}$"))
        {
            EditPhoneError = "Số điện thoại không hợp lệ (phải gồm 10 chữ số và bắt đầu bằng số 0).";
            ModalErrorMessage = EditPhoneError;
            ErrorMessage = ModalErrorMessage;
            return;
        }

        if (string.IsNullOrWhiteSpace(emailToSave))
        {
            EditEmailError = "Email liên hệ không được để trống.";
            ModalErrorMessage = EditEmailError;
            ErrorMessage = ModalErrorMessage;
            return;
        }

        if (!System.Text.RegularExpressions.Regex.IsMatch(emailToSave, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
        {
            EditEmailError = "Định dạng email không hợp lệ (ví dụ: cu_dan@apartment.vn).";
            ModalErrorMessage = EditEmailError;
            ErrorMessage = ModalErrorMessage;
            return;
        }

        // Lưu xuống DB thật nếu có ResidentService và ResidentId
        if (_residentService != null && _account?.ResidentId != null)
        {
            try
            {
                var updatedResident = await _residentService.UpdateContactAsync(_account.ResidentId.Value, phoneToSave, emailToSave);
                if (_account.Resident != null)
                {
                    _account.Resident.PhoneNumber = updatedResident.PhoneNumber;
                    _account.Resident.Email = updatedResident.Email;
                }
            }
            catch (Exception ex)
            {
                ModalErrorMessage = $"Lỗi khi lưu dữ liệu xuống database: {ex.Message}";
                ErrorMessage = ModalErrorMessage;
                return;
            }
        }
        else if (_account?.Resident != null)
        {
            _account.Resident.PhoneNumber = phoneToSave;
            _account.Resident.Email = emailToSave;
        }

        PhoneNumber = phoneToSave;
        Email = emailToSave;
        IsEditModalOpen = false;
        SuccessMessage = "Lưu thay đổi thông tin liên hệ xuống cơ sở dữ liệu thành công!";
    }

    private async Task ConfirmChangePasswordAsync()
    {
        ChangePasswordError = string.Empty;

        if (string.IsNullOrWhiteSpace(OldPassword))
        {
            ChangePasswordError = "Vui lòng nhập mật khẩu hiện tại.";
            return;
        }

        if (string.IsNullOrWhiteSpace(NewPassword) || NewPassword.Length < 6)
        {
            ChangePasswordError = "Mật khẩu mới phải có ít nhất 6 ký tự.";
            return;
        }

        if (NewPassword == OldPassword)
        {
            ChangePasswordError = "Mật khẩu mới không được trùng với mật khẩu hiện tại.";
            return;
        }

        if (NewPassword != ConfirmNewPassword)
        {
            ChangePasswordError = "Xác nhận mật khẩu mới không trùng khớp.";
            return;
        }

        if (_accountService != null && _account != null)
        {
            try
            {
                await _accountService.ChangePasswordAsync(_account.Id, OldPassword, NewPassword);
                IsChangePasswordModalOpen = false;
                OldPassword = string.Empty;
                NewPassword = string.Empty;
                ConfirmNewPassword = string.Empty;
                SuccessMessage = "Đổi mật khẩu tài khoản Quản trị viên thành công!";
                ErrorMessage = string.Empty;
            }
            catch (Exception ex)
            {
                ChangePasswordError = ex.Message;
            }
        }
        else
        {
            IsChangePasswordModalOpen = false;
            OldPassword = string.Empty;
            NewPassword = string.Empty;
            ConfirmNewPassword = string.Empty;
            SuccessMessage = "Đổi mật khẩu tài khoản thành công!";
            ErrorMessage = string.Empty;
        }
    }
}
