using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using ApartmentResidenceManagement.Application.Common;
using ApartmentResidenceManagement.Application.Services;
using ApartmentResidenceManagement.Domain.Entities;
using ApartmentResidenceManagement.Domain.Enums;
using ApartmentResidenceManagement.Domain.Interfaces;
using ApartmentResidenceManagement.Wpf.Commands;
using ApartmentResidenceManagement.Wpf.Services;

namespace ApartmentResidenceManagement.Wpf.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly UserAccount _account;
    private readonly IAppNotificationService? _notificationService;
    private readonly IUnitOfWork? _unitOfWork;
    private ViewModelBase _currentViewModel;

    // Các ViewModels con được tiêm vào (Admin)
    public DashboardViewModel DashboardVm { get; }
    public ApartmentViewModel ApartmentVm { get; }
    public ResidentViewModel ResidentVm { get; }
    public ResidencyViewModel ResidencyVm { get; }
    public VehicleViewModel VehicleVm { get; }
    public StatisticsViewModel StatisticsVm { get; }
    public AccountManagementViewModel AccountManagementVm { get; }

    // Các ViewModels con được tiêm vào (Resident)
    public ResidentHomeViewModel ResidentHomeVm { get; }
    public MyProfileViewModel MyProfileVm { get; }
    public MyApartmentViewModel MyApartmentVm { get; }
    public FamilyMembersViewModel FamilyMembersVm { get; }
    public MyVehiclesViewModel MyVehiclesVm { get; }
    public ResidencyHistoryViewModel ResidencyHistoryVm { get; }
    public UnderDevelopmentViewModel UnderDevelopmentVm { get; }

    public ViewModelBase CurrentViewModel
    {
        get => _currentViewModel;
        set
        {
            if (SetProperty(ref _currentViewModel, value))
            {
                OnPropertyChanged(nameof(ActiveTab));
            }
        }
    }

    public string ActiveTab => CurrentViewModel?.GetType().Name ?? string.Empty;

    public string WelcomeMessage { get; }
    public string RoleMessage { get; }
    public string ResidentDisplayName { get; }
    public string ResidentApartmentInfo { get; }
    public string ResidentGreeting { get; }
    public bool IsAdmin => _account.Role == UserRole.Admin;
    public bool IsResident => !IsAdmin;
    public string PortalSubtitle => IsAdmin ? "Cổng quản trị viên" : "Cổng thông tin cư dân";

    // Navigation Commands (Admin)
    public ICommand ShowDashboardCommand { get; }
    public ICommand ShowApartmentCommand { get; }
    public ICommand ShowResidentCommand { get; }
    public ICommand ShowResidencyCommand { get; }
    public ICommand ShowVehicleCommand { get; }
    public ICommand ShowStatisticsCommand { get; }
    public ICommand ShowAccountManagementCommand { get; }

    // Navigation Commands (Resident)
    public ICommand ShowResidentHomeCommand { get; }
    public ICommand ShowMyProfileCommand { get; }
    public ICommand ShowMyApartmentCommand { get; }
    public ICommand ShowFamilyMembersCommand { get; }
    public ICommand ShowMyVehiclesCommand { get; }
    public ICommand ShowResidencyHistoryCommand { get; }

    // Các tính năng đang phát triển (Resident)
    public ICommand ShowServicesCommand { get; }
    public ICommand ShowNotificationsCommand { get; }
    public ICommand ShowFeesCommand { get; }
    public ICommand ShowFeedbackCommand { get; }

    public ICommand LogoutCommand { get; }

    #region Trung tâm Thông báo (Notification Center 2026)

    private bool _isNotificationCenterOpen;
    public bool IsNotificationCenterOpen
    {
        get => _isNotificationCenterOpen;
        set
        {
            if (SetProperty(ref _isNotificationCenterOpen, value))
            {
                if (value)
                {
                    _ = RefreshNotificationsAsync();
                    if (IsAdmin && SelectableResidents.Count == 0)
                    {
                        _ = LoadSelectableResidentsAsync();
                    }
                }
            }
        }
    }

    private int _selectedNotificationTab;
    public int SelectedNotificationTab
    {
        get => _selectedNotificationTab;
        set
        {
            if (SetProperty(ref _selectedNotificationTab, value))
            {
                if (value == 2)
                {
                    _ = LoadSentNotificationsAsync();
                }
            }
        }
    }

    private int _unreadNotificationsCount;
    public int UnreadNotificationsCount
    {
        get => _unreadNotificationsCount;
        set
        {
            if (SetProperty(ref _unreadNotificationsCount, value))
            {
                OnPropertyChanged(nameof(HasUnreadNotifications));
                OnPropertyChanged(nameof(UnreadNotificationsCountText));
            }
        }
    }

    public bool HasUnreadNotifications => UnreadNotificationsCount > 0;
    public string UnreadNotificationsCountText => UnreadNotificationsCount > 99 ? "99+" : UnreadNotificationsCount.ToString();

    public ObservableCollection<NotificationItemViewModel> Notifications { get; } = new();
    public ObservableCollection<NotificationItemViewModel> FilteredNotifications { get; } = new();

    private string _selectedNotificationFilter = "Tất cả";
    public string SelectedNotificationFilter
    {
        get => _selectedNotificationFilter;
        set
        {
            if (SetProperty(ref _selectedNotificationFilter, value))
            {
                ApplyNotificationFilter();
            }
        }
    }

    private NotificationItemViewModel? _selectedNotificationDetail;
    public NotificationItemViewModel? SelectedNotificationDetail
    {
        get => _selectedNotificationDetail;
        set => SetProperty(ref _selectedNotificationDetail, value);
    }

    private bool _isNotificationDetailModalOpen;
    public bool IsNotificationDetailModalOpen
    {
        get => _isNotificationDetailModalOpen;
        set => SetProperty(ref _isNotificationDetailModalOpen, value);
    }

    // Soạn thông báo mới (Admin)
    private string _newNotificationTitle = string.Empty;
    public string NewNotificationTitle
    {
        get => _newNotificationTitle;
        set => SetProperty(ref _newNotificationTitle, value);
    }

    private string _newNotificationContent = string.Empty;
    public string NewNotificationContent
    {
        get => _newNotificationContent;
        set => SetProperty(ref _newNotificationContent, value);
    }

    private NotificationType _newNotificationType = NotificationType.General;
    public NotificationType NewNotificationType
    {
        get => _newNotificationType;
        set
        {
            if (SetProperty(ref _newNotificationType, value))
            {
                OnPropertyChanged(nameof(PreviewNoticeTypeIcon));
            }
        }
    }

    public string PreviewNoticeTypeIcon => NewNotificationType switch
    {
        NotificationType.Maintenance => "🛠️",
        NotificationType.FeeNotice => "💳",
        NotificationType.UrgentNotice => "🚨",
        _ => "📢"
    };

    private NotificationPriority _newNotificationPriority = NotificationPriority.Normal;
    public NotificationPriority NewNotificationPriority
    {
        get => _newNotificationPriority;
        set => SetProperty(ref _newNotificationPriority, value);
    }

    private int _selectedTargetAudienceIndex; // 0: Tất cả, 1: Đang cư trú, 2: Đã chuyển đi, 3: Cư dân cụ thể
    public int SelectedTargetAudienceIndex
    {
        get => _selectedTargetAudienceIndex;
        set
        {
            if (SetProperty(ref _selectedTargetAudienceIndex, value))
            {
                OnPropertyChanged(nameof(IsSpecificResidentsMode));
            }
        }
    }

    public bool IsSpecificResidentsMode => SelectedTargetAudienceIndex == 3;

    public ObservableCollection<SelectableResidentItemViewModel> SelectableResidents { get; } = new();
    public ObservableCollection<SelectableResidentItemViewModel> FilteredSelectableResidents { get; } = new();

    private string _residentSearchKeyword = string.Empty;
    public string ResidentSearchKeyword
    {
        get => _residentSearchKeyword;
        set
        {
            if (SetProperty(ref _residentSearchKeyword, value))
            {
                FilterSelectableResidents();
            }
        }
    }

    private bool _isSendingNotification;
    public bool IsSendingNotification
    {
        get => _isSendingNotification;
        set => SetProperty(ref _isSendingNotification, value);
    }

    private string _notificationSendError = string.Empty;
    public string NotificationSendError
    {
        get => _notificationSendError;
        set => SetProperty(ref _notificationSendError, value);
    }

    #region Xem trước thông báo (Preview Notice 2026)
    private bool _isPreviewNoticeModalOpen;
    public bool IsPreviewNoticeModalOpen
    {
        get => _isPreviewNoticeModalOpen;
        set => SetProperty(ref _isPreviewNoticeModalOpen, value);
    }

    private string _previewNoticeTitle = string.Empty;
    public string PreviewNoticeTitle
    {
        get => _previewNoticeTitle;
        set => SetProperty(ref _previewNoticeTitle, value);
    }

    private string _previewNoticeTypeName = string.Empty;
    public string PreviewNoticeTypeName
    {
        get => _previewNoticeTypeName;
        set => SetProperty(ref _previewNoticeTypeName, value);
    }

    private string _previewNoticeTypeBadgeBg = "#EFF6FF";
    public string PreviewNoticeTypeBadgeBg
    {
        get => _previewNoticeTypeBadgeBg;
        set => SetProperty(ref _previewNoticeTypeBadgeBg, value);
    }

    private string _previewNoticeTypeBadgeFg = "#1D4ED8";
    public string PreviewNoticeTypeBadgeFg
    {
        get => _previewNoticeTypeBadgeFg;
        set => SetProperty(ref _previewNoticeTypeBadgeFg, value);
    }

    private string _previewNoticePriorityName = string.Empty;
    public string PreviewNoticePriorityName
    {
        get => _previewNoticePriorityName;
        set => SetProperty(ref _previewNoticePriorityName, value);
    }

    private string _previewNoticePriorityBadgeBg = "#F1F5F9";
    public string PreviewNoticePriorityBadgeBg
    {
        get => _previewNoticePriorityBadgeBg;
        set => SetProperty(ref _previewNoticePriorityBadgeBg, value);
    }

    private string _previewNoticePriorityBadgeFg = "#64748B";
    public string PreviewNoticePriorityBadgeFg
    {
        get => _previewNoticePriorityBadgeFg;
        set => SetProperty(ref _previewNoticePriorityBadgeFg, value);
    }

    private string _previewNoticeAudienceText = string.Empty;
    public string PreviewNoticeAudienceText
    {
        get => _previewNoticeAudienceText;
        set => SetProperty(ref _previewNoticeAudienceText, value);
    }

    private string _previewNoticeContent = string.Empty;
    public string PreviewNoticeContent
    {
        get => _previewNoticeContent;
        set => SetProperty(ref _previewNoticeContent, value);
    }

    public ICommand OpenPreviewNoticeCommand { get; }
    public ICommand ClosePreviewNoticeCommand { get; }
    public ICommand ConfirmSendFromPreviewCommand { get; }
    #endregion

    #region Lịch sử gửi thông báo (Admin Sent History)
    public ObservableCollection<SentNotificationItemViewModel> SentNotifications { get; } = new();
    public ObservableCollection<SentNotificationItemViewModel> FilteredSentNotifications { get; } = new();

    private string _sentNotificationSearchKeyword = string.Empty;
    public string SentNotificationSearchKeyword
    {
        get => _sentNotificationSearchKeyword;
        set
        {
            if (SetProperty(ref _sentNotificationSearchKeyword, value))
            {
                FilterSentNotifications();
            }
        }
    }

    private bool _isLoadingSentNotifications;
    public bool IsLoadingSentNotifications
    {
        get => _isLoadingSentNotifications;
        set => SetProperty(ref _isLoadingSentNotifications, value);
    }

    public ICommand RefreshSentNotificationsCommand { get; }
    public ICommand OpenSentNotificationDetailCommand { get; }
    #endregion

    private string _toastMessage = string.Empty;
    public string ToastMessage
    {
        get => _toastMessage;
        set => SetProperty(ref _toastMessage, value);
    }

    private bool _isToastVisible;
    public bool IsToastVisible
    {
        get => _isToastVisible;
        set => SetProperty(ref _isToastVisible, value);
    }

    // Commands Trung tâm Thông báo
    public ICommand ToggleNotificationCenterCommand { get; }
    public ICommand CloseNotificationCenterCommand { get; }
    public ICommand RefreshNotificationsCommand { get; }
    public ICommand FilterNotificationsCommand { get; }
    public ICommand MarkAllNotificationsAsReadCommand { get; }
    public ICommand OpenNotificationItemCommand { get; }
    public ICommand CloseNotificationDetailModalCommand { get; }

    // Commands Định dạng văn phòng Rich Text
    public ICommand ApplyBoldCommand { get; }
    public ICommand ApplyItalicCommand { get; }
    public ICommand ApplyUnderlineCommand { get; }
    public ICommand ApplyColorCommand { get; }
    public ICommand ApplyBulletCommand { get; }
    public ICommand ApplyLineBreakCommand { get; }

    // Commands Chọn cư dân
    public ICommand SelectAllResidentsCommand { get; }
    public ICommand DeselectAllResidentsCommand { get; }
    public ICommand SendAnnouncementCommand { get; }
    public ICommand CloseToastCommand { get; }

    #endregion

    public MainViewModel(
        UserAccount account, 
        DashboardViewModel dashboardVm, 
        ApartmentViewModel apartmentVm, 
        ResidentViewModel residentVm,
        ResidencyViewModel residencyVm,
        VehicleViewModel vehicleVm,
        StatisticsViewModel statisticsVm,
        AccountManagementViewModel accountManagementVm,
        ResidentHomeViewModel residentHomeVm,
        MyProfileViewModel myProfileVm,
        MyApartmentViewModel myApartmentVm,
        FamilyMembersViewModel familyMembersVm,
        MyVehiclesViewModel myVehiclesVm,
        ResidencyHistoryViewModel residencyHistoryVm,
        UnderDevelopmentViewModel? underDevelopmentVm = null,
        IAppNotificationService? notificationService = null,
        IUnitOfWork? unitOfWork = null,
        Action? onLogout = null)
    {
        _account = account;
        _notificationService = notificationService;
        _unitOfWork = unitOfWork;

        DashboardVm = dashboardVm;
        ApartmentVm = apartmentVm;
        ResidentVm = residentVm;
        ResidencyVm = residencyVm;
        VehicleVm = vehicleVm;
        StatisticsVm = statisticsVm;
        AccountManagementVm = accountManagementVm;

        ResidentHomeVm = residentHomeVm;
        MyProfileVm = myProfileVm;
        MyApartmentVm = myApartmentVm;
        FamilyMembersVm = familyMembersVm;
        MyVehiclesVm = myVehiclesVm;
        ResidencyHistoryVm = residencyHistoryVm;
        UnderDevelopmentVm = underDevelopmentVm ?? new UnderDevelopmentViewModel(() => CurrentViewModel = ResidentHomeVm);

        // Khởi tạo thông tin UserAccount cho Resident ViewModels
        ResidentHomeVm.Initialize(_account);
        MyProfileVm.Initialize(_account);
        MyApartmentVm.Initialize(_account);
        FamilyMembersVm.Initialize(_account);
        MyVehiclesVm.Initialize(_account);
        ResidencyHistoryVm.Initialize(_account);

        string displayName = _account.Resident?.FullName ?? (_account.Username == "admin" ? "Ban quản lý" : _account.Username);
        ResidentDisplayName = displayName;
        ResidentApartmentInfo = _account.Resident != null ? $"Cư dân • {_account.Username}" : "Cư dân";
        ResidentGreeting = $"Xin chào, {displayName}! 👋";

        if (_account.Role == UserRole.Admin)
        {
            WelcomeMessage = $"Xin chào, {_account.Username}!";
            RoleMessage = "Ban quản lý Tòa nhà";
        }
        else
        {
            WelcomeMessage = $"Xin chào cư dân, {displayName}!";
            RoleMessage = $"Cư dân ({_account.Username})";
        }

        // Đặt Tab mặc định ban đầu dựa vào Role
        if (IsAdmin)
        {
            _currentViewModel = ApartmentVm;
            _ = SafeExecuteAsync(() => ApartmentVm.LoadDataAsync());
        }
        else
        {
            _currentViewModel = ResidentHomeVm;
            _ = SafeExecuteAsync(() => ResidentHomeVm.LoadStatsAsync());
        }

        // Khởi tạo các command điều hướng Admin
        ShowDashboardCommand = new RelayCommand(_ => {
            CurrentViewModel = DashboardVm;
            _ = SafeExecuteAsync(() => DashboardVm.LoadStatsAsync()); 
        });
        ShowApartmentCommand = new RelayCommand(_ => {
            CurrentViewModel = ApartmentVm;
            _ = SafeExecuteAsync(() => ApartmentVm.LoadDataAsync());
        });
        ShowResidentCommand = new RelayCommand(_ => {
            CurrentViewModel = ResidentVm;
            _ = SafeExecuteAsync(() => ResidentVm.LoadDataAsync());
        });
        ShowResidencyCommand = new RelayCommand(_ => {
            CurrentViewModel = ResidencyVm;
            _ = SafeExecuteAsync(() => ResidencyVm.LoadDataAsync());
        });
        ShowVehicleCommand = new RelayCommand(_ => {
            CurrentViewModel = VehicleVm;
            _ = SafeExecuteAsync(() => VehicleVm.LoadDataAsync());
        });
        ShowStatisticsCommand = new RelayCommand(_ => {
            CurrentViewModel = StatisticsVm;
            _ = SafeExecuteAsync(() => StatisticsVm.LoadStatisticsAsync());
        });
        ShowAccountManagementCommand = new RelayCommand(_ => {
            CurrentViewModel = AccountManagementVm;
            _ = SafeExecuteAsync(() => AccountManagementVm.LoadDataAsync());
        });

        // Kết nối điều hướng từ Dashboard đến các trang nghiệp vụ
        DashboardVm.OnNavigateRequested = dest =>
        {
            switch (dest?.ToLowerInvariant())
            {
                case "vehicle":
                    ShowVehicleCommand.Execute(null);
                    break;
                case "residency":
                    ResidencyVm.SelectedTabIndex = 0;
                    ShowResidencyCommand.Execute(null);
                    break;
                case "residency_requests":
                    ResidencyVm.SelectedTabIndex = 2;
                    ShowResidencyCommand.Execute(null);
                    break;
                case "apartment":
                    ShowApartmentCommand.Execute(null);
                    break;
                case "resident":
                    ShowResidentCommand.Execute(null);
                    break;
                case "statistics":
                    ShowStatisticsCommand.Execute(null);
                    break;
            }
        };

        // Khởi tạo các command điều hướng Resident
        ShowResidentHomeCommand = new RelayCommand(_ => {
            CurrentViewModel = ResidentHomeVm;
            _ = SafeExecuteAsync(() => ResidentHomeVm.LoadStatsAsync());
        });
        ShowMyProfileCommand = new RelayCommand(_ => {
            CurrentViewModel = MyProfileVm;
            _ = SafeExecuteAsync(() => MyProfileVm.LoadProfileAsync());
        });
        ShowMyApartmentCommand = new RelayCommand(_ => {
            CurrentViewModel = MyApartmentVm;
            _ = SafeExecuteAsync(() => MyApartmentVm.LoadApartmentDataAsync());
        });
        ShowFamilyMembersCommand = new RelayCommand(_ => {
            CurrentViewModel = FamilyMembersVm;
            _ = SafeExecuteAsync(() => FamilyMembersVm.LoadDataAsync());
        });
        ShowMyVehiclesCommand = new RelayCommand(_ => {
            CurrentViewModel = MyVehiclesVm;
            _ = SafeExecuteAsync(() => MyVehiclesVm.LoadMyVehiclesAsync());
        });
        ShowResidencyHistoryCommand = new RelayCommand(_ => {
            CurrentViewModel = ResidencyHistoryVm;
            _ = SafeExecuteAsync(() => ResidencyHistoryVm.LoadHistoryAsync());
        });

        // Các tính năng đang phát triển (Resident)
        ShowServicesCommand = new RelayCommand(_ => {
            UnderDevelopmentVm.SetFeature("Dịch vụ & Yêu cầu", "Hệ thống đăng ký sửa chữa, bảo trì kỹ thuật và đặt chỗ tiện ích nội khu đang được hoàn thiện.", "🛠");
            CurrentViewModel = UnderDevelopmentVm;
        });
        ShowNotificationsCommand = new RelayCommand(_ => {
            // Mở trực tiếp Trung tâm Thông báo thay vì màn hình UnderDevelopment
            IsNotificationCenterOpen = true;
        });
        ShowFeesCommand = new RelayCommand(_ => {
            UnderDevelopmentVm.SetFeature("Phí dịch vụ & Thanh toán", "Chức năng tra cứu biểu phí, hoá đơn điện nước và thanh toán trực tuyến đang được hoàn thiện.", "💳");
            CurrentViewModel = UnderDevelopmentVm;
        });
        ShowFeedbackCommand = new RelayCommand(_ => {
            UnderDevelopmentVm.SetFeature("Phản ánh & Kiến nghị", "Kênh tiếp nhận ý kiến đóng góp và phản ánh sự cố nội khu đang được phát triển.", "✏️");
            CurrentViewModel = UnderDevelopmentVm;
        });

        // Kết nối nút hành động nhanh trên Trang chủ Cư dân
        ResidentHomeVm.ConfigureNavigation(
            onRegisterTemporaryStay: () =>
            {
                UnderDevelopmentVm.SetFeature("Đăng ký tạm trú", "Hệ thống tiếp nhận hồ sơ khai báo cư trú và đăng ký tạm trú trực tuyến cho cư dân đang được hoàn thiện.", "📄");
                CurrentViewModel = UnderDevelopmentVm;
            },
            onRequestService: () =>
            {
                UnderDevelopmentVm.SetFeature("Yêu cầu dịch vụ", "Hệ thống đăng ký sửa chữa kỹ thuật, bảo trì căn hộ và đặt chỗ tiện ích nội khu đang được hoàn thiện.", "🛠");
                CurrentViewModel = UnderDevelopmentVm;
            },
            onSendFeedback: () =>
            {
                UnderDevelopmentVm.SetFeature("Phản ánh / kiến nghị", "Kênh tiếp nhận ý kiến đóng góp, phản hồi chất lượng và báo cáo sự cố chung cư đang được phát triển.", "📢");
                CurrentViewModel = UnderDevelopmentVm;
            },
            onLookupFees: () =>
            {
                UnderDevelopmentVm.SetFeature("Tra cứu phí dịch vụ", "Chức năng tra cứu bảng kê phí quản lý, tiền điện nước và thanh toán trực tuyến đang được hoàn thiện.", "💳");
                CurrentViewModel = UnderDevelopmentVm;
            },
            onViewAllAnnouncements: () =>
            {
                IsNotificationCenterOpen = true;
            },
            onViewApartmentDetail: () =>
            {
                CurrentViewModel = MyApartmentVm;
                _ = SafeExecuteAsync(() => MyApartmentVm.LoadApartmentDataAsync());
            },
            onViewAllHistory: () =>
            {
                CurrentViewModel = ResidencyHistoryVm;
                _ = SafeExecuteAsync(() => ResidencyHistoryVm.LoadHistoryAsync());
            }
        );

        // Khởi tạo các lệnh Trung tâm Thông báo
        ToggleNotificationCenterCommand = new RelayCommand(_ => IsNotificationCenterOpen = !IsNotificationCenterOpen);
        CloseNotificationCenterCommand = new RelayCommand(_ => IsNotificationCenterOpen = false);
        RefreshNotificationsCommand = new AsyncRelayCommand(_ => RefreshNotificationsAsync());
        FilterNotificationsCommand = new RelayCommand(f => {
            if (f is string filter) SelectedNotificationFilter = filter;
        });
        MarkAllNotificationsAsReadCommand = new AsyncRelayCommand(_ => MarkAllAsReadAsync());
        OpenNotificationItemCommand = new AsyncRelayCommand(p => OpenNotificationAsync(p as NotificationItemViewModel));
        CloseNotificationDetailModalCommand = new RelayCommand(_ => {
            IsNotificationDetailModalOpen = false;
            SelectedNotificationDetail = null;
        });

        // Soạn thảo Rich Text
        ApplyBoldCommand = new RelayCommand(_ => InsertFormatting("<b>", "</b>", "Văn bản in đậm"));
        ApplyItalicCommand = new RelayCommand(_ => InsertFormatting("<i>", "</i>", "Văn bản in nghiêng"));
        ApplyUnderlineCommand = new RelayCommand(_ => InsertFormatting("<u>", "</u>", "Văn bản gạch chân"));
        ApplyColorCommand = new RelayCommand(c => {
            string color = c as string ?? "#2563EB";
            InsertFormatting($"<color={color}>", "</color>", "Văn bản có màu");
        });
        ApplyBulletCommand = new RelayCommand(_ => {
            NewNotificationContent = string.IsNullOrEmpty(NewNotificationContent)
                ? "• "
                : NewNotificationContent + "\n• ";
        });
        ApplyLineBreakCommand = new RelayCommand(_ => {
            NewNotificationContent = (NewNotificationContent ?? "") + "<br/>";
        });

        // Chọn cư dân
        SelectAllResidentsCommand = new RelayCommand(_ => {
            foreach (var r in FilteredSelectableResidents) r.IsSelected = true;
        });
        DeselectAllResidentsCommand = new RelayCommand(_ => {
            foreach (var r in FilteredSelectableResidents) r.IsSelected = false;
        });
        SendAnnouncementCommand = new AsyncRelayCommand(_ => SendAnnouncementAsync());
        OpenPreviewNoticeCommand = new RelayCommand(_ => PrepareAndOpenPreview());
        ClosePreviewNoticeCommand = new RelayCommand(_ => IsPreviewNoticeModalOpen = false);
        ConfirmSendFromPreviewCommand = new AsyncRelayCommand(async _ =>
        {
            IsPreviewNoticeModalOpen = false;
            await SendAnnouncementAsync();
        });
        RefreshSentNotificationsCommand = new AsyncRelayCommand(_ => LoadSentNotificationsAsync());
        OpenSentNotificationDetailCommand = new RelayCommand(param =>
        {
            if (param is SentNotificationItemViewModel sentVm)
            {
                SelectedNotificationDetail = sentVm.ToNotificationDetail();
                IsNotificationDetailModalOpen = true;
            }
        });
        CloseToastCommand = new RelayCommand(_ => IsToastVisible = false);

        LogoutCommand = new RelayCommand(_ => onLogout?.Invoke());

        // Tải số lượng thông báo ban đầu
        _ = RefreshNotificationsAsync();
    }

    private void InsertFormatting(string openTag, string closeTag, string sampleText)
    {
        if (string.IsNullOrEmpty(NewNotificationContent))
        {
            NewNotificationContent = $"{openTag}{sampleText}{closeTag}";
        }
        else
        {
            NewNotificationContent += $" {openTag}{sampleText}{closeTag}";
        }
    }

    public async Task RefreshNotificationsAsync()
    {
        if (_notificationService == null) return;

        try
        {
            var recipients = await _notificationService.GetUserNotificationsAsync(_account.Id);
            Notifications.Clear();
            foreach (var r in recipients)
            {
                Notifications.Add(NotificationItemViewModel.FromRecipient(r));
            }

            ApplyNotificationFilter();
            UnreadNotificationsCount = Notifications.Count(n => n.IsUnread);
        }
        catch (Exception ex)
        {
            ExceptionHandlingService.Handle(ex);
        }
    }

    private void ApplyNotificationFilter()
    {
        FilteredNotifications.Clear();
        var query = Notifications.AsEnumerable();

        if (SelectedNotificationFilter == "Chưa đọc")
        {
            query = query.Where(n => n.IsUnread);
        }
        else if (SelectedNotificationFilter == "Đơn duyệt")
        {
            query = query.Where(n => n.IsApprovalRequest);
        }
        else if (SelectedNotificationFilter == "Thông báo")
        {
            query = query.Where(n => !n.IsApprovalRequest);
        }

        foreach (var item in query)
        {
            FilteredNotifications.Add(item);
        }
    }

    private async Task MarkAllAsReadAsync()
    {
        if (_notificationService == null) return;
        try
        {
            await _notificationService.MarkAllAsReadAsync(_account.Id);
            foreach (var n in Notifications)
            {
                n.IsRead = true;
            }
            UnreadNotificationsCount = 0;
            ApplyNotificationFilter();
            ShowToast("Đã đánh dấu tất cả thông báo là đã đọc.");
        }
        catch (Exception ex)
        {
            ExceptionHandlingService.Handle(ex);
        }
    }

    private async Task OpenNotificationAsync(NotificationItemViewModel? item)
    {
        if (item == null) return;

        // Đánh dấu đã đọc nếu chưa đọc
        if (item.IsUnread && _notificationService != null)
        {
            await _notificationService.MarkAsReadAsync(item.NotificationId, _account.Id);
            item.IsRead = true;
            UnreadNotificationsCount = Notifications.Count(n => n.IsUnread);
            ApplyNotificationFilter();
        }

        // Nếu là Đơn duyệt và có liên kết: tự động chuyển trang tương ứng
        if (item.IsApprovalRequest && item.HasRelatedEntity)
        {
            IsNotificationCenterOpen = false;

            if (item.RelatedEntityType == "Vehicle" && item.RelatedEntityId.HasValue)
            {
                ShowVehicleCommand.Execute(null);
                await VehicleVm.SelectVehicleByIdAsync(item.RelatedEntityId.Value);
                return;
            }

            if (item.RelatedEntityType == "MemberRegistrationRequest" && item.RelatedEntityId.HasValue)
            {
                ShowResidencyCommand.Execute(null);
                await ResidencyVm.SelectMemberRequestByIdAsync(item.RelatedEntityId.Value);
                return;
            }
        }

        // Mở popup xem chi tiết thông báo rich text
        SelectedNotificationDetail = item;
        IsNotificationDetailModalOpen = true;
    }

    public async Task LoadSelectableResidentsAsync()
    {
        if (_unitOfWork == null) return;

        try
        {
            var residents = await _unitOfWork.Residents.GetAllWithDetailsAsync();
            SelectableResidents.Clear();

            // RÀNG BUỘC BẮT BUỘC: Chỉ lấy cư dân có tài khoản đang HOẠT ĐỘNG
            var activeAccountResidents = residents
                .Where(r => r.UserAccount != null && r.UserAccount.IsActive)
                .OrderBy(r => r.FullName)
                .ToList();

            foreach (var r in activeAccountResidents)
            {
                var activeHistory = r.ResidenceHistories.FirstOrDefault(rh => rh.EndDate == null);
                string aptInfo = activeHistory?.Apartment != null 
                    ? $"Căn hộ {activeHistory.Apartment.ApartmentNumber} (Tầng {activeHistory.Apartment.Floor})" 
                    : (r.ResidenceHistories.Count > 0 ? "Đã chuyển đi" : "Chưa đăng ký");

                string status = activeHistory != null ? "Đang cư trú" : "Đã chuyển đi";

                SelectableResidents.Add(new SelectableResidentItemViewModel
                {
                    ResidentId = r.Id,
                    FullName = r.FullName,
                    Username = r.UserAccount!.Username,
                    ApartmentInfo = aptInfo,
                    StatusText = status,
                    AvatarPath = !string.IsNullOrEmpty(r.AvatarPath) ? r.AvatarPath : "/Assets/resident_avatar.png",
                    IsSelected = false
                });
            }

            FilterSelectableResidents();
        }
        catch (Exception ex)
        {
            ExceptionHandlingService.Handle(ex);
        }
    }

    private void FilterSelectableResidents()
    {
        FilteredSelectableResidents.Clear();
        var query = SelectableResidents.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(ResidentSearchKeyword))
        {
            query = query.Where(r => VietnameseSearchHelper.MatchesSearch(ResidentSearchKeyword, r.FullName, r.Username, r.ApartmentInfo));
        }

        foreach (var item in query)
        {
            FilteredSelectableResidents.Add(item);
        }
    }

    private async Task SendAnnouncementAsync()
    {
        NotificationSendError = string.Empty;

        if (string.IsNullOrWhiteSpace(NewNotificationTitle))
        {
            NotificationSendError = "Vui lòng nhập tiêu đề thông báo.";
            return;
        }

        if (string.IsNullOrWhiteSpace(NewNotificationContent))
        {
            NotificationSendError = "Vui lòng nhập nội dung thông báo.";
            return;
        }

        if (_notificationService == null)
        {
            NotificationSendError = "Dịch vụ thông báo chưa sẵn sàng.";
            return;
        }

        TargetAudienceType audience = SelectedTargetAudienceIndex switch
        {
            1 => TargetAudienceType.ActiveResidents,
            2 => TargetAudienceType.MovedOutResidents,
            3 => TargetAudienceType.SpecificResidents,
            _ => TargetAudienceType.All
        };

        List<int>? specificIds = null;
        if (audience == TargetAudienceType.SpecificResidents)
        {
            specificIds = SelectableResidents.Where(r => r.IsSelected).Select(r => r.ResidentId).ToList();
            if (specificIds.Count == 0)
            {
                NotificationSendError = "Vui lòng chọn ít nhất 1 cư dân trong danh sách.";
                return;
            }
        }

        IsSendingNotification = true;
        try
        {
            await _notificationService.CreateAnnouncementAsync(
                _account.Id,
                NewNotificationTitle,
                NewNotificationContent,
                NewNotificationType,
                NewNotificationPriority,
                audience,
                specificIds);

            // Xóa form và reset chọn
            NewNotificationTitle = string.Empty;
            NewNotificationContent = string.Empty;
            foreach (var r in SelectableResidents) r.IsSelected = false;

            SelectedNotificationTab = 2; // Chuyển sang tab Lịch sử gửi để kiểm tra ngay thông báo vừa phát hành
            await LoadSentNotificationsAsync();
            await RefreshNotificationsAsync();

            ShowToast("Tạo và gửi thông báo tới cư dân thành công!");
        }
        catch (Exception ex)
        {
            NotificationSendError = $"Lỗi khi gửi thông báo: {ex.Message}";
        }
        finally
        {
            IsSendingNotification = false;
        }
    }

    private void PrepareAndOpenPreview()
    {
        NotificationSendError = string.Empty;

        if (string.IsNullOrWhiteSpace(NewNotificationTitle))
        {
            NotificationSendError = "Vui lòng nhập tiêu đề thông báo trước khi xem trước.";
            return;
        }

        if (string.IsNullOrWhiteSpace(NewNotificationContent))
        {
            NotificationSendError = "Vui lòng nhập nội dung thông báo trước khi xem trước.";
            return;
        }

        PreviewNoticeTitle = NewNotificationTitle.Trim();
        PreviewNoticeContent = NewNotificationContent;

        PreviewNoticeTypeName = NewNotificationType switch
        {
            NotificationType.Maintenance => "🛠️ Bảo trì - Kỹ thuật",
            NotificationType.FeeNotice => "💳 Phí dịch vụ",
            NotificationType.UrgentNotice => "🚨 Khẩn cấp",
            _ => "📢 Thông báo chung"
        };

        PreviewNoticeTypeBadgeBg = NewNotificationType switch
        {
            NotificationType.Maintenance => "#FEF3C7",
            NotificationType.FeeNotice => "#F3E8FF",
            NotificationType.UrgentNotice => "#FEE2E2",
            _ => "#EFF6FF"
        };

        PreviewNoticeTypeBadgeFg = NewNotificationType switch
        {
            NotificationType.Maintenance => "#D97706",
            NotificationType.FeeNotice => "#7C3AED",
            NotificationType.UrgentNotice => "#DC2626",
            _ => "#1D4ED8"
        };

        PreviewNoticePriorityName = NewNotificationPriority switch
        {
            NotificationPriority.Urgent => "🔥 Khẩn cấp",
            NotificationPriority.Important => "⚡ Quan trọng",
            _ => "Bình thường"
        };

        PreviewNoticePriorityBadgeBg = NewNotificationPriority switch
        {
            NotificationPriority.Urgent => "#FEE2E2",
            NotificationPriority.Important => "#FEF3C7",
            _ => "#F1F5F9"
        };

        PreviewNoticePriorityBadgeFg = NewNotificationPriority switch
        {
            NotificationPriority.Urgent => "#DC2626",
            NotificationPriority.Important => "#B45309",
            _ => "#64748B"
        };

        PreviewNoticeAudienceText = SelectedTargetAudienceIndex switch
        {
            1 => $"Nhóm cư dân đang cư trú ({SelectableResidents.Count(r => r.StatusText == "Đang cư trú")} cư dân có tài khoản hoạt động)",
            2 => $"Nhóm cư dân đã chuyển đi ({SelectableResidents.Count(r => r.StatusText == "Đã chuyển đi")} cư dân có tài khoản hoạt động)",
            3 => GetSpecificAudienceText(),
            _ => $"Toàn bộ cư dân có tài khoản đang hoạt động ({SelectableResidents.Count} cư dân)"
        };

        if (SelectedTargetAudienceIndex == 3 && SelectableResidents.Count(r => r.IsSelected) == 0)
        {
            NotificationSendError = "Vui lòng chọn ít nhất 1 cư dân trong danh sách để xem trước.";
            return;
        }

        IsPreviewNoticeModalOpen = true;
    }

    private string GetSpecificAudienceText()
    {
        var selected = SelectableResidents.Where(r => r.IsSelected).ToList();
        if (selected.Count == 0) return "Chưa chọn cư dân nào";
        string names = string.Join(", ", selected.Take(3).Select(s => s.FullName));
        if (selected.Count > 3) names += $" và {selected.Count - 3} cư dân khác";
        return $"Cư dân được chọn ({selected.Count} người: {names})";
    }

    public async Task LoadSentNotificationsAsync()
    {
        if (_notificationService == null) return;

        IsLoadingSentNotifications = true;
        try
        {
            var sentList = await _notificationService.GetSentAnnouncementsAsync();
            SentNotifications.Clear();
            foreach (var n in sentList)
            {
                int totalRecipients = n.Recipients?.Count ?? 0;
                int readRecipients = n.Recipients?.Count(r => r.IsRead) ?? 0;
                SentNotifications.Add(new SentNotificationItemViewModel
                {
                    NotificationId = n.Id,
                    Title = n.Title,
                    Content = n.Content,
                    Type = n.Type,
                    Priority = n.Priority,
                    TargetAudience = n.TargetAudience,
                    CreatedAt = n.CreatedAt,
                    RecipientCount = totalRecipients,
                    ReadCount = readRecipients
                });
            }
            FilterSentNotifications();
        }
        catch (Exception ex)
        {
            ExceptionHandlingService.Handle(ex);
        }
        finally
        {
            IsLoadingSentNotifications = false;
        }
    }

    private void FilterSentNotifications()
    {
        FilteredSentNotifications.Clear();
        var query = SentNotifications.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SentNotificationSearchKeyword))
        {
            query = query.Where(n => VietnameseSearchHelper.MatchesSearch(SentNotificationSearchKeyword, n.Title, n.TypeName, n.TargetAudienceText));
        }

        foreach (var item in query)
        {
            FilteredSentNotifications.Add(item);
        }
    }

    private void ShowToast(string message)
    {
        ToastMessage = message;
        IsToastVisible = true;

        // Tự động ẩn toast sau 4 giây
        Task.Delay(4000).ContinueWith(_ => {
            IsToastVisible = false;
        }, TaskScheduler.FromCurrentSynchronizationContext());
    }

    private static async Task SafeExecuteAsync(Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            ExceptionHandlingService.Handle(ex);
        }
    }
}
