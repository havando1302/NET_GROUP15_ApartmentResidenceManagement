using System;
using System.Threading.Tasks;
using System.Windows.Input;
using ApartmentResidenceManagement.Domain.Entities;
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

    // Trạng thái thông báo
    private string _successMessage = string.Empty;
    private string _errorMessage = string.Empty;

    #region Properties
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
        set => SetProperty(ref _editPhoneNumber, value);
    }

    public string ContactAddress
    {
        get => _contactAddress;
        set => SetProperty(ref _editContactAddress, value);
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
        set => SetProperty(ref _errorMessage, value);
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
        new AvatarPresetItem { Name = "Nguyễn Văn Hùng", Role = "Ông nội", ImagePath = "/Assets/avatar_grandfather.png" },
        new AvatarPresetItem { Name = "Lê Thị Mai", Role = "Bà nội", ImagePath = "/Assets/avatar_grandmother.png" },
        new AvatarPresetItem { Name = "Nguyễn Tuấn Anh", Role = "Em trai", ImagePath = "/Assets/avatar_young_man.png" }
    };
    #endregion

    #region Commands
    public ICommand SaveProfileCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand ResetCommand { get; }
    public ICommand UpdatePhotoCommand { get; }
    public ICommand ToggleAvatarPickerCommand { get; }
    public ICommand SelectAvatarCommand { get; }
    #endregion

    public MyProfileViewModel()
    {
        SaveProfileCommand = new AsyncRelayCommand(_ => SaveProfileAsync());
        RefreshCommand = new AsyncRelayCommand(_ => LoadProfileAsync());
        ResetCommand = new RelayCommand(_ => ResetEditForm());
        ToggleAvatarPickerCommand = new RelayCommand(_ => IsAvatarPickerOpen = !IsAvatarPickerOpen);
        SelectAvatarCommand = new RelayCommand(p =>
        {
            if (p is string path && !string.IsNullOrEmpty(path))
            {
                AvatarPath = path;
                IsAvatarPickerOpen = false;
                SuccessMessage = "Đã cập nhật ảnh đại diện thành công!";
                ErrorMessage = string.Empty;
            }
            else if (p is AvatarPresetItem item)
            {
                AvatarPath = item.ImagePath;
                IsAvatarPickerOpen = false;
                SuccessMessage = $"Đã đổi ảnh đại diện sang {item.Name} ({item.Role}) thành công!";
                ErrorMessage = string.Empty;
            }
        });
        UpdatePhotoCommand = ToggleAvatarPickerCommand;
    }

    public void Initialize(UserAccount account)
    {
        _account = account;
        LoadProfileData();
    }

    public Task LoadProfileAsync()
    {
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;
        LoadProfileData();
        SuccessMessage = "Đã làm mới thông tin hồ sơ cá nhân!";
        return Task.CompletedTask;
    }

    private void LoadProfileData()
    {
        if (_account?.Resident != null)
        {
            var r = _account.Resident;
            ResidentId = $"CD-{r.Id:D4}";
            FullName = r.FullName;
            PhoneNumber = !string.IsNullOrWhiteSpace(r.PhoneNumber) ? r.PhoneNumber : "0987 654 321";
            EditPhoneNumber = PhoneNumber;
            Email = !string.IsNullOrWhiteSpace(r.Email) ? r.Email : "minhnv@gmail.com";
        }
        else if (_account != null)
        {
            ResidentId = $"CD-{_account.Id:D4}";
            FullName = "Nguyễn Văn Minh";
            PhoneNumber = "0987 654 321";
            EditPhoneNumber = PhoneNumber;
            Email = $"{_account.Username}@gmail.com";
        }

        EditContactAddress = Hometown;
        ContactAddress = Hometown;
    }

    private void ResetEditForm()
    {
        EditPhoneNumber = PhoneNumber;
        EditContactAddress = Hometown;
        ErrorMessage = string.Empty;
        SuccessMessage = "Đã khôi phục lại thông tin liên hệ ban đầu.";
    }

    private Task SaveProfileAsync()
    {
        SuccessMessage = string.Empty;
        ErrorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(EditPhoneNumber))
        {
            ErrorMessage = "Số điện thoại liên hệ không được để trống.";
            return Task.CompletedTask;
        }

        PhoneNumber = EditPhoneNumber.Trim();
        if (!string.IsNullOrWhiteSpace(EditContactAddress))
        {
            Hometown = EditContactAddress.Trim();
        }

        if (_account?.Resident != null)
        {
            _account.Resident.PhoneNumber = PhoneNumber;
        }

        SuccessMessage = "Lưu thay đổi thông tin liên hệ thành công!";
        return Task.CompletedTask;
    }
}
