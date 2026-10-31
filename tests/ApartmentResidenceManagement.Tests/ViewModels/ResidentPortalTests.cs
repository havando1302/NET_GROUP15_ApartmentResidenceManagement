using System.Threading.Tasks;
using ApartmentResidenceManagement.Domain.Entities;
using ApartmentResidenceManagement.Domain.Enums;
using ApartmentResidenceManagement.Wpf.ViewModels;
using Xunit;

namespace ApartmentResidenceManagement.Tests.ViewModels;

public class ResidentPortalTests
{
    private UserAccount CreateSampleUserAccount(int id = 1, string username = "resident1", string fullName = "Nguyễn Văn Minh", string phone = "0987654321")
    {
        var resident = new Resident
        {
            Id = id,
            FullName = fullName,
            PhoneNumber = phone,
            Email = $"{username}@apartment.vn"
        };

        return new UserAccount
        {
            Id = id,
            Username = username,
            PasswordHash = "dummyhash",
            Role = UserRole.Resident,
            IsActive = true,
            ResidentId = id,
            Resident = resident,
            CreatedAt = System.DateTime.UtcNow
        };
    }

    #region ResidentHomeViewModel Tests
    [Fact]
    public void ResidentHomeViewModel_Announcements_ShouldBeEmptyByDefault()
    {
        // Arrange & Act
        var vm = new ResidentHomeViewModel();

        // Assert
        Assert.Empty(vm.RecentAnnouncements);
        Assert.False(vm.HasAnnouncements);
    }

    [Fact]
    public void ResidentHomeViewModel_Initialize_WithResidentAccount_UpdatesProperties()
    {
        // Arrange
        var vm = new ResidentHomeViewModel();
        var account = CreateSampleUserAccount(2, "resident_hoang", "Hoàng Văn Nam", "0912345678");

        // Act
        vm.Initialize(account);

        // Assert
        Assert.Equal("Hoàng Văn Nam", vm.ResidentName);
        Assert.Equal("Đang cư trú", vm.ResidencyStatus);
        Assert.False(string.IsNullOrWhiteSpace(vm.ApartmentNumber));
    }

    [Fact]
    public void ResidentHomeViewModel_Initialize_WithNoResidentEntity_UsesUsername()
    {
        // Arrange
        var vm = new ResidentHomeViewModel();
        var account = new UserAccount
        {
            Id = 99,
            Username = "account_no_resident",
            PasswordHash = "hash",
            Role = UserRole.Resident,
            IsActive = true,
            Resident = null
        };

        // Act
        vm.Initialize(account);

        // Assert
        Assert.Equal("account_no_resident", vm.ResidentName);
    }

    [Fact]
    public async Task ResidentHomeViewModel_LoadStatsAsync_RefreshesSuccessfully()
    {
        // Arrange
        var vm = new ResidentHomeViewModel();
        var account = CreateSampleUserAccount();
        vm.Initialize(account);

        // Act
        await vm.LoadStatsAsync();

        // Assert
        Assert.False(vm.IsLoading);
        Assert.Equal(string.Empty, vm.ErrorMessage);
    }

    [Fact]
    public void ResidentHomeViewModel_ConfigureNavigation_TriggersCallbacks()
    {
        // Arrange
        bool serviceRequested = false;
        bool feedbackSent = false;
        var vm = new ResidentHomeViewModel();
        vm.ConfigureNavigation(
            onRegisterTemporaryStay: null,
            onRequestService: () => serviceRequested = true,
            onSendFeedback: () => feedbackSent = true,
            onLookupFees: null,
            onViewAllAnnouncements: null);

        // Act
        vm.RequestServiceCommand.Execute(null);
        vm.SendFeedbackCommand.Execute(null);

        // Assert
        Assert.True(serviceRequested);
        Assert.True(feedbackSent);
    }
    #endregion

    #region MyProfileViewModel Tests
    [Fact]
    public void MyProfileViewModel_Initialize_LoadsResidentInformation()
    {
        // Arrange
        var vm = new MyProfileViewModel();
        var account = CreateSampleUserAccount(10, "nguyentrang", "Nguyễn Kiều Trang", "0909123456");

        // Act
        vm.Initialize(account);

        // Assert
        Assert.Equal("CD-0010", vm.ResidentId);
        Assert.Equal("Nguyễn Kiều Trang", vm.FullName);
        Assert.Equal("0909123456", vm.PhoneNumber);
        Assert.False(string.IsNullOrWhiteSpace(vm.Email));
    }

