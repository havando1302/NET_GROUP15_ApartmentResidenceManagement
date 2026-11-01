using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using ApartmentResidenceManagement.Domain.Entities;
using ApartmentResidenceManagement.Domain.Enums;
using ApartmentResidenceManagement.Application.Services;
using ApartmentResidenceManagement.Wpf.Commands;

namespace ApartmentResidenceManagement.Wpf.ViewModels;

public class ResidencyViewModel : ViewModelBase
{
    private readonly ResidenceService _residenceService;
    private readonly ApartmentService _apartmentService;
    private readonly ResidentService _residentService;
    private readonly MemberRequestService? _memberRequestService;
    
    private List<ResidenceHistory> _allResidences = new();
    private List<MemberRegistrationRequestItemViewModel> _allRequests = new();

    // Tab chuyển đổi: 0 = "Danh sách cư trú", 1 = "Yêu cầu thêm thành viên"
    private int _selectedTabIndex = 0;

    // Lọc & Tìm kiếm cư trú
    private string _searchText = string.Empty;
    private string _selectedStatusFilter = "Tất cả"; // Tất cả, Đang cư trú, Đã chuyển đi
    
    // UI Collections
    private ObservableCollection<ResidenceHistory> _residences = new();
    private ObservableCollection<MemberRegistrationRequestItemViewModel> _requests = new();
    private List<Apartment> _availableApartments = new();
    private List<Resident> _availableResidents = new();
    
    // Thao tác Form Cư trú
    private ResidenceHistory? _selectedResidence;
    private bool _isFormOpen;
    private string _formTitle = string.Empty;
    private string _actionType = "Register"; // Register, Terminate, Transfer
    
    // Form Inputs
    private Apartment? _selectedApartmentInput;
    private Resident? _selectedResidentInput;
    private RelationshipType _selectedRelationshipType = RelationshipType.FamilyMember;
    private DateTime _startDateInput = DateTime.Today;
    private DateTime _endDateInput = DateTime.Today;

    // Thao tác Từ chối Yêu cầu
    private bool _isRejectDialogOpen;
    private MemberRegistrationRequestItemViewModel? _requestToReject;
    private string _rejectReasonInput = string.Empty;
    private string _requestSuccessMessage = string.Empty;
    
    // Báo lỗi
    private string _errorMessage = string.Empty;
    private bool _hasError;
    private bool _isLoading;

    #region Properties
    public int SelectedTabIndex
    {
        get => _selectedTabIndex;
        set
        {
            if (SetProperty(ref _selectedTabIndex, value))
            {
                OnPropertyChanged(nameof(IsResidencesTab));
                OnPropertyChanged(nameof(IsRequestsTab));
            }
        }
    }

    public bool IsResidencesTab => SelectedTabIndex == 0;
    public bool IsRequestsTab => SelectedTabIndex == 1;

    public ObservableCollection<ResidenceHistory> Residences
    {
        get => _residences;
        set => SetProperty(ref _residences, value);
    }

    public ObservableCollection<MemberRegistrationRequestItemViewModel> Requests
    {
        get => _requests;
        set => SetProperty(ref _requests, value);
    }

    public int PendingRequestsCount => _allRequests.Count(r => r.Status == RequestStatus.Pending);
    public bool HasPendingRequests => PendingRequestsCount > 0;
    public bool HasRequests => Requests.Count > 0;

    public List<Apartment> AvailableApartments
    {
        get => _availableApartments;
        set => SetProperty(ref _availableApartments, value);
    }

