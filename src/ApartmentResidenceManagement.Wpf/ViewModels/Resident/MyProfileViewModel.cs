using System;
using System.Threading.Tasks;
using System.Windows.Input;
using ApartmentResidenceManagement.Domain.Entities;
using ApartmentResidenceManagement.Wpf.Commands;

namespace ApartmentResidenceManagement.Wpf.ViewModels;

public class MyProfileViewModel : ViewModelBase
{
    private UserAccount? _account;

    // Các thuộc tính chỉ xem
    private string _residentId = "CD-0001";
    private string _fullName = "Nguyễn Văn A";
    private string _dobText = "15/08/1995";
    private string _genderText = "Nam";
    private string _identityCard = "001200012345";

    // Các thuộc tính có thể sửa
    private string _phoneNumber = "0901234567";
    private string _homeTown = "Hà Nội";

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

    public string GenderText
    {
        get => _genderText;
        set => SetProperty(ref _genderText, value);
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

    public string HomeTown
    {
        get => _homeTown;
        set => SetProperty(ref _homeTown, value);
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

    public ICommand SaveProfileCommand { get; }

    public MyProfileViewModel()
    {
        SaveProfileCommand = new AsyncRelayCommand(_ => SaveProfileAsync());
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
        return Task.CompletedTask;
    }

    private void LoadProfileData()
    {
        if (_account?.Resident != null)
        {
            var r = _account.Resident;
            ResidentId = $"CD-{r.Id:D4}";
            FullName = r.FullName;
            PhoneNumber = r.PhoneNumber ?? "0901234567";
            IdentityCard = "001200012345";
            DobText = "15/08/1995";
            GenderText = "Nam";
            HomeTown = "Hà Nội";
        }
        else if (_account != null)
        {
            ResidentId = $"CD-{_account.Id:D4}";
            FullName = _account.Username;
            PhoneNumber = "0901234567";
            IdentityCard = "001200012345";
            DobText = "15/08/1995";
            GenderText = "Nam";
            HomeTown = "Hà Nội";
        }
    }

    private Task SaveProfileAsync()
    {
        SuccessMessage = string.Empty;
        ErrorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(PhoneNumber))
        {
            ErrorMessage = "Số điện thoại liên hệ không được để trống.";
            return Task.CompletedTask;
        }

        if (_account?.Resident != null)
        {
            _account.Resident.PhoneNumber = PhoneNumber;
        }

        SuccessMessage = "Cập nhật thông tin liên hệ thành công!";
        return Task.CompletedTask;
    }
}