    [Fact]
    public async Task MyProfileViewModel_SaveProfile_WithValidData_UpdatesModelAndResident()
    {
        // Arrange
        var vm = new MyProfileViewModel();
        var account = CreateSampleUserAccount(5, "user5", "Tên Ban Đầu", "0900000001");
        vm.Initialize(account);

        // Act
        vm.FullName = "Tên Sau Cập Nhật";
        vm.PhoneNumber = "0988776655";
        await vm.LoadProfileAsync(); // test refresh
        vm.FullName = "Tên Sau Cập Nhật";
        vm.PhoneNumber = "0988776655";
        vm.SaveProfileCommand.Execute(null);

        // Assert
        Assert.Equal("Cập nhật thông tin cá nhân thành công!", vm.SuccessMessage);
        Assert.Equal(string.Empty, vm.ErrorMessage);
        Assert.Equal("Tên Sau Cập Nhật", account.Resident!.FullName);
        Assert.Equal("0988776655", account.Resident!.PhoneNumber);
    }

    [Fact]
    public void MyProfileViewModel_SaveProfile_WithEmptyFullName_SetsErrorMessage()
    {
        // Arrange
        var vm = new MyProfileViewModel();
        vm.FullName = "   ";
        vm.PhoneNumber = "0901234567";

        // Act
        vm.SaveProfileCommand.Execute(null);

        // Assert
        Assert.Equal("Họ và tên không được để trống.", vm.ErrorMessage);
        Assert.Empty(vm.SuccessMessage);
    }

    [Fact]
    public void MyProfileViewModel_SaveProfile_WithEmptyPhoneNumber_SetsErrorMessage()
    {
        // Arrange
        var vm = new MyProfileViewModel();
        vm.FullName = "Nguyễn Văn A";
        vm.PhoneNumber = "   ";

        // Act
        vm.SaveProfileCommand.Execute(null);

        // Assert
        Assert.Equal("Số điện thoại liên hệ không được để trống.", vm.ErrorMessage);
        Assert.Empty(vm.SuccessMessage);
    }

    [Fact]
    public void MyProfileViewModel_GenderToggles_MaintainExclusivity()
    {
        // Arrange
        var vm = new MyProfileViewModel();

        // Act 1: set female
        vm.IsFemale = true;
        Assert.True(vm.IsFemale);
        Assert.False(vm.IsMale);

        // Act 2: set male
        vm.IsMale = true;
        Assert.True(vm.IsMale);
        Assert.False(vm.IsFemale);
    }
    #endregion

    #region MyApartmentViewModel Tests
    [Fact]
    public void MyApartmentViewModel_Initialize_PopulatesMembersAndHistory()
    {
        // Arrange
        var vm = new MyApartmentViewModel();
        var account = CreateSampleUserAccount();

        // Act
        vm.Initialize(account);

        // Assert
        Assert.True(vm.HasApartment);
        Assert.NotEmpty(vm.HouseholdMembers);
        Assert.NotEmpty(vm.HistoryRecords);
        Assert.False(string.IsNullOrWhiteSpace(vm.ApartmentNumber));
    }

    [Fact]
    public void MyApartmentViewModel_Commands_ExecuteCorrectly()
    {
        // Arrange
        var vm = new MyApartmentViewModel();
        var member = new HouseholdMemberItem { FullName = "Nguyễn Văn Test" };

        // Act
        vm.DownloadCertificateCommand.Execute(null);
        Assert.Contains("PDF", vm.StatusMessage);

        vm.AddMemberCommand.Execute(null);
        Assert.Contains("thêm thành viên", vm.StatusMessage);

        vm.EditMemberCommand.Execute(member);
        Assert.Contains("Nguyễn Văn Test", vm.StatusMessage);
    }
    #endregion

    #region FamilyMembersViewModel & ResidencyHistoryViewModel Tests
    [Fact]
    public void FamilyMembersViewModel_Initialize_PopulatesMembersCorrectly()
    {
        // Arrange
        var vm = new FamilyMembersViewModel();
        var account = CreateSampleUserAccount();

        // Act
        vm.Initialize(account);

        // Assert
        Assert.True(vm.HasMembers);
        Assert.True(vm.TotalMembers >= 1);
        Assert.Equal(vm.Members.Count, vm.TotalMembers);
    }

    [Fact]
    public void ResidencyHistoryViewModel_Initialize_LoadsHistoryAndResidentInfo()
    {
        // Arrange
        var vm = new ResidencyHistoryViewModel();
        var account = CreateSampleUserAccount(3, "resident3", "Trần Thị B");

        // Act
        vm.Initialize(account);

        // Assert
        Assert.Equal("Trần Thị B", vm.ResidentName);
        Assert.Equal("CD-0003", vm.ResidentCode);
        Assert.True(vm.HasHistory);
        Assert.True(vm.TotalEvents >= 1);
    }
    #endregion
}
