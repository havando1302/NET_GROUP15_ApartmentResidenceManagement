using System;
using System.Windows.Input;
using ApartmentResidenceManagement.Domain.Entities;
using ApartmentResidenceManagement.Domain.Enums;
using ApartmentResidenceManagement.Wpf.Commands;

namespace ApartmentResidenceManagement.Wpf.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly UserAccount _account;
    private ViewModelBase _currentViewModel;

    // Các ViewModels con được tiêm vào (Admin)
    public DashboardViewModel DashboardVm { get; }
    public ApartmentViewModel ApartmentVm { get; }
    public ResidentViewModel ResidentVm { get; }
    public ResidencyViewModel ResidencyVm { get; }
    public VehicleViewModel VehicleVm { get; }
    public StatisticsViewModel StatisticsVm { get; }

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

    // Navigation Commands (Admin)
    public ICommand ShowDashboardCommand { get; }
    public ICommand ShowApartmentCommand { get; }
    public ICommand ShowResidentCommand { get; }
    public ICommand ShowResidencyCommand { get; }
    public ICommand ShowVehicleCommand { get; }
    public ICommand ShowStatisticsCommand { get; }

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

    public MainViewModel(
        UserAccount account, 
        DashboardViewModel dashboardVm, 
        ApartmentViewModel apartmentVm, 
        ResidentViewModel residentVm,
        ResidencyViewModel residencyVm,
        VehicleViewModel vehicleVm,
        StatisticsViewModel statisticsVm,
        ResidentHomeViewModel residentHomeVm,
        MyProfileViewModel myProfileVm,
        MyApartmentViewModel myApartmentVm,
        FamilyMembersViewModel familyMembersVm,
        MyVehiclesViewModel myVehiclesVm,
        ResidencyHistoryViewModel residencyHistoryVm,
        UnderDevelopmentViewModel? underDevelopmentVm = null,
        Action? onLogout = null)
    {
        _account = account;
        DashboardVm = dashboardVm;
        ApartmentVm = apartmentVm;
        ResidentVm = residentVm;
        ResidencyVm = residencyVm;
        VehicleVm = vehicleVm;
        StatisticsVm = statisticsVm;

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

        string displayName = _account.Resident?.FullName ?? (_account.Username == "admin" ? "Ban quản lý" : "Nguyễn Văn Minh");
        ResidentDisplayName = displayName;
        ResidentApartmentInfo = "Cư dân • Căn hộ A-1203";
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
            _currentViewModel = DashboardVm;
            _ = DashboardVm.LoadStatsAsync();
        }
        else
        {
            _currentViewModel = ResidentHomeVm;
            _ = ResidentHomeVm.LoadStatsAsync();
        }

        // Khởi tạo các command điều hướng Admin
        ShowDashboardCommand = new RelayCommand(_ => {
            CurrentViewModel = DashboardVm;
            _ = DashboardVm.LoadStatsAsync(); 
        });
        ShowApartmentCommand = new RelayCommand(_ => {
            CurrentViewModel = ApartmentVm;
            _ = ApartmentVm.LoadDataAsync();
        });
        ShowResidentCommand = new RelayCommand(_ => {
            CurrentViewModel = ResidentVm;
            _ = ResidentVm.LoadDataAsync();
        });
        ShowResidencyCommand = new RelayCommand(_ => {
            CurrentViewModel = ResidencyVm;
            _ = ResidencyVm.LoadDataAsync();
        });
        ShowVehicleCommand = new RelayCommand(_ => {
            CurrentViewModel = VehicleVm;
            _ = VehicleVm.LoadDataAsync();
        });
        ShowStatisticsCommand = new RelayCommand(_ => {
            CurrentViewModel = StatisticsVm;
            _ = StatisticsVm.LoadStatisticsAsync();
        });

        // Khởi tạo các command điều hướng Resident
        ShowResidentHomeCommand = new RelayCommand(_ => {
            CurrentViewModel = ResidentHomeVm;
            _ = ResidentHomeVm.LoadStatsAsync();
        });
        ShowMyProfileCommand = new RelayCommand(_ => {
            CurrentViewModel = MyProfileVm;
            _ = MyProfileVm.LoadProfileAsync();
        });
        ShowMyApartmentCommand = new RelayCommand(_ => {
            CurrentViewModel = MyApartmentVm;
            _ = MyApartmentVm.LoadApartmentDataAsync();
        });
        ShowFamilyMembersCommand = new RelayCommand(_ => {
            CurrentViewModel = FamilyMembersVm;
            _ = FamilyMembersVm.LoadFamilyMembersAsync();
        });
        ShowMyVehiclesCommand = new RelayCommand(_ => {
            CurrentViewModel = MyVehiclesVm;
            _ = MyVehiclesVm.LoadMyVehiclesAsync();
        });
        ShowResidencyHistoryCommand = new RelayCommand(_ => {
            CurrentViewModel = ResidencyHistoryVm;
            _ = ResidencyHistoryVm.LoadHistoryAsync();
        });

        // Các tính năng đang phát triển (Hiển thị màn hình thông báo thân thiện)
        ShowServicesCommand = new RelayCommand(_ => {
            UnderDevelopmentVm.SetFeature("Dịch vụ & Yêu cầu", "Hệ thống đăng ký sửa chữa, bảo trì kỹ thuật và đặt chỗ tiện ích nội khu đang được hoàn thiện.", "🛠");
            CurrentViewModel = UnderDevelopmentVm;
        });
        ShowNotificationsCommand = new RelayCommand(_ => {
            UnderDevelopmentVm.SetFeature("Thông báo hệ thống", "Trung tâm quản lý và lưu trữ thông báo từ Ban quản lý tòa nhà đang được phát triển.", "🔔");
            CurrentViewModel = UnderDevelopmentVm;
        });
        ShowFeesCommand = new RelayCommand(_ => {
            UnderDevelopmentVm.SetFeature("Phí dịch vụ & Thanh toán", "Chức năng tra cứu biểu phí, hoá đơn điện nước và thanh toán trực tuyến đang được hoàn thiện.", "💳");
            CurrentViewModel = UnderDevelopmentVm;
        });
        ShowFeedbackCommand = new RelayCommand(_ => {
            UnderDevelopmentVm.SetFeature("Phản ánh & Kiến nghị", "Kênh tiếp nhận ý kiến đóng góp và phản ánh sự cố nội khu đang được phát triển.", "✏️");
            CurrentViewModel = UnderDevelopmentVm;
        });

        // Kết nối nút hành động nhanh trên Trang chủ Cư dân tới màn hình Tính năng đang phát triển
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
                UnderDevelopmentVm.SetFeature("Tất cả thông báo", "Trung tâm quản lý, lọc và xem chi tiết các thông báo từ Ban quản lý tòa nhà đang được phát triển.", "🔔");
                CurrentViewModel = UnderDevelopmentVm;
            },
            onViewApartmentDetail: () =>
            {
                CurrentViewModel = MyApartmentVm;
                _ = MyApartmentVm.LoadApartmentDataAsync();
            },
            onViewAllHistory: () =>
            {
                CurrentViewModel = ResidencyHistoryVm;
                _ = ResidencyHistoryVm.LoadHistoryAsync();
            }
        );

        // Command Đăng xuất: gọi callback để đóng MainWindow từ App.xaml.cs
        LogoutCommand = new RelayCommand(_ => onLogout?.Invoke());
    }
}