    public List<Resident> AvailableResidents
    {
        get => _availableResidents;
        set => SetProperty(ref _availableResidents, value);
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                ApplyFilters();
            }
        }
    }

    public string SelectedStatusFilter
    {
        get => _selectedStatusFilter;
        set
        {
            if (SetProperty(ref _selectedStatusFilter, value))
            {
                ApplyFilters();
            }
        }
    }

    public List<string> StatusFilterOptions { get; } = new()
    {
        "Tất cả",
        "Đang cư trú",
        "Đã chuyển đi"
    };

    // Form Properties
    public bool IsFormOpen
    {
        get => _isFormOpen;
        set => SetProperty(ref _isFormOpen, value);
    }

    public string FormTitle
    {
        get => _formTitle;
        set => SetProperty(ref _formTitle, value);
    }

    public string ActionType
    {
        get => _actionType;
        set
        {
            if (SetProperty(ref _actionType, value))
            {
                OnPropertyChanged(nameof(IsRegisterAction));
                OnPropertyChanged(nameof(IsTerminateAction));
                OnPropertyChanged(nameof(IsTransferAction));
            }
        }
    }

    public bool IsRegisterAction => ActionType == "Register";
    public bool IsTerminateAction => ActionType == "Terminate";
    public bool IsTransferAction => ActionType == "Transfer";

    public ResidenceHistory? SelectedResidence
    {
        get => _selectedResidence;
        set => SetProperty(ref _selectedResidence, value);
    }

    public Apartment? SelectedApartmentInput
    {
        get => _selectedApartmentInput;
        set => SetProperty(ref _selectedApartmentInput, value);
    }

    public Resident? SelectedResidentInput
    {
        get => _selectedResidentInput;
        set => SetProperty(ref _selectedResidentInput, value);
    }

    public RelationshipType SelectedRelationshipType
    {
        get => _selectedRelationshipType;
        set => SetProperty(ref _selectedRelationshipType, value);
    }

    public List<RelationshipType> RelationshipTypes { get; } = Enum.GetValues(typeof(RelationshipType)).Cast<RelationshipType>().ToList();

    public DateTime StartDateInput
    {
        get => _startDateInput;
        set => SetProperty(ref _startDateInput, value);
    }

    public DateTime EndDateInput
    {
        get => _endDateInput;
        set => SetProperty(ref _endDateInput, value);
    }

    // Reject Dialog Properties
    public bool IsRejectDialogOpen
    {
        get => _isRejectDialogOpen;
        set => SetProperty(ref _isRejectDialogOpen, value);
    }

    public string RejectReasonInput
    {
        get => _rejectReasonInput;
        set => SetProperty(ref _rejectReasonInput, value);
    }

    public string RequestSuccessMessage
    {
        get => _requestSuccessMessage;
        set => SetProperty(ref _requestSuccessMessage, value);
    }

    // Báo lỗi
    public string ErrorMessage
    {
        get => _errorMessage;
        set
        {
            if (SetProperty(ref _errorMessage, value))
            {
                HasError = !string.IsNullOrEmpty(value);
            }
        }
    }

    public bool HasError
    {
        get => _hasError;
        set => SetProperty(ref _hasError, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }
    #endregion

    #region Commands
    public ICommand LoadResidencesCommand { get; }
    public ICommand OpenRegisterFormCommand { get; }
    public ICommand OpenTerminateFormCommand { get; }
    public ICommand OpenTransferFormCommand { get; }
    public ICommand SaveResidencyCommand { get; }
    public ICommand CancelFormCommand { get; }

    public ICommand SwitchTabCommand { get; }
    public ICommand ApproveRequestCommand { get; }
    public ICommand OpenRejectDialogCommand { get; }
    public ICommand ConfirmRejectRequestCommand { get; }
    public ICommand CloseRejectDialogCommand { get; }
    #endregion

    public ResidencyViewModel(
        ResidenceService residenceService,
        ApartmentService apartmentService,
        ResidentService residentService,
        MemberRequestService? memberRequestService = null)
    {
        _residenceService = residenceService;
        _apartmentService = apartmentService;
        _residentService = residentService;
        _memberRequestService = memberRequestService;

        LoadResidencesCommand = new AsyncRelayCommand(_ => LoadDataAsync());
        OpenRegisterFormCommand = new RelayCommand(_ => OpenRegisterForm());
        OpenTerminateFormCommand = new RelayCommand(r => OpenTerminateForm(r));
        OpenTransferFormCommand = new RelayCommand(r => OpenTransferForm(r));
        SaveResidencyCommand = new AsyncRelayCommand(_ => SaveResidencyAsync());
        CancelFormCommand = new RelayCommand(_ => CloseForm());

        SwitchTabCommand = new RelayCommand(tab =>
        {
            if (tab is string s && int.TryParse(s, out int idx))
            {
                SelectedTabIndex = idx;
            }
            else if (tab is int i)
            {
                SelectedTabIndex = i;
            }
        });

        ApproveRequestCommand = new AsyncRelayCommand(async p =>
        {
            if (p is MemberRegistrationRequestItemViewModel req && _memberRequestService != null)
            {
                await ApproveRequestAsync(req);
            }
        });

        OpenRejectDialogCommand = new RelayCommand(p =>
        {
            if (p is MemberRegistrationRequestItemViewModel req)
            {
                _requestToReject = req;
                RejectReasonInput = string.Empty;
                IsRejectDialogOpen = true;
            }
        });

        CloseRejectDialogCommand = new RelayCommand(_ =>
        {
            IsRejectDialogOpen = false;
            _requestToReject = null;
        });

        ConfirmRejectRequestCommand = new AsyncRelayCommand(async _ =>
        {
            if (_requestToReject != null && _memberRequestService != null)
            {
                await RejectRequestAsync(_requestToReject, RejectReasonInput);
            }
        });
    }

    public async Task LoadDataAsync()
    {
        ErrorMessage = string.Empty;
        IsLoading = true;
        try
        {
            var list = await _residenceService.GetAllResidencesAsync();
            _allResidences = list.ToList();

            var apartments = await _apartmentService.GetAllApartmentsAsync();
            AvailableApartments = apartments
                .Where(a => a.Status != ApartmentStatus.UnderMaintenance)
                .OrderBy(a => a.ApartmentNumber)
                .ToList();

            var residents = await _residentService.GetAllResidentsAsync();
            var activeResidentIds = _allResidences
                .Where(rh => rh.IsActive)
                .Select(rh => rh.ResidentId)
                .ToHashSet();
            AvailableResidents = residents
                .Where(r => !activeResidentIds.Contains(r.Id))
                .OrderBy(r => r.FullName)
                .ToList();

            ApplyFilters();

            // Load requests
            await LoadRequestsAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Lỗi khi lấy dữ liệu cư trú hoặc danh mục: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task LoadRequestsAsync()
    {
        _allRequests.Clear();
        Requests.Clear();

        if (_memberRequestService != null)
        {
            var reqs = await _memberRequestService.GetAllRequestsWithDetailsAsync();
            foreach (var req in reqs)
            {
                string relText = req.RelationshipType switch
                {
                    RelationshipType.Owner => "Chủ hộ",
                    RelationshipType.FamilyMember => "Thành viên gia đình",
                    RelationshipType.Tenant => "Người thuê",
                    RelationshipType.Temporary => "Tạm trú",
                    _ => "Cư dân"
                };

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
                    _ => "#475569"
                };

                string border = req.Status switch
                {
                    RequestStatus.Pending => "#FDE68A",
                    RequestStatus.Approved => "#BBF7D0",
                    RequestStatus.Rejected => "#FECACA",
                    _ => "#CBD5E1"
                };

                var item = new MemberRegistrationRequestItemViewModel
                {
                    Id = req.Id,
                    ApartmentId = req.ApartmentId,
                    ApartmentNumber = req.Apartment?.ApartmentNumber ?? $"P.{req.ApartmentId}",
                    RequesterResidentId = req.RequesterResidentId,
                    RequesterName = req.RequesterResident?.FullName ?? "Cư dân",
                    FullName = req.FullName,
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
                };

                _allRequests.Add(item);
                Requests.Add(item);
            }
        }

        OnPropertyChanged(nameof(PendingRequestsCount));
        OnPropertyChanged(nameof(HasPendingRequests));
        OnPropertyChanged(nameof(HasRequests));
    }

    private async Task ApproveRequestAsync(MemberRegistrationRequestItemViewModel req)
    {
        if (_memberRequestService == null) return;

        IsLoading = true;
        ErrorMessage = string.Empty;
        RequestSuccessMessage = string.Empty;

        try
        {
            var res = await _memberRequestService.ApproveRequestAsync(req.Id);
            if (res.IsSuccess)
            {
                RequestSuccessMessage = $"Đã phê duyệt và thêm thành viên '{req.FullName}' vào căn hộ {req.ApartmentNumber} thành công!";
                await LoadDataAsync();
            }
            else
            {
                ErrorMessage = res.ErrorMessage ?? "Không thể phê duyệt yêu cầu.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Lỗi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task RejectRequestAsync(MemberRegistrationRequestItemViewModel req, string? reason)
    {
        if (_memberRequestService == null) return;

        IsLoading = true;
        ErrorMessage = string.Empty;
        RequestSuccessMessage = string.Empty;

        try
        {
            var res = await _memberRequestService.RejectRequestAsync(req.Id, reason);
            if (res.IsSuccess)
            {
                IsRejectDialogOpen = false;
                _requestToReject = null;
                RequestSuccessMessage = $"Đã từ chối yêu cầu của '{req.FullName}'.";
                await LoadRequestsAsync();
            }
            else
            {
                ErrorMessage = res.ErrorMessage ?? "Không thể từ chối yêu cầu.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Lỗi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ApplyFilters()
    {
        var filtered = _allResidences.AsEnumerable();

        if (SelectedStatusFilter == "Đang cư trú")
        {
            filtered = filtered.Where(rh => rh.IsActive);
        }
        else if (SelectedStatusFilter == "Đã chuyển đi")
        {
            filtered = filtered.Where(rh => !rh.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var search = SearchText.Trim().ToLower();
            filtered = filtered.Where(rh =>
                (rh.Resident?.FullName != null && rh.Resident.FullName.ToLower().Contains(search)) ||
                (rh.Resident?.IdentityCard != null && rh.Resident.IdentityCard.ToLower().Contains(search)) ||
                (rh.Apartment?.ApartmentNumber != null && rh.Apartment.ApartmentNumber.ToLower().Contains(search))
            );
        }

        Residences.Clear();
        foreach (var item in filtered)
        {
            Residences.Add(item);
        }
    }

    private void OpenRegisterForm()
    {
        ErrorMessage = string.Empty;
        SelectedResidence = null;
        SelectedApartmentInput = null;
        SelectedResidentInput = null;
        SelectedRelationshipType = RelationshipType.FamilyMember;
        StartDateInput = DateTime.Today;
        ActionType = "Register";
        FormTitle = "ĐĂNG KÝ CƯ TRÚ MỚI";
        IsFormOpen = true;
    }

    private void OpenTerminateForm(object? param)
    {
        if (param is ResidenceHistory rh)
        {
            ErrorMessage = string.Empty;
            SelectedResidence = rh;
            SelectedApartmentInput = rh.Apartment;
            SelectedResidentInput = rh.Resident;
            SelectedRelationshipType = rh.RelationshipType;
            StartDateInput = rh.StartDate;
            EndDateInput = DateTime.Today;
            ActionType = "Terminate";
            FormTitle = $"KẾT THÚC CƯ TRÚ: {rh.Resident?.FullName} ({rh.Apartment?.ApartmentNumber})";
            IsFormOpen = true;
        }
    }

    private void OpenTransferForm(object? param)
    {
        if (param is ResidenceHistory rh)
        {
            ErrorMessage = string.Empty;
            SelectedResidence = rh;
            SelectedResidentInput = rh.Resident;
            SelectedApartmentInput = null;
            SelectedRelationshipType = rh.RelationshipType;
            StartDateInput = DateTime.Today;
            ActionType = "Transfer";
            FormTitle = $"CHUYỂN HỘ CHO: {rh.Resident?.FullName} (Hiện tại: {rh.Apartment?.ApartmentNumber})";
            IsFormOpen = true;
        }
    }

    private void CloseForm()
    {
        IsFormOpen = false;
        ErrorMessage = string.Empty;
    }

    private async Task SaveResidencyAsync()
    {
        ErrorMessage = string.Empty;
        IsLoading = true;
        try
        {
            if (ActionType == "Register")
            {
                if (SelectedApartmentInput == null)
                {
                    ErrorMessage = "Vui lòng chọn căn hộ.";
                    return;
                }
                if (SelectedResidentInput == null)
                {
                    ErrorMessage = "Vui lòng chọn cư dân.";
                    return;
                }

                var res = await _residenceService.RegisterResidenceAsync(
                    SelectedApartmentInput.Id,
                    SelectedResidentInput.Id,
                    SelectedRelationshipType,
                    StartDateInput
                );

                if (!res.IsSuccess)
                {
                    ErrorMessage = res.ErrorMessage ?? "Đăng ký cư trú thất bại.";
                    return;
                }
            }
            else if (ActionType == "Terminate")
            {
                if (SelectedResidence == null)
                {
                    ErrorMessage = "Không tìm thấy hồ sơ cư trú cần kết thúc.";
                    return;
                }

                var res = await _residenceService.TerminateResidenceAsync(SelectedResidence.Id, EndDateInput);
                if (!res.IsSuccess)
                {
                    ErrorMessage = res.ErrorMessage ?? "Kết thúc cư trú thất bại.";
                    return;
                }
            }
            else if (ActionType == "Transfer")
            {
                if (SelectedResidence == null)
                {
                    ErrorMessage = "Không tìm thấy hồ sơ cư trú hiện tại.";
                    return;
                }
                if (SelectedApartmentInput == null)
                {
                    ErrorMessage = "Vui lòng chọn căn hộ mới cần chuyển đến.";
                    return;
                }

                var res = await _residenceService.TransferApartmentAsync(
                    SelectedResidence.ResidentId,
                    SelectedApartmentInput.Id,
                    SelectedRelationshipType,
                    StartDateInput
                );

                if (!res.IsSuccess)
                {
                    ErrorMessage = res.ErrorMessage ?? "Chuyển căn hộ thất bại.";
                    return;
                }
            }

            CloseForm();
            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Đã xảy ra lỗi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
}
