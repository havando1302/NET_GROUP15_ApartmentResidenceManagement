using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Input;
using ApartmentResidenceManagement.Application.Services;
using ApartmentResidenceManagement.Domain.Entities;
using ApartmentResidenceManagement.Domain.Enums;
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

public class MemberRegistrationRequestItemViewModel : ViewModelBase
{
    public int Id { get; set; }
    public int ApartmentId { get; set; }
    public string ApartmentNumber { get; set; } = string.Empty;
    public int RequesterResidentId { get; set; }
    public string RequesterName { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;
    public string? AvatarImagePath { get; set; }
    public bool HasAvatarImage => !string.IsNullOrEmpty(AvatarImagePath);
    public DateTime DateOfBirth { get; set; }
    public string BirthDateText => DateOfBirth.ToString("dd/MM/yyyy");
    public string? IdentityCard { get; set; }
    public string IdentityCardText => !string.IsNullOrWhiteSpace(IdentityCard) ? IdentityCard : "---";
    public string? PhoneNumber { get; set; }
    public string PhoneNumberText => !string.IsNullOrWhiteSpace(PhoneNumber) ? PhoneNumber : "---";
    public string? Email { get; set; }
    public RelationshipType RelationshipType { get; set; }
    public string RelationshipText { get; set; } = string.Empty;

    public RequestStatus Status { get; set; }
    public string StatusText { get; set; } = string.Empty;
    public string StatusBadgeBg { get; set; } = "#FEF3C7";
    public string StatusBadgeFg { get; set; } = "#D97706";
    public string StatusBadgeBorder { get; set; } = "#FDE68A";

    public DateTime CreatedAt { get; set; }
    public string CreatedAtText => CreatedAt.ToString("dd/MM/yyyy");
    public DateTime? ProcessedAt { get; set; }
    public string? RejectReason { get; set; }
    public bool HasRejectReason => !string.IsNullOrWhiteSpace(RejectReason);
    public bool IsPending => Status == RequestStatus.Pending;
}

public class RelationshipOption
{
    public RelationshipType Value { get; set; }
    public string DisplayName { get; set; } = string.Empty;
}

public class FamilyMembersViewModel : ViewModelBase
{
    private UserAccount? _account;
    private int _currentApartmentId;
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

    // Yêu cầu thêm thành viên mới (Modal)
    private bool _isAddRequestModalOpen;
    private string _newMemberFullName = string.Empty;
    private DateTime _newMemberDateOfBirth = DateTime.Today.AddYears(-20);
    private string _newMemberIdentityCard = string.Empty;
    private string _newMemberPhoneNumber = string.Empty;
    private string _newMemberEmail = string.Empty;
    private RelationshipType _selectedRelationshipType = RelationshipType.FamilyMember;
    private string _addRequestErrorMessage = string.Empty;
    private string _addRequestSuccessMessage = string.Empty;
    private bool _isSubmittingRequest;

    // Field-level error properties
    private string _fullNameError = string.Empty;
    private string _birthDateError = string.Empty;
    private string _identityCardError = string.Empty;
    private string _phoneNumberError = string.Empty;
    private string _emailError = string.Empty;

    // Tab lọc yêu cầu bên cột phải
    private string _selectedRequestFilter = "Tất cả"; // Tất cả, Chờ duyệt, Đã duyệt, Từ chối

    private readonly ObservableCollection<AccordionFamilyMemberItem> _allMembers = new();
    private readonly List<MemberRegistrationRequestItemViewModel> _allRequests = new();

    private readonly ResidenceService? _residenceService;
    private readonly MemberRequestService? _memberRequestService;

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

    public int TotalMembers => _allMembers.Count;
    public int TotalRequestsCount => _allRequests.Count;
    public int PendingRequestsCount => _allRequests.Count(r => r.Status == RequestStatus.Pending);
    public int ApprovedRequestsCount => _allRequests.Count(r => r.Status == RequestStatus.Approved);
    public int RejectedRequestsCount => _allRequests.Count(r => r.Status == RequestStatus.Rejected);

    // Edit Modal Properties
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

    public List<string> AvailableStatuses { get; } = new()
    {
        "Đang cư trú",
        "Tạm vắng",
        "Đã chuyển đi"
    };

    // Add Request Modal Properties
    public bool IsAddRequestModalOpen
    {
        get => _isAddRequestModalOpen;
        set => SetProperty(ref _isAddRequestModalOpen, value);
    }

