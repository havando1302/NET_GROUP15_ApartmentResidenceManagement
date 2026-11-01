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
    private string _apartmentNumber = "---";
    private string _buildingBlock = "---";
    private string _floorNumber = "---";
    private string _apartmentArea = "---";
    private int _familyCount = 0;

    // Ảnh căn hộ & gallery
    private string _currentApartmentImage = "/Assets/apartment_interior_1.png";

    // Thông tin tòa nhà
    private int _buildingFloors = 20;
    private int _buildingElevators = 2;
    private int _buildingTotalApartments = 50;

    // Tình trạng cư trú
    private string _residencyStartDate = "---";
    private string _residencyEndDate = "---";
    private string _residencyDuration = "---";
    private string _residencyForm = "---";
    private string _residencyStatus = "Chưa có cư trú hoạt động";

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

    public string ResidencyEndDate
    {
        get => _residencyEndDate;
        set => SetProperty(ref _residencyEndDate, value);
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

    private readonly Application.Services.ResidenceService? _residenceService;

    public MyApartmentViewModel() : this(null)
    {
    }

    public MyApartmentViewModel(Application.Services.ResidenceService? residenceService)
    {
        _residenceService = residenceService;
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
    }

    public void Initialize(UserAccount account)
    {
        _account = account;
        _ = LoadApartmentDataAsync();
    }

    public async Task LoadApartmentDataAsync()
    {
        IsLoading = true;
        ErrorMessage = string.Empty;

        try
        {
            if (_residenceService != null && _account?.ResidentId.HasValue == true)
            {
                var activeResidence = await _residenceService.GetActiveResidenceByResidentIdAsync(_account.ResidentId.Value);
                if (activeResidence != null)
                {
                    var apt = activeResidence.Apartment;
                    ApartmentNumber = apt != null ? apt.ApartmentNumber : "Chưa cập nhật";
                    BuildingBlock = apt != null && apt.ApartmentNumber.Contains('-') ? apt.ApartmentNumber.Split('-')[0] : "A";
                    FloorNumber = apt != null ? apt.Floor.ToString() : "1";
                    ApartmentArea = apt != null ? $"{apt.Area} m²" : "0 m²";

                    var startDate = activeResidence.StartDate.Date;
                    int days = Math.Max(1, (int)(DateTime.Today - startDate).TotalDays);
                    ResidencyStartDate = startDate.ToString("dd/MM/yyyy");
                    ResidencyEndDate = activeResidence.EndDate.HasValue ? activeResidence.EndDate.Value.ToString("dd/MM/yyyy") : "Chưa xác định";
                    ResidencyDuration = $"{days} ngày (tính đến hôm nay)";
                    ResidencyStatus = "Đang cư trú";

                    var allActive = await _residenceService.GetActiveResidencesByApartmentIdAsync(activeResidence.ApartmentId);
                    ResidentMembers.Clear();
                    int stt = 1;
                    foreach (var memberRes in allActive)
                    {
                        var res = memberRes.Resident;
                        string relationName = memberRes.RelationshipType switch
                        {
                            Domain.Enums.RelationshipType.Owner => "Chủ hộ",
                            Domain.Enums.RelationshipType.FamilyMember => "Thành viên gia đình",
                            Domain.Enums.RelationshipType.Tenant => "Người thuê",
                            Domain.Enums.RelationshipType.Temporary => "Tạm trú",
                            _ => "Cư dân"
                        };

                        ResidentMembers.Add(new ApartmentResidentMemberItem
                        {
                            STT = stt++,
                            AvatarIcon = memberRes.RelationshipType == Domain.Enums.RelationshipType.Owner ? "👨" : "👤",
                            AvatarImagePath = memberRes.RelationshipType == Domain.Enums.RelationshipType.Owner ? "/Assets/resident_avatar.png" : null,
                            FullName = res != null ? res.FullName : "Thành viên",
                            Relationship = relationName,
                            BirthDate = res != null ? res.DateOfBirth.ToString("dd/MM/yyyy") : "---",
                            Status = "Đang cư trú"
                        });
                    }

                    FamilyCount = ResidentMembers.Count;
                }
                else
                {
                    ApartmentNumber = "Chưa có căn hộ đang cư trú";
                    BuildingBlock = "---";
                    FloorNumber = "---";
                    ApartmentArea = "0 m²";
                    FamilyCount = 0;
                    ResidencyStartDate = "---";
                    ResidencyEndDate = "---";
                    ResidencyDuration = "---";
                    ResidencyStatus = "Chưa có cư trú hoạt động";
                    ResidentMembers.Clear();
                }
            }
            else
            {
                ApartmentNumber = "Chưa có căn hộ đang cư trú";
                BuildingBlock = "---";
                FloorNumber = "---";
                ApartmentArea = "0 m²";
                FamilyCount = 0;
                ResidencyStartDate = "---";
                ResidencyEndDate = "---";
                ResidencyDuration = "---";
                ResidencyStatus = "Chưa có cư trú hoạt động";
                ResidentMembers.Clear();
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Lỗi khi tải thông tin căn hộ: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
}
