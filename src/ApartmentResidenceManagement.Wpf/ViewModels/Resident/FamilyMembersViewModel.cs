using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using ApartmentResidenceManagement.Domain.Entities;
using ApartmentResidenceManagement.Wpf.Commands;

namespace ApartmentResidenceManagement.Wpf.ViewModels;

public class AccordionFamilyMemberItem : ViewModelBase
{
    private bool _isExpanded;
    private string _phoneNumber = string.Empty;
    private string _hometown = string.Empty;
    private string _status = "Đang cư trú";

    public string AvatarIcon { get; set; } = "👨";
    public string? AvatarImagePath { get; set; }
    public bool HasAvatarImage => !string.IsNullOrEmpty(AvatarImagePath);
    public string FullName { get; set; } = string.Empty;
    public string Relationship { get; set; } = string.Empty;
    public bool IsOwner { get; set; }
    public string BirthDate { get; set; } = string.Empty;
    public string Gender { get; set; } = "Nam";
    public string IdentityCard { get; set; } = string.Empty;

    public string Hometown
    {
        get => _hometown;
        set => SetProperty(ref _hometown, value);
    }

    public string PhoneNumber
    {
        get => _phoneNumber;
        set => SetProperty(ref _phoneNumber, value);
    }

    public string Status
    {
        get => _status;
        set => SetProperty(ref _status, value);
    }

    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            if (SetProperty(ref _isExpanded, value))
            {
                OnPropertyChanged(nameof(ExpandIcon));
            }
        }
    }

    public string ExpandIcon => IsExpanded ? "ᐱ" : "ᐯ";

    public ICommand ToggleExpandCommand { get; }

    public AccordionFamilyMemberItem()
    {
        ToggleExpandCommand = new RelayCommand(_ => IsExpanded = !IsExpanded);
    }
}

public class FamilyMembersViewModel : ViewModelBase
{
    private UserAccount? _account;
    private string _apartmentNumber = "A-1203";
    private string _buildingBlock = "A";
    private string _floorNumber = "12";
    private string _apartmentArea = "72 m²";
    private string _searchText = string.Empty;
    private bool _isLoading;
    private string _errorMessage = string.Empty;

    // Chỉnh sửa thông tin thành viên
    private bool _isEditMemberModalOpen;
    private AccordionFamilyMemberItem? _editingMember;
    private string _editMemberFullName = string.Empty;
    private string _editMemberRelationship = string.Empty;
    private string _editMemberPhone = string.Empty;
    private string _editMemberHometown = string.Empty;
    private string _editMemberStatus = "Đang cư trú";
    private string _editSuccessMessage = string.Empty;
    private string _editErrorMessage = string.Empty;