    public string NewMemberFullName
    {
        get => _newMemberFullName;
        set
        {
            if (SetProperty(ref _newMemberFullName, value))
            {
                if (!string.IsNullOrEmpty(FullNameError)) ValidateInputs();
            }
        }
    }

    public DateTime NewMemberDateOfBirth
    {
        get => _newMemberDateOfBirth;
        set
        {
            if (SetProperty(ref _newMemberDateOfBirth, value))
            {
                if (!string.IsNullOrEmpty(BirthDateError)) ValidateInputs();
            }
        }
    }

    public string NewMemberIdentityCard
    {
        get => _newMemberIdentityCard;
        set
        {
            if (SetProperty(ref _newMemberIdentityCard, value))
            {
                if (!string.IsNullOrEmpty(IdentityCardError)) ValidateInputs();
            }
        }
    }

    public string NewMemberPhoneNumber
    {
        get => _newMemberPhoneNumber;
        set
        {
            if (SetProperty(ref _newMemberPhoneNumber, value))
            {
                if (!string.IsNullOrEmpty(PhoneNumberError)) ValidateInputs();
            }
        }
    }

    public string NewMemberEmail
    {
        get => _newMemberEmail;
        set
        {
            if (SetProperty(ref _newMemberEmail, value))
            {
                if (!string.IsNullOrEmpty(EmailError)) ValidateInputs();
            }
        }
    }

    public RelationshipType SelectedRelationshipType
    {
        get => _selectedRelationshipType;
        set => SetProperty(ref _selectedRelationshipType, value);
    }

    public string AddRequestErrorMessage
    {
        get => _addRequestErrorMessage;
        set => SetProperty(ref _addRequestErrorMessage, value);
    }

    public string AddRequestSuccessMessage
    {
        get => _addRequestSuccessMessage;
        set => SetProperty(ref _addRequestSuccessMessage, value);
    }

    public bool IsSubmittingRequest
    {
        get => _isSubmittingRequest;
        set => SetProperty(ref _isSubmittingRequest, value);
    }

    public string FullNameError
    {
        get => _fullNameError;
        set => SetProperty(ref _fullNameError, value);
    }

    public string BirthDateError
    {
        get => _birthDateError;
        set => SetProperty(ref _birthDateError, value);
    }

    public string IdentityCardError
    {
        get => _identityCardError;
        set => SetProperty(ref _identityCardError, value);
    }

    public string PhoneNumberError
    {
        get => _phoneNumberError;
        set => SetProperty(ref _phoneNumberError, value);
    }

    public string EmailError
    {
        get => _emailError;
        set => SetProperty(ref _emailError, value);
    }

    public string SelectedRequestFilter
    {
        get => _selectedRequestFilter;
        set
        {
            if (SetProperty(ref _selectedRequestFilter, value))
            {
                FilterRequests();
            }
        }
    }

    public List<RelationshipOption> AvailableRelationships { get; } = new()
    {
        new RelationshipOption { Value = RelationshipType.FamilyMember, DisplayName = "Thành viên gia đình" },
        new RelationshipOption { Value = RelationshipType.Tenant, DisplayName = "Người thuê căn hộ" },
        new RelationshipOption { Value = RelationshipType.Temporary, DisplayName = "Cư trú tạm thời" }
    };

    public ObservableCollection<AccordionFamilyMemberItem> FilteredMembers { get; } = new();
    public ObservableCollection<MemberRegistrationRequestItemViewModel> FilteredRequests { get; } = new();

    public bool HasMembers => FilteredMembers.Count > 0;
    public bool HasRequests => FilteredRequests.Count > 0;
    #endregion

    #region Commands
    public ICommand RefreshCommand { get; }
    public ICommand OpenEditMemberModalCommand { get; }
    public ICommand CloseEditMemberModalCommand { get; }
    public ICommand SaveMemberEditCommand { get; }

    public ICommand OpenAddRequestModalCommand { get; }
    public ICommand CloseAddRequestModalCommand { get; }
    public ICommand SubmitAddRequestCommand { get; }
    public ICommand SelectRequestFilterCommand { get; }
    #endregion

    public FamilyMembersViewModel() : this(null, null)
    {
    }

