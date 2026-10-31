using System;
using System.Threading.Tasks;
using System.Windows.Input;
using ApartmentResidenceManagement.Domain.Entities;
using ApartmentResidenceManagement.Domain.Enums;
using ApartmentResidenceManagement.Wpf.Commands;

namespace ApartmentResidenceManagement.Wpf.ViewModels;

public class MyApartmentViewModel : ViewModelBase
{
    private UserAccount? _account;

    // Chi tiết Căn hộ
    private string _apartmentNumber = "Chưa đăng ký";
    private string _floorText = "Chưa rõ";
    private string _areaText = "Chưa rõ";
    private string _apartmentStatusText = "Chưa rõ";
    
    // Chi tiết Cư trú
    private string _relationshipText = "Chưa rõ";
    private string _startDateText = "Chưa rõ";
    private bool _hasApartment;
    private bool _isLoading;
    private string _errorMessage = string.Empty;

    #region Properties
    public string ApartmentNumber
    {
        get => _apartmentNumber;
        set => SetProperty(ref _apartmentNumber, value);
    }

    public string FloorText
    {
        get => _floorText;
        set => SetProperty(ref _floorText, value);
    }

    public string AreaText
    {
        get => _areaText;
        set => SetProperty(ref _areaText, value);
    }

    public string ApartmentStatusText
    {
        get => _apartmentStatusText;
        set => SetProperty(ref _apartmentStatusText, value);
    }

    public string RelationshipText
    {
        get => _relationshipText;
        set => SetProperty(ref _relationshipText, value);
    }

    public string StartDateText
    {
        get => _startDateText;
        set => SetProperty(ref _startDateText, value);
    }

    public bool HasApartment
    {
        get => _hasApartment;
        set => SetProperty(ref _hasApartment, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }
    #endregion

    public ICommand RefreshCommand { get; }

    public MyApartmentViewModel()
    {
        RefreshCommand = new AsyncRelayCommand(_ => LoadApartmentDataAsync());
    }

    public void Initialize(UserAccount account)
    {
        _account = account;
        _ = LoadApartmentDataAsync();
    }

    public Task LoadApartmentDataAsync()
    {
        IsLoading = true;
        try
        {
            if (_account?.Resident != null)
            {
                // Cư dân mẫu đã có căn hộ liên kết
                ApartmentNumber = "P.102";
                FloorText = "Tầng 1";
                AreaText = "65.5 m²";
                ApartmentStatusText = "Đang Cư Trú (Occupied)";
                RelationshipText = "Chủ Hộ (Owner)";
                StartDateText = "01/01/2026";
                HasApartment = true;
                ErrorMessage = string.Empty;
            }
            else
            {
                // Tài khoản không có thông tin cư dân
                HasApartment = false;
                ErrorMessage = string.Empty;
            }
        }
        catch (Exception ex)
        {
            HasApartment = false;
            ErrorMessage = $"Lỗi khi tải thông tin căn hộ: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }

        return Task.CompletedTask;
    }
}
