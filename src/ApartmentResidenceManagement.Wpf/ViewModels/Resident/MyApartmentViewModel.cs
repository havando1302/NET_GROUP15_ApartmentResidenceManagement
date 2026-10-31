using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using ApartmentResidenceManagement.Domain.Entities;
using ApartmentResidenceManagement.Wpf.Commands;

namespace ApartmentResidenceManagement.Wpf.ViewModels;

public class ApartmentGalleryImageItem
{
    public string Title { get; set; } = string.Empty;
    public string ImagePath { get; set; } = string.Empty;
}

public class ApartmentResidentMemberItem
{
    public int STT { get; set; }
    public string AvatarIcon { get; set; } = "👤";
    public string? AvatarImagePath { get; set; }
    public bool HasAvatarImage => !string.IsNullOrEmpty(AvatarImagePath);
    public string FullName { get; set; } = string.Empty;
    public string Relationship { get; set; } = string.Empty;
    public string BirthDate { get; set; } = string.Empty;
    public string Status { get; set; } = "Đang cư trú";
    public string StatusForeground { get; set; } = "#16A34A";
    public string StatusBackground { get; set; } = "#DCFCE7";
}

public class MyApartmentViewModel : ViewModelBase
{
    private UserAccount? _account;

    // Thông tin căn hộ
    private string _apartmentNumber = "A-1203";
    private string _buildingBlock = "A";
    private string _floorNumber = "12";
    private string _apartmentArea = "72 m²";
    private int _familyCount = 4;

    // Ảnh căn hộ & gallery
    private string _currentApartmentImage = "/Assets/apartment_interior_1.png";

    // Thông tin tòa nhà
    private int _buildingFloors = 20;
    private int _buildingElevators = 4;
    private int _buildingTotalApartments = 120;

    // Tình trạng cư trú
    private string _residencyStartDate = "10/01/2026";
    private string _residencyDuration = "20 ngày";
    private string _residencyForm = "Cư trú thường trú";
    private string _residencyStatus = "Đang cư trú";

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

    public string ApartmentArea
    {
        get => _apartmentArea;
        set => SetProperty(ref _apartmentArea, value);
    }

    public int FamilyCount
    {
        get => _familyCount;
        set => SetProperty(ref _familyCount, value);
    }

    public string CurrentApartmentImage
    {
        get => _currentApartmentImage;
        set => SetProperty(ref _currentApartmentImage, value);
    }

    public ObservableCollection<ApartmentGalleryImageItem> GalleryImages { get; } = new()
    {
        new ApartmentGalleryImageItem { Title = "Phòng khách & Bếp", ImagePath = "/Assets/apartment_interior_1.png" },
        new ApartmentGalleryImageItem { Title = "Khu sinh hoạt chung", ImagePath = "/Assets/apartment_interior_2.png" },
        new ApartmentGalleryImageItem { Title = "Không gian thư giãn", ImagePath = "/Assets/apartment_interior_3.png" }
    };

    public int BuildingFloors
    {
        get => _buildingFloors;
        set => SetProperty(ref _buildingFloors, value);
    }

    public int BuildingElevators
    {
        get => _buildingElevators;
        set => SetProperty(ref _buildingElevators, value);
    }

    public int BuildingTotalApartments
    {
        get => _buildingTotalApartments;
        set => SetProperty(ref _buildingTotalApartments, value);
    }

    public string ResidencyStartDate
    {
        get => _residencyStartDate;
        set => SetProperty(ref _residencyStartDate, value);
    }

    public string ResidencyDuration
    {
        get => _residencyDuration;
        set => SetProperty(ref _residencyDuration, value);
    }

    public string ResidencyForm
    {
        get => _residencyForm;
        set => SetProperty(ref _residencyForm, value);
    }

    public string ResidencyStatus
    {
        get => _residencyStatus;
        set => SetProperty(ref _residencyStatus, value);
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

    public ObservableCollection<ApartmentResidentMemberItem> ResidentMembers { get; } = new();
    #endregion

    #region Commands
    public ICommand RefreshCommand { get; }
    public ICommand SelectGalleryImageCommand { get; }
    #endregion

    public MyApartmentViewModel()
    {
        RefreshCommand = new AsyncRelayCommand(_ => LoadApartmentDataAsync());
        SelectGalleryImageCommand = new RelayCommand(p =>
        {
            if (p is string path && !string.IsNullOrEmpty(path))
            {
                CurrentApartmentImage = path;
            }
            else if (p is ApartmentGalleryImageItem item)
            {
                CurrentApartmentImage = item.ImagePath;
            }
        });
        InitDefaultData();
    }

    private void InitDefaultData()
    {
        ResidentMembers.Clear();
        ResidentMembers.Add(new ApartmentResidentMemberItem
        {
            STT = 1,
            AvatarIcon = "👨",
            AvatarImagePath = "/Assets/resident_avatar.png",
            FullName = "Nguyễn Văn Minh",
            Relationship = "Chủ hộ",
            BirthDate = "15/03/2002",
            Status = "Đang cư trú"
        });
        ResidentMembers.Add(new ApartmentResidentMemberItem
        {
            STT = 2,
            AvatarIcon = "👩",
            AvatarImagePath = "/Assets/wife_avatar.png",
            FullName = "Trần Thị Hoa",
            Relationship = "Vợ",
            BirthDate = "20/07/2002",
            Status = "Đang cư trú"
        });
        ResidentMembers.Add(new ApartmentResidentMemberItem
        {
            STT = 3,
            AvatarIcon = "👦",
            FullName = "Nguyễn Gia Bảo",
            Relationship = "Con",
            BirthDate = "05/09/2018",
            Status = "Đang cư trú"
        });
        ResidentMembers.Add(new ApartmentResidentMemberItem
        {
            STT = 4,
            AvatarIcon = "👦",
            FullName = "Nguyễn Minh Anh",
            Relationship = "Con",
            BirthDate = "12/11/2022",
            Status = "Đang cư trú"
        });
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
            InitDefaultData();
            ErrorMessage = string.Empty;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Lỗi khi tải thông tin căn hộ: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }

        return Task.CompletedTask;
    }
}
