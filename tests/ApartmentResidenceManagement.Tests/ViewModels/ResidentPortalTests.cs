using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ApartmentResidenceManagement.Application.Services;
using ApartmentResidenceManagement.Domain.Entities;
using ApartmentResidenceManagement.Domain.Enums;
using ApartmentResidenceManagement.Domain.Interfaces;
using ApartmentResidenceManagement.Wpf.ViewModels;
using Moq;
using Xunit;

namespace ApartmentResidenceManagement.Tests.ViewModels;

public class ResidentPortalTests
{
    private readonly Mock<IUnitOfWork> _mockUow;
    private readonly Mock<IResidentRepository> _mockResidentRepo;
    private readonly Mock<IApartmentRepository> _mockApartmentRepo;
    private readonly Mock<IResidenceHistoryRepository> _mockResidenceHistoryRepo;
    private readonly Mock<IMemberRegistrationRequestRepository> _mockRequestRepo;
    private readonly ResidenceService _residenceService;
    private readonly ResidentService _residentService;
    private readonly MemberRequestService _memberRequestService;

    public ResidentPortalTests()
    {
        _mockUow = new Mock<IUnitOfWork>();
        _mockResidentRepo = new Mock<IResidentRepository>();
        _mockApartmentRepo = new Mock<IApartmentRepository>();
        _mockResidenceHistoryRepo = new Mock<IResidenceHistoryRepository>();
        _mockRequestRepo = new Mock<IMemberRegistrationRequestRepository>();

        _mockUow.Setup(u => u.Residents).Returns(_mockResidentRepo.Object);
        _mockUow.Setup(u => u.Apartments).Returns(_mockApartmentRepo.Object);
        _mockUow.Setup(u => u.ResidenceHistories).Returns(_mockResidenceHistoryRepo.Object);
        _mockUow.Setup(u => u.MemberRegistrationRequests).Returns(_mockRequestRepo.Object);

        _residenceService = new ResidenceService(_mockUow.Object);
        _residentService = new ResidentService(_mockUow.Object);
        _memberRequestService = new MemberRequestService(_mockUow.Object, _residenceService);
    }

