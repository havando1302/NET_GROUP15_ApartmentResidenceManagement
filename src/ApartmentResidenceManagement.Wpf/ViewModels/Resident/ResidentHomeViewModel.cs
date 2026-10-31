using System;
using System.Threading.Tasks;
using System.Windows.Input;
using ApartmentResidenceManagement.Domain.Entities;
using ApartmentResidenceManagement.Wpf.Commands;

namespace ApartmentResidenceManagement.Wpf.ViewModels;

public class ResidentHomeViewModel : ViewModelBase
{
    private UserAccount? _account;

    private string _residentName = string.Empty;
    private string _apartmentNumber = "Chưa có";
    private string _relationshipText = "Chưa rõ";
    private int _familyCount = 0;
    private int _vehicleCount = 0;
    private string _residencyStatus = "Chưa cư trú";
    private string _errorMessage = string.Empty;
    private bool _isLoading;

    #region Properties
    public string ResidentName
    {
        get => _residentName;
        set => SetProperty(ref _residentName, value);
    }

    public string ApartmentNumber
    {
        get => _apartmentNumber;
        set => SetProperty(ref _apartmentNumber, value);
    }

    public string RelationshipText
    {
        get => _relationshipText;
        set => SetProperty(ref _relationshipText, value);
    }

    public int FamilyCount
    {
        get => _familyCount;
        set => SetProperty(ref _familyCount, value);
    }

    public int VehicleCount
    {
        get => _vehicleCount;
        set => SetProperty(ref _vehicleCount, value);
    }

    public string ResidencyStatus
    {
        get => _residencyStatus;
        set => SetProperty(ref _residencyStatus, value);
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }
    #endregion

    public ICommand RefreshCommand { get; }

    public ResidentHomeViewModel()
    {
        RefreshCommand = new AsyncRelayCommand(_ => LoadStatsAsync());
    }

    public void Initialize(UserAccount account)
    {
        _account = account;
        UpdateFromAccount();
    }

    private void UpdateFromAccount()
    {
        if (_account == null) return;

        if (_account.Resident != null)
        {
            ResidentName = _account.Resident.FullName;
            ResidencyStatus = "Đang cư trú";
            RelationshipText = "Chủ hộ";
            ApartmentNumber = "P.102"; // Mặc định hiển thị căn hộ của cư dân mẫu
            FamilyCount = 1;
        }
        else
        {
            ResidentName = _account.Username;
            ResidencyStatus = "Đang cư trú";
            RelationshipText = "Cư dân";
            ApartmentNumber = "P.102";
            FamilyCount = 1;
        }
    }

    public Task LoadStatsAsync()
    {
        IsLoading = true;
        try
        {
            UpdateFromAccount();
            ErrorMessage = string.Empty;
        }
        catch (Exception)
        {
            ErrorMessage = "Không thể làm mới thông tin tổng quan.";
        }
        finally
        {
            IsLoading = false;
        }
        return Task.CompletedTask;
    }
}
