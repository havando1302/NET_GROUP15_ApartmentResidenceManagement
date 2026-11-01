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
    public async Task MyProfileViewModel_SaveProfile_UpdatesPhoneNumberAndEmail_LeavesHometownUnchanged()
    {
        // Arrange
        var vm = new MyProfileViewModel();
        var account = CreateSampleUserAccount(5, "user5", "Nguyễn Văn Minh", "0900000001");
        vm.Initialize(account);
        var initialHometown = vm.Hometown;

        // Act
        vm.EditPhoneNumber = "0988776655";
        vm.EditEmail = "new_email@apartment.vn";
        vm.SaveProfileCommand.Execute(null);

        // Assert
        Assert.Equal("Lưu thay đổi thông tin liên hệ thành công!", vm.SuccessMessage);
        Assert.Equal(string.Empty, vm.ErrorMessage);
        Assert.Equal("0988776655", vm.PhoneNumber);
        Assert.Equal("new_email@apartment.vn", vm.Email);
        Assert.Equal(initialHometown, vm.Hometown);
        Assert.Equal("0988776655", account.Resident!.PhoneNumber);
        Assert.Equal("new_email@apartment.vn", account.Resident!.Email);
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

    [Fact]
    public void MyProfileViewModel_AvatarPresets_AndSelectionCommand_WorkCorrectly()
    {
        // Arrange
        var vm = new MyProfileViewModel();
        Assert.Equal(10, vm.AvailableAvatars.Count);
        Assert.False(vm.IsAvatarPickerOpen);

        // Act: toggle picker
        vm.ToggleAvatarPickerCommand.Execute(null);
        Assert.True(vm.IsAvatarPickerOpen);

        // Act: select avatar preset
        var chosenPreset = vm.AvailableAvatars[1]; // Trần Thị Hoa
        vm.SelectAvatarCommand.Execute(chosenPreset);

        // Assert
        Assert.Equal("/Assets/wife_avatar.png", vm.AvatarPath);
        Assert.False(vm.IsAvatarPickerOpen);
        Assert.Contains("Trần Thị Hoa", vm.SuccessMessage);
    }

    [Fact]
    public void MyProfileViewModel_EditModal_AllowsEditingEmailAndPhone()
    {
        // Arrange
        var vm = new MyProfileViewModel();
        var account = CreateSampleUserAccount();
        vm.Initialize(account);

        // Act: Open modal
        vm.OpenEditModalCommand.Execute(null);
        Assert.True(vm.IsEditModalOpen);
        Assert.Equal(vm.PhoneNumber, vm.EditPhoneNumber);
        Assert.Equal(vm.Email, vm.EditEmail);

        // Act: Edit and Save
        vm.EditPhoneNumber = "0933888999";
        vm.EditEmail = "new_minh@apartment.vn";
        vm.SaveProfileCommand.Execute(null);

        // Assert
        Assert.False(vm.IsEditModalOpen);
        Assert.Equal("0933888999", vm.PhoneNumber);
        Assert.Equal("new_minh@apartment.vn", vm.Email);
        Assert.Contains("thành công", vm.SuccessMessage);
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

    [Fact]
    public void MyApartmentViewModel_ActiveResidency_EndDateIsUndetermined_AndDurationIsDynamic()
    {
        // Arrange
        var vm = new MyApartmentViewModel();

        // Assert
        Assert.Equal("Chưa xác định", vm.ResidencyEndDate);
        Assert.Equal("10/01/2026", vm.ResidencyStartDate);
        Assert.Contains("ngày (tính đến hôm nay)", vm.ResidencyDuration);
        Assert.True(vm.ResidentMembers.Count >= 4);
    }

    [Fact]
    public void MyApartmentViewModel_SelectGalleryImage_UpdatesCurrentImage()
    {
        // Arrange
        var vm = new MyApartmentViewModel();
        var targetImage = vm.GalleryImages[1];

        // Act
        vm.SelectGalleryImageCommand.Execute(targetImage);

        // Assert
        Assert.Equal(targetImage.ImagePath, vm.CurrentApartmentImage);
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

        // Act: search by relationship
        vm.SearchText = "Con";
        Assert.Equal(2, vm.FilteredMembers.Count);

        // Act: clear search
        vm.SearchText = string.Empty;
        Assert.Equal(4, vm.FilteredMembers.Count);
    }

    [Fact]
    public void FamilyMembersViewModel_EditMemberModal_UpdatesMemberPhoneAndStatus()
    {
        // Arrange
        var vm = new FamilyMembersViewModel();
        var member = vm.FilteredMembers[1]; // Trần Thị Hoa
        var initialHometown = member.Hometown;

        // Act: Open edit member modal
        vm.OpenEditMemberModalCommand.Execute(member);
        Assert.True(vm.IsEditMemberModalOpen);
        Assert.Equal("Trần Thị Hoa", vm.EditMemberFullName);

        // Act: Update phone and status
        vm.EditMemberPhone = "0911223344";
        vm.EditMemberStatus = "Tạm vắng";
        vm.SaveMemberEditCommand.Execute(null);

        // Assert
        Assert.False(vm.IsEditMemberModalOpen);
        Assert.Equal("0911223344", member.PhoneNumber);
        Assert.Equal("Tạm vắng", member.Status);
        Assert.Equal(initialHometown, member.Hometown); // Hometown must remain unchanged
        Assert.Contains("Trần Thị Hoa", vm.EditSuccessMessage);
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
    public void ResidencyHistoryViewModel_ActiveSession_EndDateIsUndetermined_AndSummaryContainsChuaXacDinh()
    {
        // Arrange
        var vm = new ResidencyHistoryViewModel();
        var activeSession = vm.Sessions[0];

        // Assert
        Assert.True(activeSession.IsActive);
        Assert.Equal("Chưa xác định", activeSession.EndDate);
        Assert.Contains("Chưa xác định", activeSession.DateRange);
        Assert.Contains("ngày (tính đến hôm nay)", activeSession.Duration);
        Assert.Contains("Chưa xác định", activeSession.SessionSummaryText);
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