    private readonly ObservableCollection<AccordionFamilyMemberItem> _allMembers = new();

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

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                FilterMembers();
            }
        }
    }

    public int TotalMembers => FilteredMembers.Count;

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

    public bool IsEditMemberModalOpen
    {
        get => _isEditMemberModalOpen;
        set => SetProperty(ref _isEditMemberModalOpen, value);
    }

    public string EditMemberFullName
    {
        get => _editMemberFullName;
        set => SetProperty(ref _editMemberFullName, value);
    }

    public string EditMemberRelationship
    {
        get => _editMemberRelationship;
        set => SetProperty(ref _editMemberRelationship, value);
    }

    public string EditMemberPhone
    {
        get => _editMemberPhone;
        set => SetProperty(ref _editMemberPhone, value);
    }

    public string EditMemberHometown
    {
        get => _editMemberHometown;
        set => SetProperty(ref _editMemberHometown, value);
    }

    public string EditMemberStatus
    {
        get => _editMemberStatus;
        set => SetProperty(ref _editMemberStatus, value);
    }

    public string EditSuccessMessage
    {
        get => _editSuccessMessage;
        set => SetProperty(ref _editSuccessMessage, value);
    }

    public string EditErrorMessage
    {
        get => _editErrorMessage;
        set => SetProperty(ref _editErrorMessage, value);
    }

    public ObservableCollection<string> AvailableStatuses { get; } = new()
    {
        "Đang cư trú",
        "Tạm vắng",
        "Tạm trú"
    };

    public ObservableCollection<AccordionFamilyMemberItem> FilteredMembers { get; } = new();
    public bool HasMembers => FilteredMembers.Count > 0;
    #endregion

    #region Commands
    public ICommand RefreshCommand { get; }
    public ICommand OpenEditMemberModalCommand { get; }
    public ICommand CloseEditMemberModalCommand { get; }
    public ICommand SaveMemberEditCommand { get; }
    #endregion

    public FamilyMembersViewModel()
    {
        RefreshCommand = new AsyncRelayCommand(_ => LoadFamilyMembersAsync());

        OpenEditMemberModalCommand = new RelayCommand(p =>
        {
            if (p is AccordionFamilyMemberItem member)
            {
                _editingMember = member;
                EditMemberFullName = member.FullName;
                EditMemberRelationship = member.Relationship;
                EditMemberPhone = member.PhoneNumber == "---" ? "" : member.PhoneNumber;
                EditMemberHometown = member.Hometown;
                EditMemberStatus = member.Status;
                EditErrorMessage = string.Empty;
                IsEditMemberModalOpen = true;
            }
        });

        CloseEditMemberModalCommand = new RelayCommand(_ =>
        {
            IsEditMemberModalOpen = false;
            _editingMember = null;
            EditErrorMessage = string.Empty;
        });

        SaveMemberEditCommand = new RelayCommand(_ =>
        {
            if (_editingMember == null) return;

            if (!string.IsNullOrWhiteSpace(EditMemberPhone) && EditMemberPhone.Trim().Length < 8)
            {
                EditErrorMessage = "Số điện thoại không hợp lệ (tối thiểu 8 chữ số).";
                return;
            }

            _editingMember.PhoneNumber = string.IsNullOrWhiteSpace(EditMemberPhone) ? "---" : EditMemberPhone.Trim();
            _editingMember.Status = EditMemberStatus;

            IsEditMemberModalOpen = false;
            EditSuccessMessage = $"Cập nhật thông tin thành viên {_editingMember.FullName} thành công!";
            _editingMember = null;
            FilterMembers();
        });

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
        _allMembers.Clear();
        _allMembers.Add(new AccordionFamilyMemberItem
        {
            AvatarIcon = "👨",
            AvatarImagePath = "/Assets/resident_avatar.png",
            FullName = "Nguyễn Văn Minh",
            Relationship = "Chủ hộ",
            IsOwner = true,
            BirthDate = "15/03/2002",
            Gender = "Nam",
            IdentityCard = "012345678901",
            Hometown = "Đan Phượng, Hà Nội",
            PhoneNumber = "0987 654 321",
            Status = "Đang cư trú",
            IsExpanded = true
        });

        _allMembers.Add(new AccordionFamilyMemberItem
        {
            AvatarIcon = "👩",
            AvatarImagePath = "/Assets/wife_avatar.png",
            FullName = "Trần Thị Hoa",
            Relationship = "Vợ",
            IsOwner = false,
            BirthDate = "20/07/2002",
            Gender = "Nữ",
            IdentityCard = "012345678902",
            Hometown = "Thạch Thất, Hà Nội",
            PhoneNumber = "0987 654 322",
            Status = "Đang cư trú",
            IsExpanded = false
        });

        _allMembers.Add(new AccordionFamilyMemberItem
        {
            AvatarIcon = "👦",
            AvatarImagePath = "/Assets/avatar_son.png",
            FullName = "Nguyễn Gia Bảo",
            Relationship = "Con",
            IsOwner = false,
            BirthDate = "05/09/2018",
            Gender = "Nam",
            IdentityCard = "---",
            Hometown = "Đan Phượng, Hà Nội",
            PhoneNumber = "---",
            Status = "Đang cư trú",
            IsExpanded = false
        });

        _allMembers.Add(new AccordionFamilyMemberItem
        {
            AvatarIcon = "👧",
            AvatarImagePath = "/Assets/avatar_daughter.png",
            FullName = "Nguyễn An Nhiên",
            Relationship = "Con",
            IsOwner = false,
            BirthDate = "12/11/2022",
            Gender = "Nữ",
            IdentityCard = "---",
            Hometown = "Đan Phượng, Hà Nội",
            PhoneNumber = "---",
            Status = "Đang cư trú",
            IsExpanded = false
        });

        FilterMembers();
    }

    private void FilterMembers()
    {
        FilteredMembers.Clear();
        var query = _allMembers.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            string keyword = SearchText.Trim().ToLowerInvariant();
            query = query.Where(m =>
                m.FullName.ToLowerInvariant().Contains(keyword) ||
                m.Relationship.ToLowerInvariant().Contains(keyword) ||
                m.IdentityCard.ToLowerInvariant().Contains(keyword) ||
                m.PhoneNumber.ToLowerInvariant().Contains(keyword));
        }

        foreach (var item in query)
        {
            FilteredMembers.Add(item);
        }

        OnPropertyChanged(nameof(TotalMembers));
        OnPropertyChanged(nameof(HasMembers));
    }
}