    public FamilyMembersViewModel(ResidenceService? residenceService, MemberRequestService? memberRequestService = null)
    {
        _residenceService = residenceService;
        _memberRequestService = memberRequestService;

        RefreshCommand = new AsyncRelayCommand(_ => LoadDataAsync());

        OpenEditMemberModalCommand = new RelayCommand(p =>
        {
            if (p is AccordionFamilyMemberItem member)
            {
                _editingMember = member;
                EditMemberFullName = member.FullName;
                EditMemberRelationship = member.Relationship;
                EditMemberPhone = member.PhoneNumber == "Chưa cập nhật" || member.PhoneNumber == "---" ? "" : member.PhoneNumber;
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

            if (!string.IsNullOrWhiteSpace(EditMemberPhone))
            {
                if (!Regex.IsMatch(EditMemberPhone.Trim(), @"^0\d{9}$"))
                {
                    EditErrorMessage = "Số điện thoại phải gồm 10 chữ số bắt đầu bằng số 0.";
                    return;
                }
            }

            _editingMember.PhoneNumber = string.IsNullOrWhiteSpace(EditMemberPhone) ? "Chưa cập nhật" : EditMemberPhone.Trim();
            _editingMember.Status = EditMemberStatus;

            IsEditMemberModalOpen = false;
            EditSuccessMessage = $"Cập nhật thông tin thành viên {_editingMember.FullName} thành công!";
            _editingMember = null;
            FilterMembers();
        });

        OpenAddRequestModalCommand = new RelayCommand(_ =>
        {
            NewMemberFullName = string.Empty;
            NewMemberDateOfBirth = DateTime.Today.AddYears(-20);
            NewMemberIdentityCard = string.Empty;
            NewMemberPhoneNumber = string.Empty;
            NewMemberEmail = string.Empty;
            SelectedRelationshipType = RelationshipType.FamilyMember;
            
            FullNameError = string.Empty;
            BirthDateError = string.Empty;
            IdentityCardError = string.Empty;
            PhoneNumberError = string.Empty;
            EmailError = string.Empty;
            AddRequestErrorMessage = string.Empty;
            
            IsAddRequestModalOpen = true;
        });

        CloseAddRequestModalCommand = new RelayCommand(_ =>
        {
            IsAddRequestModalOpen = false;
            AddRequestErrorMessage = string.Empty;
            FullNameError = string.Empty;
            BirthDateError = string.Empty;
            IdentityCardError = string.Empty;
            PhoneNumberError = string.Empty;
            EmailError = string.Empty;
        });

        SubmitAddRequestCommand = new AsyncRelayCommand(_ => SubmitAddRequestAsync());

        SelectRequestFilterCommand = new RelayCommand(p =>
        {
            if (p is string filter)
            {
                SelectedRequestFilter = filter;
            }
        });
    }

    public void Initialize(UserAccount account)
    {
        _account = account;
        _ = LoadDataAsync();
    }

    public bool ValidateInputs()
    {
        bool isValid = true;

        // Họ và tên: Bắt buộc, 2–100 ký tự, không chỉ chứa khoảng trắng
        if (string.IsNullOrWhiteSpace(NewMemberFullName) || NewMemberFullName.Trim().Length < 2 || NewMemberFullName.Trim().Length > 100)
        {
            FullNameError = "Họ và tên bắt buộc từ 2 đến 100 ký tự và không chỉ chứa khoảng trắng.";
            isValid = false;
        }
        else
        {
            FullNameError = string.Empty;
        }

        // Ngày sinh: Không được lớn hơn ngày hiện tại
        if (NewMemberDateOfBirth > DateTime.Today)
        {
            BirthDateError = "Ngày sinh không được lớn hơn ngày hiện tại.";
            isValid = false;
        }
        else
        {
            BirthDateError = string.Empty;
        }

        // CCCD: nếu nhập CCCD 12 số, phải đúng 12 chữ số
        if (!string.IsNullOrWhiteSpace(NewMemberIdentityCard))
        {
            if (!Regex.IsMatch(NewMemberIdentityCard.Trim(), @"^\d{12}$"))
            {
                IdentityCardError = "CCCD phải gồm đúng 12 chữ số.";
                isValid = false;
            }
            else
            {
                IdentityCardError = string.Empty;
            }
        }
        else
        {
            IdentityCardError = string.Empty;
        }

        // Số điện thoại: nếu có, phải đúng 10 số bắt đầu bằng 0
        if (!string.IsNullOrWhiteSpace(NewMemberPhoneNumber))
        {
            if (!Regex.IsMatch(NewMemberPhoneNumber.Trim(), @"^0\d{9}$"))
            {
                PhoneNumberError = "Số điện thoại phải gồm 10 chữ số bắt đầu bằng số 0.";
                isValid = false;
            }
            else
            {
                PhoneNumberError = string.Empty;
            }
        }
        else
        {
            PhoneNumberError = string.Empty;
        }

        // Email: nếu có, kiểm tra định dạng hợp lệ
        if (!string.IsNullOrWhiteSpace(NewMemberEmail))
        {
            if (!Regex.IsMatch(NewMemberEmail.Trim(), @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                EmailError = "Địa chỉ email không đúng định dạng hợp lệ.";
                isValid = false;
            }
            else
            {
                EmailError = string.Empty;
            }
        }
        else
        {
            EmailError = string.Empty;
        }

        return isValid;
    }

    public async Task LoadDataAsync()
    {
        IsLoading = true;
        ErrorMessage = string.Empty;

        try
        {
            await LoadFamilyMembersAsync();
            await LoadRequestsAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Lỗi khi tải dữ liệu: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task LoadFamilyMembersAsync()
    {
        _allMembers.Clear();

        if (_residenceService != null && _account?.ResidentId.HasValue == true)
        {
            var activeResidence = await _residenceService.GetActiveResidenceByResidentIdAsync(_account.ResidentId.Value);
            if (activeResidence != null)
            {
                _currentApartmentId = activeResidence.ApartmentId;
                if (activeResidence.Apartment != null)
                {
                    ApartmentNumber = activeResidence.Apartment.ApartmentNumber;
                    BuildingBlock = activeResidence.Apartment.ApartmentNumber.Length > 0 && char.IsLetter(activeResidence.Apartment.ApartmentNumber[0])
                        ? activeResidence.Apartment.ApartmentNumber[0].ToString()
                        : "A";
                    FloorNumber = activeResidence.Apartment.Floor.ToString();
                    ApartmentArea = $"{activeResidence.Apartment.Area} m²";
                }

                var allActive = await _residenceService.GetActiveResidencesByApartmentIdAsync(activeResidence.ApartmentId);
                foreach (var memberRes in allActive)
                {
                    var res = memberRes.Resident;
                    bool isOwner = memberRes.RelationshipType == RelationshipType.Owner;
                    string relText = memberRes.RelationshipType switch
                    {
                        RelationshipType.Owner => "Chủ hộ",
                        RelationshipType.FamilyMember => "Thành viên gia đình",
                        RelationshipType.Tenant => "Người thuê",
                        RelationshipType.Temporary => "Tạm trú",
                        _ => "Cư dân"
                    };

                    string avatarPath = isOwner ? "/Assets/resident_avatar.png" : "/Assets/avatar_young_man.png";

                    _allMembers.Add(new AccordionFamilyMemberItem
                    {
                        AvatarIcon = isOwner ? "👨" : "👤",
                        AvatarImagePath = avatarPath,
                        FullName = res != null ? res.FullName : "Thành viên",
                        Relationship = relText,
                        IsOwner = isOwner,
                        BirthDate = res != null ? res.DateOfBirth.ToString("dd/MM/yyyy") : "Chưa cập nhật",
                        Gender = "Chưa cập nhật",
                        IdentityCard = !string.IsNullOrWhiteSpace(res?.IdentityCard) ? res.IdentityCard : "Chưa cập nhật",
                        PhoneNumber = !string.IsNullOrWhiteSpace(res?.PhoneNumber) ? res.PhoneNumber : "Chưa cập nhật",
                        Hometown = "Chưa cập nhật",
                        Status = "Đang cư trú",
                        IsExpanded = false
                    });
                }
            }
        }

        FilterMembers();
        OnPropertyChanged(nameof(TotalMembers));
    }

    public async Task LoadRequestsAsync()
    {
        _allRequests.Clear();

        if (_memberRequestService != null && _currentApartmentId > 0)
        {
            var requests = await _memberRequestService.GetRequestsByApartmentIdAsync(_currentApartmentId);
            foreach (var req in requests.OrderByDescending(r => r.CreatedAt))
            {
                string statusText = req.Status switch
                {
                    RequestStatus.Pending => "Chờ duyệt",
                    RequestStatus.Approved => "Đã duyệt",
                    RequestStatus.Rejected => "Từ chối",
                    _ => "Không xác định"
                };

                string bg = req.Status switch
                {
                    RequestStatus.Pending => "#FEF3C7",
                    RequestStatus.Approved => "#DCFCE7",
                    RequestStatus.Rejected => "#FEE2E2",
                    _ => "#F1F5F9"
                };

                string fg = req.Status switch
                {
                    RequestStatus.Pending => "#D97706",
                    RequestStatus.Approved => "#16A34A",
                    RequestStatus.Rejected => "#DC2626",
                    _ => "#64748B"
                };

                string border = req.Status switch
                {
                    RequestStatus.Pending => "#FDE68A",
                    RequestStatus.Approved => "#BBF7D0",
                    RequestStatus.Rejected => "#FECACA",
                    _ => "#E2E8F0"
                };

                string relText = req.RelationshipType switch
                {
                    RelationshipType.Owner => "Chủ hộ",
                    RelationshipType.FamilyMember => "Con",
                    RelationshipType.Tenant => "Người thuê",
                    RelationshipType.Temporary => "Tạm trú",
                    _ => "Thành viên"
                };

                _allRequests.Add(new MemberRegistrationRequestItemViewModel
                {
                    Id = req.Id,
                    ApartmentId = req.ApartmentId,
                    ApartmentNumber = req.Apartment != null ? req.Apartment.ApartmentNumber : ApartmentNumber,
                    RequesterResidentId = req.RequesterResidentId,
                    RequesterName = req.RequesterResident != null ? req.RequesterResident.FullName : "Cư dân",
                    FullName = req.FullName,
                    AvatarImagePath = "/Assets/avatar_son.png",
                    DateOfBirth = req.DateOfBirth,
                    IdentityCard = req.IdentityCard,
                    PhoneNumber = req.PhoneNumber,
                    Email = req.Email,
                    RelationshipType = req.RelationshipType,
                    RelationshipText = relText,
                    Status = req.Status,
                    StatusText = statusText,
                    StatusBadgeBg = bg,
                    StatusBadgeFg = fg,
                    StatusBadgeBorder = border,
                    CreatedAt = req.CreatedAt,
                    ProcessedAt = req.ProcessedAt,
                    RejectReason = req.RejectReason
                });
            }
        }

        FilterRequests();
        OnPropertyChanged(nameof(TotalRequestsCount));
        OnPropertyChanged(nameof(PendingRequestsCount));
        OnPropertyChanged(nameof(ApprovedRequestsCount));
        OnPropertyChanged(nameof(RejectedRequestsCount));
    }

    private async Task SubmitAddRequestAsync()
    {
        AddRequestErrorMessage = string.Empty;

        if (!ValidateInputs())
        {
            AddRequestErrorMessage = "Vui lòng kiểm tra và sửa các thông tin chưa hợp lệ bên dưới.";
            return;
        }

        if (_account?.ResidentId == null || _currentApartmentId <= 0)
        {
            AddRequestErrorMessage = "Không tìm thấy thông tin căn hộ hiện tại của bạn.";
            return;
        }

        if (_memberRequestService == null)
        {
            AddRequestErrorMessage = "Dịch vụ yêu cầu chưa sẵn sàng.";
            return;
        }

        IsSubmittingRequest = true;
        try
        {
            var result = await _memberRequestService.CreateRequestAsync(
                _currentApartmentId,
                _account.ResidentId.Value,
                NewMemberFullName,
                NewMemberDateOfBirth,
                NewMemberIdentityCard,
                NewMemberPhoneNumber,
                NewMemberEmail,
                SelectedRelationshipType);

            if (result.IsSuccess)
            {
                IsAddRequestModalOpen = false;
                AddRequestSuccessMessage = $"Đã gửi yêu cầu thêm thành viên '{NewMemberFullName}' thành công! Ban quản lý sẽ sớm duyệt.";
                await LoadRequestsAsync();
            }
            else
            {
                AddRequestErrorMessage = result.ErrorMessage ?? "Không thể gửi yêu cầu.";
            }
        }
        catch (Exception ex)
        {
            AddRequestErrorMessage = $"Lỗi: {ex.Message}";
        }
        finally
        {
            IsSubmittingRequest = false;
        }
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

    private void FilterRequests()
    {
        FilteredRequests.Clear();
        var query = _allRequests.AsEnumerable();

        if (SelectedRequestFilter == "Chờ duyệt")
        {
            query = query.Where(r => r.Status == RequestStatus.Pending);
        }
        else if (SelectedRequestFilter == "Đã duyệt")
        {
            query = query.Where(r => r.Status == RequestStatus.Approved);
        }
        else if (SelectedRequestFilter == "Từ chối")
        {
            query = query.Where(r => r.Status == RequestStatus.Rejected);
        }

        foreach (var item in query)
        {
            FilteredRequests.Add(item);
        }

        OnPropertyChanged(nameof(HasRequests));
    }
}
