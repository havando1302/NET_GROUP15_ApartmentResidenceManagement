using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using ApartmentResidenceManagement.Domain.Entities;
using ApartmentResidenceManagement.Wpf.Commands;

namespace ApartmentResidenceManagement.Wpf.ViewModels;

public class FamilyMemberModelItem
{
    public int STT { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Relationship { get; set; } = string.Empty;
    public string BirthDate { get; set; } = string.Empty;
    public string IdentityCard { get; set; } = string.Empty;
    public string Status { get; set; } = "Đang cư trú";
    public string StatusForeground { get; set; } = "#16A34A";
    public string StatusBackground { get; set; } = "#DCFCE7";
}

public class FamilyMembersViewModel : ViewModelBase
{
    private UserAccount? _account;
    private string _apartmentNumber = "A-1203";
    private string _buildingBlock = "A";
    private string _floorNumber = "12";
    private bool _isLoading;
    private string _errorMessage = string.Empty;

    #region Properties
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

    public int TotalMembers => Members.Count;

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

    public ObservableCollection<FamilyMemberModelItem> Members { get; } = new();
    public bool HasMembers => Members.Count > 0;
    #endregion

    #region Commands
    public ICommand RefreshCommand { get; }
    #endregion

    public FamilyMembersViewModel()
    {
        RefreshCommand = new AsyncRelayCommand(_ => LoadFamilyMembersAsync());
        InitDefaultMembers();
    }

    public void Initialize(UserAccount account)
    {
        _account = account;
        _ = LoadFamilyMembersAsync();
    }

    public Task LoadFamilyMembersAsync()
    {
        IsLoading = true;
        try
        {
            InitDefaultMembers();
            ErrorMessage = string.Empty;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Lỗi khi tải danh sách thành viên: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }

        return Task.CompletedTask;
    }

    private void InitDefaultMembers()
    {
        Members.Clear();
        Members.Add(new FamilyMemberModelItem
        {
            STT = 1,
            FullName = "Nguyễn Văn Minh",
            Relationship = "Chủ hộ",
            BirthDate = "15/04/2002",
            IdentityCard = "001202012345",
            Status = "Đang cư trú",
            StatusForeground = "#16A34A",
            StatusBackground = "#DCFCE7"
        });
        Members.Add(new FamilyMemberModelItem
        {
            STT = 2,
            FullName = "Trần Thị Thu Hà",
            Relationship = "Vợ",
            BirthDate = "20/08/2003",
            IdentityCard = "001203045678",
            Status = "Đang cư trú",
            StatusForeground = "#16A34A",
            StatusBackground = "#DCFCE7"
        });
        Members.Add(new FamilyMemberModelItem
        {
            STT = 3,
            FullName = "Nguyễn Minh Quân",
            Relationship = "Con",
            BirthDate = "10/03/2025",
            IdentityCard = "---",
            Status = "Đang cư trú",
            StatusForeground = "#16A34A",
            StatusBackground = "#DCFCE7"
        });

        OnPropertyChanged(nameof(TotalMembers));
        OnPropertyChanged(nameof(HasMembers));
    }
}