    private UserAccount CreateSampleUserAccount(int id = 1, string username = "resident1", string fullName = "Nguyễn Văn Minh", string phone = "0987 654 321")
    {
        var resident = new Resident
        {
            Id = id,
            FullName = fullName,
            PhoneNumber = phone,
            Email = $"{username}@apartment.vn",
            DateOfBirth = new DateTime(2000, 1, 1),
            IdentityCard = "012345678901"
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

    private void SetupMockResidenceData(int residentId, int apartmentId = 101, string aptNumber = "A-1203")
    {
        var apt = new Apartment { Id = apartmentId, ApartmentNumber = aptNumber, Floor = 12, Area = 72, Status = ApartmentStatus.Occupied };
        var resident = new Resident { Id = residentId, FullName = "Nguyễn Văn Minh", PhoneNumber = "0987 654 321", Email = "minh@apartment.vn", DateOfBirth = new DateTime(2000, 1, 1) };
        var wife = new Resident { Id = 202, FullName = "Trần Thị Hoa", PhoneNumber = "0911223344", Email = "hoa@apartment.vn", DateOfBirth = new DateTime(2002, 7, 20) };

        var activeResidence = new ResidenceHistory
        {
            Id = 1,
            ResidentId = residentId,
            ApartmentId = apartmentId,
            Apartment = apt,
            Resident = resident,
            RelationshipType = RelationshipType.Owner,
            StartDate = new DateTime(2026, 1, 10),
            EndDate = null,
            IsActive = true
        };

        var wifeResidence = new ResidenceHistory
        {
            Id = 2,
            ResidentId = 202,
            ApartmentId = apartmentId,
            Apartment = apt,
            Resident = wife,
            RelationshipType = RelationshipType.FamilyMember,
            StartDate = new DateTime(2026, 1, 10),
            EndDate = null,
            IsActive = true
        };

        var oldResidence = new ResidenceHistory
        {
            Id = 3,
            ResidentId = residentId,
            ApartmentId = 99,
            Apartment = new Apartment { Id = 99, ApartmentNumber = "A-1008", Floor = 10, Area = 65, Status = ApartmentStatus.Empty },
            Resident = resident,
            RelationshipType = RelationshipType.Owner,
            StartDate = new DateTime(2024, 7, 1),
            EndDate = new DateTime(2026, 1, 9),
            IsActive = false
        };

        _mockResidenceHistoryRepo.Setup(r => r.GetActiveByResidentIdAsync(residentId))
            .ReturnsAsync(activeResidence);

        _mockResidenceHistoryRepo.Setup(r => r.GetActiveByApartmentIdAsync(apartmentId))
            .ReturnsAsync(new List<ResidenceHistory> { activeResidence, wifeResidence });

        _mockResidenceHistoryRepo.Setup(r => r.GetHistoryByResidentIdAsync(residentId))
            .ReturnsAsync(new List<ResidenceHistory> { activeResidence, oldResidence });

        _mockApartmentRepo.Setup(r => r.GetByIdAsync(apartmentId))
            .ReturnsAsync(apt);

        _mockResidentRepo.Setup(r => r.GetByIdAsync(residentId))
            .ReturnsAsync(resident);
    }

    #region ResidentHomeViewModel Tests
    [Fact]
    public async Task ResidentHomeViewModel_Initialize_WithActiveResidence_LoadsPropertiesAndStats()
    {
        // Arrange
        int resId = 2;
        SetupMockResidenceData(resId, 101, "A-1203");
        var vm = new ResidentHomeViewModel(_residenceService);
        var account = CreateSampleUserAccount(resId, "resident_hoang", "Hoàng Văn Nam", "0912345678");

        // Act
        vm.Initialize(account);
        await vm.LoadStatsAsync();

        // Assert
        Assert.Equal("Hoàng Văn Nam", vm.ResidentName);
        Assert.Equal("Đang cư trú", vm.ResidencyStatus);
        Assert.Equal("A-1203", vm.ApartmentNumber);
        Assert.Equal(2, vm.FamilyCount);
        Assert.Equal(2, vm.ResidencyChangesCount);
        Assert.Equal(0, vm.PendingRequestsCount);
        Assert.Equal(3, vm.NewAnnouncementsCount);
    }

    [Fact]
    public async Task ResidentHomeViewModel_Initialize_WithoutActiveResidence_ShowsNoApartment()
    {
        // Arrange
        int resId = 999;
        _mockResidenceHistoryRepo.Setup(r => r.GetActiveByResidentIdAsync(resId))
            .ReturnsAsync((ResidenceHistory?)null);
        _mockResidenceHistoryRepo.Setup(r => r.GetHistoryByResidentIdAsync(resId))
            .ReturnsAsync(new List<ResidenceHistory>());

        var vm = new ResidentHomeViewModel(_residenceService);
        var account = CreateSampleUserAccount(resId, "no_apt_user", "Cư Dân Mới", "0999999999");

        // Act
        vm.Initialize(account);
        await vm.LoadStatsAsync();

        // Assert
        Assert.Equal("Chưa có căn hộ", vm.ApartmentNumber);
        Assert.Equal("Chưa có cư trú hoạt động", vm.ResidencyStatus);
        Assert.Equal(0, vm.FamilyCount);
        Assert.Equal(0, vm.ResidencyChangesCount);
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
        var vm = new MyProfileViewModel(_residentService);
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
    public async Task MyProfileViewModel_SaveProfile_UpdatesPhoneNumberAndEmail_CallsResidentService()
    {
        // Arrange
        int resId = 5;
        var resident = new Resident { Id = resId, FullName = "Nguyễn Văn Minh", PhoneNumber = "0900000001", Email = "old@apartment.vn" };
        _mockResidentRepo.Setup(r => r.GetByIdAsync(resId)).ReturnsAsync(resident);
        _mockUow.Setup(u => u.CompleteAsync()).ReturnsAsync(1);

        var vm = new MyProfileViewModel(_residentService);
        var account = CreateSampleUserAccount(resId, "user5", "Nguyễn Văn Minh", "0900000001");
        account.Resident = resident;
        vm.Initialize(account);
        var initialHometown = vm.Hometown;

        // Act
        vm.EditPhoneNumber = "0988776655";
        vm.EditEmail = "new_email@apartment.vn";
        await ((ApartmentResidenceManagement.Wpf.Commands.AsyncRelayCommand)vm.SaveProfileCommand).ExecuteAsync(null);

        // Assert
        Assert.Contains("thành công", vm.SuccessMessage);
        Assert.Equal(string.Empty, vm.ErrorMessage);
        Assert.Equal("0988776655", vm.PhoneNumber);
        Assert.Equal("new_email@apartment.vn", vm.Email);
        Assert.Equal(initialHometown, vm.Hometown);
        Assert.Equal("0988776655", resident.PhoneNumber);
        Assert.Equal("new_email@apartment.vn", resident.Email);
        _mockResidentRepo.Verify(r => r.Update(It.IsAny<Resident>()), Times.Once);
        _mockUow.Verify(u => u.CompleteAsync(), Times.Once);
    }

    [Fact]
    public void MyProfileViewModel_SaveProfile_WithEmptyPhone_SetsErrorMessage()
    {
        // Arrange
        var vm = new MyProfileViewModel(_residentService);
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
        var account = CreateSampleUserAccount(1, "minh", "Nguyễn Văn Minh", "0911223344");
        vm.Initialize(account);
        vm.EditPhoneNumber = "0999999999";
        vm.EditEmail = "modified@gmail.com";

        // Act
        vm.ResetCommand.Execute(null);

        // Assert
        Assert.Equal("0911223344", vm.EditPhoneNumber);
        Assert.Equal("minh@apartment.vn", vm.EditEmail);
    }

    [Fact]
    public void MyProfileViewModel_SelectAvatar_UpdatesAvatarPath()
    {
        // Arrange
        var vm = new MyProfileViewModel();
        string newPath = "/Assets/avatar_daughter.png";

        // Act
        vm.SelectAvatarCommand.Execute(newPath);

        // Assert
        Assert.Equal(newPath, vm.AvatarPath);
        Assert.False(vm.IsAvatarPickerOpen);
        Assert.Contains("thành công", vm.SuccessMessage);
    }
    #endregion

    #region MyApartmentViewModel Tests
    [Fact]
    public async Task MyApartmentViewModel_Initialize_PopulatesApartmentAndResidentMembers()
    {
        // Arrange
        int resId = 1;
        SetupMockResidenceData(resId, 101, "A-1203");
        var vm = new MyApartmentViewModel(_residenceService);
        var account = CreateSampleUserAccount(resId);

        // Act
        vm.Initialize(account);
        await vm.LoadApartmentDataAsync();

        // Assert
        Assert.Equal("A-1203", vm.ApartmentNumber);
        Assert.Equal("A", vm.BuildingBlock);
        Assert.Equal(2, vm.ResidentMembers.Count);
        Assert.Equal("Đang cư trú", vm.ResidencyStatus);
        Assert.Equal(20, vm.BuildingFloors);
    }

    [Fact]
    public async Task MyApartmentViewModel_ActiveResidency_EndDateIsUndetermined_AndDurationIsDynamic()
    {
        // Arrange
        int resId = 1;
        SetupMockResidenceData(resId);
        var vm = new MyApartmentViewModel(_residenceService);
        var account = CreateSampleUserAccount(resId);

        // Act
        vm.Initialize(account);
        await vm.LoadApartmentDataAsync();

        // Assert
        Assert.Equal("Chưa xác định", vm.ResidencyEndDate);
        Assert.Equal("10/01/2026", vm.ResidencyStartDate);
        Assert.Contains("ngày (tính đến hôm nay)", vm.ResidencyDuration);
        Assert.True(vm.ResidentMembers.Count >= 2);
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
    public async Task FamilyMembersViewModel_SearchText_FiltersMembersList()
    {
        // Arrange
        int resId = 1;
        SetupMockResidenceData(resId);
        var vm = new FamilyMembersViewModel(_residenceService);
        var account = CreateSampleUserAccount(resId);

        // Act: Initialize & load
        vm.Initialize(account);
        await vm.LoadFamilyMembersAsync();
        Assert.Equal(2, vm.FilteredMembers.Count);

        // Act: search by name
        vm.SearchText = "Hoa";

        // Assert
        Assert.Single(vm.FilteredMembers);
        Assert.Equal("Trần Thị Hoa", vm.FilteredMembers[0].FullName);

        // Act: search by relationship
        vm.SearchText = "Chủ hộ";
        Assert.Single(vm.FilteredMembers);
        Assert.Equal("Nguyễn Văn Minh", vm.FilteredMembers[0].FullName);

        // Act: clear search
        vm.SearchText = string.Empty;
        Assert.Equal(2, vm.FilteredMembers.Count);
    }

    [Fact]
    public async Task FamilyMembersViewModel_EditMemberModal_UpdatesMemberPhoneAndStatus()
    {
        // Arrange
        int resId = 1;
        SetupMockResidenceData(resId);
        var vm = new FamilyMembersViewModel(_residenceService);
        var account = CreateSampleUserAccount(resId);
        vm.Initialize(account);
        await vm.LoadFamilyMembersAsync();

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
        Assert.Equal(initialHometown, member.Hometown);
        Assert.Contains("Trần Thị Hoa", vm.EditSuccessMessage);
    }

    [Fact]
    public async Task FamilyMembersViewModel_AddRequestModal_SubmitsSuccessfullyAndLoadsRequests()
    {
        // Arrange
        int resId = 1;
        int aptId = 101;
        SetupMockResidenceData(resId, aptId, "A-1203");
        var requestsList = new List<MemberRegistrationRequest>();
        _mockRequestRepo.Setup(r => r.GetByApartmentIdAsync(aptId))
            .ReturnsAsync(() => requestsList.ToList());
        _mockRequestRepo.Setup(r => r.AddAsync(It.IsAny<MemberRegistrationRequest>()))
            .Callback<MemberRegistrationRequest>(r => requestsList.Add(r));

        var vm = new FamilyMembersViewModel(_residenceService, _memberRequestService);
        var account = CreateSampleUserAccount(resId);
        vm.Initialize(account);
        await vm.LoadDataAsync();

        // Act: Open modal
        vm.OpenAddRequestModalCommand.Execute(null);
        Assert.True(vm.IsAddRequestModalOpen);

        // Fill data
        vm.NewMemberFullName = "Nguyễn Văn Em";
        vm.NewMemberDateOfBirth = new DateTime(2008, 10, 10);
        vm.NewMemberIdentityCard = "012345678999";
        vm.NewMemberPhoneNumber = "0988776655";
        vm.SelectedRelationshipType = RelationshipType.FamilyMember;

        // Submit
        if (vm.SubmitAddRequestCommand is ApartmentResidenceManagement.Wpf.Commands.AsyncRelayCommand asyncCmd)
        {
            await asyncCmd.ExecuteAsync(null);
        }
        else
        {
            vm.SubmitAddRequestCommand.Execute(null);
        }

        // Assert
        Assert.True(string.IsNullOrEmpty(vm.AddRequestErrorMessage), vm.AddRequestErrorMessage);
        Assert.False(vm.IsAddRequestModalOpen);
        Assert.Contains("Nguyễn Văn Em", vm.AddRequestSuccessMessage);
        Assert.Equal(1, vm.TotalRequestsCount);
        Assert.Equal(1, vm.PendingRequestsCount);
        Assert.Single(vm.FilteredRequests);
    }

    [Fact]
    public async Task FamilyMembersViewModel_FilterRequests_FiltersByStatusCorrectly()
    {
        // Arrange
        int resId = 1;
        int aptId = 101;
        SetupMockResidenceData(resId, aptId, "A-1203");

        var requestsList = new List<MemberRegistrationRequest>
        {
            new MemberRegistrationRequest { Id = 1, ApartmentId = aptId, FullName = "Thành viên 1", Status = RequestStatus.Pending, CreatedAt = DateTime.Now },
            new MemberRegistrationRequest { Id = 2, ApartmentId = aptId, FullName = "Thành viên 2", Status = RequestStatus.Approved, CreatedAt = DateTime.Now },
            new MemberRegistrationRequest { Id = 3, ApartmentId = aptId, FullName = "Thành viên 3", Status = RequestStatus.Rejected, CreatedAt = DateTime.Now, RejectReason = "Thiếu giấy tờ" }
        };

        _mockRequestRepo.Setup(r => r.GetByApartmentIdAsync(aptId))
            .ReturnsAsync(requestsList);

        var vm = new FamilyMembersViewModel(_residenceService, _memberRequestService);
        var account = CreateSampleUserAccount(resId);
        vm.Initialize(account);
        await vm.LoadDataAsync();

        // Default filter: Tất cả
        Assert.Equal(3, vm.FilteredRequests.Count);

        // Filter: Chờ duyệt
        vm.SelectedRequestFilter = "Chờ duyệt";
        Assert.Single(vm.FilteredRequests);
        Assert.Equal("Thành viên 1", vm.FilteredRequests[0].FullName);

        // Filter: Đã duyệt
        vm.SelectedRequestFilter = "Đã duyệt";
        Assert.Single(vm.FilteredRequests);
        Assert.Equal("Thành viên 2", vm.FilteredRequests[0].FullName);

        // Filter: Từ chối
        vm.SelectedRequestFilter = "Từ chối";
        Assert.Single(vm.FilteredRequests);
        Assert.Equal("Thành viên 3", vm.FilteredRequests[0].FullName);
    }
    #endregion

    #region ResidencyHistoryViewModel Tests
    [Fact]
    public async Task ResidencyHistoryViewModel_SelectSession_UpdatesSelectedSessionAndDetails()
    {
        // Arrange
        int resId = 1;
        SetupMockResidenceData(resId);
        var vm = new ResidencyHistoryViewModel(_residenceService);
        var account = CreateSampleUserAccount(resId);

        // Act
        vm.Initialize(account);
        await vm.LoadHistoryAsync();

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
    public async Task ResidencyHistoryViewModel_ActiveSession_EndDateIsUndetermined_AndSummaryContainsChuaXacDinh()
    {
        // Arrange
        int resId = 1;
        SetupMockResidenceData(resId);
        var vm = new ResidencyHistoryViewModel(_residenceService);
        var account = CreateSampleUserAccount(resId);

        // Act
        vm.Initialize(account);
        await vm.LoadHistoryAsync();

        var activeSession = vm.Sessions[0];

        // Assert
        Assert.True(activeSession.IsActive);
        Assert.Equal("Chưa xác định", activeSession.EndDate);
        Assert.Contains("Chưa xác định", activeSession.DateRange);
        Assert.Contains("ngày (tính đến hôm nay)", activeSession.Duration);
        Assert.Contains("Chưa xác định", activeSession.SessionSummaryText);
    }

    [Fact]
    public async Task ResidencyHistoryViewModel_Initialize_LoadsTimelineEvents()
    {
        // Arrange
        int resId = 1;
        SetupMockResidenceData(resId);
        var vm = new ResidencyHistoryViewModel(_residenceService);
        var account = CreateSampleUserAccount(resId);

        // Act
        vm.Initialize(account);
        await vm.LoadHistoryAsync();

        // Assert
        Assert.Equal(2, vm.TotalResidencyCount);
        Assert.Equal(2, vm.TimelineEvents.Count);
        Assert.True(vm.HasHistory);
    }
    #endregion
}
