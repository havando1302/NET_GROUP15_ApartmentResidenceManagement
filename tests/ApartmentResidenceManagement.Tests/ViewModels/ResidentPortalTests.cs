using System.Threading.Tasks;
using ApartmentResidenceManagement.Domain.Entities;
using ApartmentResidenceManagement.Domain.Enums;
using ApartmentResidenceManagement.Wpf.ViewModels;
using Xunit;

namespace ApartmentResidenceManagement.Tests.ViewModels;

public class ResidentPortalTests
{
    private UserAccount CreateSampleUserAccount(int id = 1, string username = "resident1", string fullName = "Nguyễn Văn Minh", string phone = "0987 654 321")
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
    public void ResidentHomeViewModel_Initialize_WithResidentAccount_UpdatesPropertiesAndStats()
    {
        // Arrange
        var vm = new ResidentHomeViewModel();
        var account = CreateSampleUserAccount(2, "resident_hoang", "Hoàng Văn Nam", "0912345678");

        // Act
        vm.Initialize(account);

        // Assert
        Assert.Equal("Hoàng Văn Nam", vm.ResidentName);
        Assert.Equal("Đang cư trú", vm.ResidencyStatus);
        Assert.Equal("A-1203", vm.ApartmentNumber);
        Assert.Equal(4, vm.FamilyCount);
        Assert.Equal(2, vm.ResidencyChangesCount);
        Assert.Equal(0, vm.PendingRequestsCount);
        Assert.Equal(3, vm.NewAnnouncementsCount);
    }

    [Fact]
    public void ResidentHomeViewModel_QuickNavigationCommands_ExecuteConfiguredActions()
    {
        // Arrange
        bool apartmentNavigated = false;
        bool historyNavigated = false;
        var vm = new ResidentHomeViewModel();
        vm.ConfigureNavigation(
            onRegisterTemporaryStay: null,
            onRequestService: null,
            onSendFeedback: null,
            onLookupFees: null,
            onViewAllAnnouncements: null,
            onViewApartmentDetail: () => apartmentNavigated = true,
            onViewAllHistory: () => historyNavigated = true);

        // Act
        vm.ViewApartmentDetailCommand.Execute(null);
        vm.ViewAllHistoryCommand.Execute(null);

        // Assert
        Assert.True(apartmentNavigated);
        Assert.True(historyNavigated);
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
    public async Task MyProfileViewModel_SaveProfile_UpdatesPhoneNumberAndAddress()
    {
        // Arrange
        var vm = new MyProfileViewModel();
        var account = CreateSampleUserAccount(5, "user5", "Nguyễn Văn Minh", "0900000001");
        vm.Initialize(account);

        // Act
        vm.EditPhoneNumber = "0988776655";
        vm.EditContactAddress = "Cầu Giấy, Hà Nội";
        vm.SaveProfileCommand.Execute(null);

        // Assert
        Assert.Equal("Lưu thay đổi thông tin liên hệ thành công!", vm.SuccessMessage);
        Assert.Equal(string.Empty, vm.ErrorMessage);
        Assert.Equal("0988776655", vm.PhoneNumber);
        Assert.Equal("Cầu Giấy, Hà Nội", vm.Hometown);
        Assert.Equal("0988776655", account.Resident!.PhoneNumber);
    }

    [Fact]
    public void MyProfileViewModel_SaveProfile_WithEmptyPhone_SetsErrorMessage()
    {
        // Arrange
        var vm = new MyProfileViewModel();
        vm.EditPhoneNumber = "   ";

        // Act
        vm.SaveProfileCommand.Execute(null);

        // Assert
        Assert.Equal("Số điện thoại liên hệ không được để trống.", vm.ErrorMessage);
        Assert.Empty(vm.SuccessMessage);
    }

    [Fact]
    public void MyProfileViewModel_ResetCommand_RestoresOriginalValues()
    {
        // Arrange
        var vm = new MyProfileViewModel();
        vm.PhoneNumber = "0987654321";
        vm.Hometown = "Đan Phượng, Hà Nội";
        vm.EditPhoneNumber = "0111222333";
        vm.EditContactAddress = "Đống Đa, Hà Nội";

        // Act
        vm.ResetCommand.Execute(null);

        // Assert
        Assert.Equal("0987654321", vm.EditPhoneNumber);
        Assert.Equal("Đan Phượng, Hà Nội", vm.EditContactAddress);
        Assert.Contains("khôi phục", vm.SuccessMessage);
    }
    #endregion

    #region MyApartmentViewModel Tests
    [Fact]
    public void MyApartmentViewModel_Initialize_PopulatesApartmentAndResidentMembers()
    {
        // Arrange
        var vm = new MyApartmentViewModel();
        var account = CreateSampleUserAccount();

        // Act
        vm.Initialize(account);

        // Assert
        Assert.Equal("A-1203", vm.ApartmentNumber);
        Assert.Equal("A", vm.BuildingBlock);
        Assert.Equal(4, vm.ResidentMembers.Count);
        Assert.Equal("Đang cư trú", vm.ResidencyStatus);
        Assert.Equal(20, vm.BuildingFloors);
    }
    #endregion

    #region FamilyMembersViewModel Tests
    [Fact]
    public void FamilyMembersViewModel_AccordionToggle_ExpandsAndCollapses()
    {
        // Arrange
        var vm = new FamilyMembersViewModel();
        var member = vm.FilteredMembers[0];

        // Act & Assert
        Assert.True(member.IsExpanded);
        Assert.Equal("ᐱ", member.ExpandIcon);

        member.ToggleExpandCommand.Execute(null);
        Assert.False(member.IsExpanded);
        Assert.Equal("ᐯ", member.ExpandIcon);

        member.ToggleExpandCommand.Execute(null);
        Assert.True(member.IsExpanded);
    }

    [Fact]
    public void FamilyMembersViewModel_SearchText_FiltersMembersList()
    {
        // Arrange
        var vm = new FamilyMembersViewModel();
        Assert.Equal(4, vm.FilteredMembers.Count);

        // Act: search by name
        vm.SearchText = "Hoa";

        // Assert
        Assert.Single(vm.FilteredMembers);
        Assert.Equal("Trần Thị Hoa", vm.FilteredMembers[0].FullName);

        // Act: clear search
        vm.SearchText = string.Empty;
        Assert.Equal(4, vm.FilteredMembers.Count);
    }
    #endregion

    #region ResidencyHistoryViewModel Tests
    [Fact]
    public void ResidencyHistoryViewModel_SelectSession_UpdatesSelectedSessionAndDetails()
    {
        // Arrange
        var vm = new ResidencyHistoryViewModel();
        Assert.NotNull(vm.SelectedSession);
        Assert.Equal("A-1203", vm.SelectedSession.ApartmentNumber);

        // Act: select second session (A-1008)
        var secondSession = vm.Sessions[1];
        vm.SelectSessionCommand.Execute(secondSession);

        // Assert
        Assert.Equal("A-1008", vm.SelectedSession.ApartmentNumber);
        Assert.True(secondSession.IsSelected);
        Assert.False(vm.Sessions[0].IsSelected);
    }

    [Fact]
    public void ResidencyHistoryViewModel_Initialize_LoadsTimelineEvents()
    {
        // Arrange
        var vm = new ResidencyHistoryViewModel();
        var account = CreateSampleUserAccount();

        // Act
        vm.Initialize(account);

        // Assert
        Assert.Equal(2, vm.TotalResidencyCount);
        Assert.Equal(2, vm.TimelineEvents.Count);
        Assert.True(vm.HasHistory);
    }
    #endregion
}
