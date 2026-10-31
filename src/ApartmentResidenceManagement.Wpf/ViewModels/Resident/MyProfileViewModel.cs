using System;
using System.Threading.Tasks;
using System.Windows.Input;
using ApartmentResidenceManagement.Domain.Entities;
using ApartmentResidenceManagement.Wpf.Commands;

namespace ApartmentResidenceManagement.Wpf.ViewModels;

public class MyProfileViewModel : ViewModelBase
{
    private UserAccount? _account;

    // Trường thông tin cá nhân
    private string _residentId = "CD-0001";
    private string _fullName = "Nguyễn Văn Minh";
    private string _dobText = "15/04/2002";
    private bool _isMale = true;
    private bool _isFemale = false;
    private string _identityCard = "001202012345";
    private string _phoneNumber = "0987 654 321";
    private string _email = "minhnguyen@gmail.com";
    private string _contactAddress = "Căn hộ A-1203, Tòa A, Chung cư ResidenceManager";
    private string _note = string.Empty;
    private string _apartmentSubtitle = "Cư dân căn hộ A-1203";

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

    public string DobText
    {
        get => _dobText;
        set => SetProperty(ref _dobText, value);
    }

    public bool IsMale
    {
        get => _isMale;
        set
        {
            if (SetProperty(ref _isMale, value) && value)
            {
                IsFemale = false;
            }
        }
    }

    public bool IsFemale
    {
        get => _isFemale;
        set
        {
            if (SetProperty(ref _isFemale, value) && value)
            {
                IsMale = false;
            }
        }
    }

    public string IdentityCard
    {
        get => _identityCard;
        set => SetProperty(ref _identityCard, value);
    }

    public string PhoneNumber
    {
        get => _phoneNumber;
        set => SetProperty(ref _phoneNumber, value);
    }

    public string Email
    {
        get => _email;
        set => SetProperty(ref _email, value);
    }

    public string ContactAddress
    {
        get => _contactAddress;
        set => SetProperty(ref _contactAddress, value);
    }

    public string Note
    {
        get => _note;
        set => SetProperty(ref _note, value);
    }

    public string ApartmentSubtitle
    {
        get => _apartmentSubtitle;
        set => SetProperty(ref _apartmentSubtitle, value);
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
    #endregion

    #region Commands
    public ICommand SaveProfileCommand { get; }
    public ICommand UpdateProfileCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand UpdatePhotoCommand { get; }
    #endregion

    public MyProfileViewModel()
    {
        SaveProfileCommand = new AsyncRelayCommand(_ => SaveProfileAsync());
        UpdateProfileCommand = SaveProfileCommand;
        RefreshCommand = new AsyncRelayCommand(_ => LoadProfileAsync());
        UpdatePhotoCommand = new RelayCommand(_ =>
        {
            SuccessMessage = "Tính năng tải lên ảnh đại diện mới đang được xử lý.";
            ErrorMessage = string.Empty;
        });
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
        SuccessMessage = "Đã làm mới thông tin cá nhân!";
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
            IdentityCard = "001202012345";
            DobText = "15/04/2002";
            IsMale = true;
            IsFemale = false;
            Email = !string.IsNullOrWhiteSpace(r.Email) ? r.Email : $"{_account.Username}@gmail.com";
            ContactAddress = "Căn hộ A-1203, Tòa A, Chung cư ResidenceManager";
            ApartmentSubtitle = "Cư dân căn hộ A-1203";
        }
        else if (_account != null)
        {
            ResidentId = $"CD-{_account.Id:D4}";
            FullName = "Nguyễn Văn Minh";
            PhoneNumber = "0987 654 321";
            IdentityCard = "001202012345";
            DobText = "15/04/2002";
            IsMale = true;
            IsFemale = false;
            Email = "minhnguyen@gmail.com";
            ContactAddress = "Căn hộ A-1203, Tòa A, Chung cư ResidenceManager";
            ApartmentSubtitle = "Cư dân căn hộ A-1203";
        }
    }

    private Task SaveProfileAsync()
    {
        SuccessMessage = string.Empty;
        ErrorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(FullName))
        {
            ErrorMessage = "Họ và tên không được để trống.";
            return Task.CompletedTask;
        }

        if (string.IsNullOrWhiteSpace(PhoneNumber))
        {
            ErrorMessage = "Số điện thoại liên hệ không được để trống.";
            return Task.CompletedTask;
        }

        if (_account?.Resident != null)
        {
            _account.Resident.FullName = FullName;
            _account.Resident.PhoneNumber = PhoneNumber;
        }

        SuccessMessage = "Cập nhật thông tin cá nhân thành công!";
        return Task.CompletedTask;
    }
}
