using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApartmentResidenceManagement.Application.Security;
using ApartmentResidenceManagement.Application.Services;
using ApartmentResidenceManagement.Domain.Entities;
using ApartmentResidenceManagement.Domain.Enums;
using ApartmentResidenceManagement.Domain.Interfaces;
using ApartmentResidenceManagement.Wpf.ViewModels;
using Moq;
using Xunit;

namespace ApartmentResidenceManagement.Tests.ViewModels;

public class ResidentViewModelTests
{
    private readonly Mock<IUnitOfWork> _mockUow;
    private readonly Mock<IResidentRepository> _mockResidentRepo;
    private readonly Mock<IResidenceHistoryRepository> _mockHistoryRepo;
    private readonly Mock<IApartmentRepository> _mockApartmentRepo;
    private readonly Mock<IUserAccountRepository> _mockAccountRepo;
    private readonly Mock<IPasswordHasher> _mockHasher;

    private readonly ResidentService _residentService;
    private readonly AccountService _accountService;
    private readonly ResidenceService _residenceService;
    private readonly ApartmentService _apartmentService;

    public ResidentViewModelTests()
    {
        _mockUow = new Mock<IUnitOfWork>();
        _mockResidentRepo = new Mock<IResidentRepository>();
        _mockHistoryRepo = new Mock<IResidenceHistoryRepository>();
        _mockApartmentRepo = new Mock<IApartmentRepository>();
        _mockAccountRepo = new Mock<IUserAccountRepository>();
        _mockHasher = new Mock<IPasswordHasher>();

        _mockUow.Setup(u => u.Residents).Returns(_mockResidentRepo.Object);
        _mockUow.Setup(u => u.ResidenceHistories).Returns(_mockHistoryRepo.Object);
        _mockUow.Setup(u => u.Apartments).Returns(_mockApartmentRepo.Object);
        _mockUow.Setup(u => u.UserAccounts).Returns(_mockAccountRepo.Object);

        _residentService = new ResidentService(_mockUow.Object);
        _accountService = new AccountService(_mockUow.Object, _mockHasher.Object);
        _residenceService = new ResidenceService(_mockUow.Object);
        _apartmentService = new ApartmentService(_mockUow.Object);
    }

    private (List<Resident> residents, List<ResidenceHistory> histories, List<Apartment> apartments) CreateSampleData()
    {
        var apt1 = new Apartment { Id = 1, ApartmentNumber = "A-1203", Floor = 12, Area = 75 };
        var apt2 = new Apartment { Id = 2, ApartmentNumber = "A-1009", Floor = 10, Area = 85 };

        var r1 = new Resident
        {
            Id = 1,
            FullName = "Nguyễn Văn A",
            DateOfBirth = new DateTime(1990, 8, 15),
            Gender = GenderType.Male,
            IdentityCard = "001090012345",
            PhoneNumber = "0912345678",
            HomeTown = "Hà Nội"
        };

        var r2 = new Resident
        {
            Id = 2,
            FullName = "Trần Thị B",
            DateOfBirth = new DateTime(1992, 3, 22),
            Gender = GenderType.Female,
            IdentityCard = "001192034567",
            PhoneNumber = "0912345679",
            HomeTown = "Hải Phòng"
        };

        var r3 = new Resident
        {
            Id = 3,
            FullName = "Lê Văn C",
            DateOfBirth = new DateTime(1988, 11, 5),
            Gender = GenderType.Male,
            IdentityCard = "001088056789",
            PhoneNumber = "0912345680",
            HomeTown = "Nam Định"
        };

        var r4 = new Resident
        {
            Id = 4,
            FullName = "Phạm Thị D",
            DateOfBirth = new DateTime(1995, 7, 17),
            Gender = GenderType.Female,
            IdentityCard = "001195078901",
            PhoneNumber = "0912345681",
            HomeTown = "Hà Nam"
        };

        // r1: Đang cư trú tại A-1203 (Active)
        var h1 = new ResidenceHistory
        {
            Id = 1,
            ResidentId = 1,
            ApartmentId = 1,
            Apartment = apt1,
            Resident = r1,
            RelationshipType = RelationshipType.Owner,
            StartDate = new DateTime(2026, 1, 10),
            IsActive = true
        };

        // r2: Đang cư trú tại A-1009 (Active)
        var h2 = new ResidenceHistory
        {
            Id = 2,
            ResidentId = 2,
            ApartmentId = 2,
            Apartment = apt2,
            Resident = r2,
            RelationshipType = RelationshipType.FamilyMember,
            StartDate = new DateTime(2026, 1, 12),
            IsActive = true
        };

        // r3: Đã chuyển đi (Ended residence, IsActive = false)
        var h3 = new ResidenceHistory
        {
            Id = 3,
            ResidentId = 3,
            ApartmentId = 1,
            Apartment = apt1,
            Resident = r3,
            RelationshipType = RelationshipType.Tenant,
            StartDate = new DateTime(2025, 1, 1),
            EndDate = new DateTime(2025, 12, 31),
            IsActive = false
        };

        // r4: Chưa đăng ký cư trú (Không có lịch sử)

        return (
            new List<Resident> { r1, r2, r3, r4 },
            new List<ResidenceHistory> { h1, h2, h3 },
            new List<Apartment> { apt1, apt2 }
        );
    }

    [Fact]
    public async Task LoadDataAsync_ShouldCalculateKPIsAndMapResidenceStatusCorrectly()
    {
        // Arrange
        var (residents, histories, apartments) = CreateSampleData();
        _mockResidentRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(residents);
        _mockHistoryRepo.Setup(r => r.GetAllWithDetailsAsync()).ReturnsAsync(histories);
        _mockApartmentRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(apartments);

        var vm = new ResidentViewModel(_residentService, _accountService, _residenceService, _apartmentService);

        // Act
        await vm.LoadDataAsync();

        // Assert: 4 KPI Cards
        Assert.Equal(4, vm.TotalResidentsCount);
        Assert.Equal(2, vm.ActiveResidentsCount);        // r1, r2
        Assert.Equal(1, vm.MovedOutResidentsCount);      // r3
        Assert.Equal(1, vm.UnregisteredResidentsCount);  // r4

        // Assert: Danh sách hiển thị
        Assert.Equal(4, vm.PagedResidents.Count);

        var item1 = vm.PagedResidents.First(r => r.Id == 1);
        Assert.Equal("CD001", item1.ResidentCode);
        Assert.Equal("A-1203", item1.ApartmentNumber);
        Assert.Equal("Đang cư trú", item1.StatusText);

        var item3 = vm.PagedResidents.First(r => r.Id == 3);
        Assert.Equal("CD003", item3.ResidentCode);
        Assert.Equal("-", item3.ApartmentNumber);
        Assert.Equal("Đã chuyển đi", item3.StatusText);

        var item4 = vm.PagedResidents.First(r => r.Id == 4);
        Assert.Equal("CD004", item4.ResidentCode);
        Assert.Equal("-", item4.ApartmentNumber);
        Assert.Equal("Chưa đăng ký", item4.StatusText);
    }

    [Fact]
    public async Task SearchText_ShouldFilterByNameCodeAndIdentityCard()
    {
        // Arrange
        var (residents, histories, apartments) = CreateSampleData();
        _mockResidentRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(residents);
        _mockHistoryRepo.Setup(r => r.GetAllWithDetailsAsync()).ReturnsAsync(histories);
        _mockApartmentRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(apartments);

        var vm = new ResidentViewModel(_residentService, _accountService, _residenceService, _apartmentService);
        await vm.LoadDataAsync();

        // Act 1: Tìm theo tên
        vm.SearchText = "Trần Thị B";
        Assert.Single(vm.PagedResidents);
        Assert.Equal(2, vm.PagedResidents[0].Id);

        // Act 2: Tìm theo mã cư dân "CD001"
        vm.SearchText = "CD001";
        Assert.Single(vm.PagedResidents);
        Assert.Equal(1, vm.PagedResidents[0].Id);

        // Act 3: Tìm theo số CCCD "001088"
        vm.SearchText = "001088";
        Assert.Single(vm.PagedResidents);
        Assert.Equal(3, vm.PagedResidents[0].Id);
    }

    [Fact]
    public async Task SelectedStatusFilter_ShouldFilterByStatus()
    {
        // Arrange
        var (residents, histories, apartments) = CreateSampleData();
        _mockResidentRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(residents);
        _mockHistoryRepo.Setup(r => r.GetAllWithDetailsAsync()).ReturnsAsync(histories);
        _mockApartmentRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(apartments);

        var vm = new ResidentViewModel(_residentService, _accountService, _residenceService, _apartmentService);
        await vm.LoadDataAsync();

        // Act 1: Lọc Đang cư trú
        vm.SelectedStatusFilter = "Đang cư trú";
        Assert.Equal(2, vm.PagedResidents.Count);
        Assert.All(vm.PagedResidents, r => Assert.Equal("Đang cư trú", r.StatusText));

        // Act 2: Lọc Đã chuyển đi
        vm.SelectedStatusFilter = "Đã chuyển đi";
        Assert.Single(vm.PagedResidents);
        Assert.Equal(3, vm.PagedResidents[0].Id);

        // Act 3: Lọc Chưa đăng ký
        vm.SelectedStatusFilter = "Chưa đăng ký";
        Assert.Single(vm.PagedResidents);
        Assert.Equal(4, vm.PagedResidents[0].Id);
    }

    [Fact]
    public async Task Pagination_NextAndPreviousPage_ShouldWorkProperly()
    {
        // Arrange: Tạo 15 cư dân để phân trang với PageSize = 8
        var residents = new List<Resident>();
        for (int i = 1; i <= 15; i++)
        {
            residents.Add(new Resident
            {
                Id = i,
                FullName = $"Cư dân {i}",
                DateOfBirth = new DateTime(1990, 1, 1),
                Gender = GenderType.Male
            });
        }

        _mockResidentRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(residents);
        _mockHistoryRepo.Setup(r => r.GetAllWithDetailsAsync()).ReturnsAsync(new List<ResidenceHistory>());
        _mockApartmentRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Apartment>());

        var vm = new ResidentViewModel(_residentService, _accountService, _residenceService, _apartmentService);
        vm.PageSize = 8;
        await vm.LoadDataAsync();

        // Assert initial
        Assert.Equal(15, vm.TotalItems);
        Assert.Equal(2, vm.TotalPages);
        Assert.Equal(1, vm.CurrentPage);
        Assert.Equal(8, vm.PagedResidents.Count);

        // Act: Next Page
        vm.NextPageCommand.Execute(null);
        Assert.Equal(2, vm.CurrentPage);
        Assert.Equal(7, vm.PagedResidents.Count);

        // Act: Previous Page
        vm.PreviousPageCommand.Execute(null);
        Assert.Equal(1, vm.CurrentPage);
        Assert.Equal(8, vm.PagedResidents.Count);
    }

    [Fact]
    public async Task MouseWheelNavigation_ShouldSelectNextAndPreviousAcrossPages()
    {
        // Arrange
        var residents = new List<Resident>();
        for (int i = 1; i <= 10; i++)
        {
            residents.Add(new Resident { Id = i, FullName = $"Cư dân {i}", DateOfBirth = new DateTime(1990, 1, 1) });
        }

        _mockResidentRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(residents);
        _mockHistoryRepo.Setup(r => r.GetAllWithDetailsAsync()).ReturnsAsync(new List<ResidenceHistory>());
        _mockApartmentRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Apartment>());

        var vm = new ResidentViewModel(_residentService, _accountService, _residenceService, _apartmentService);
        vm.PageSize = 8;
        await vm.LoadDataAsync();

        // Bắt đầu tại dòng đầu tiên
        Assert.Equal(1, vm.SelectedResidentItem?.Id);

        // Lăn xuống 1 dòng -> Id = 2
        vm.SelectNextItemOrNextPage();
        Assert.Equal(2, vm.SelectedResidentItem?.Id);

        // Lăn lên 1 dòng -> Id = 1
        vm.SelectPreviousItemOrPreviousPage();
        Assert.Equal(1, vm.SelectedResidentItem?.Id);
    }

    [Fact]
    public void CloseDetailCommand_ShouldSetIsDetailPanelOpenToFalse()
    {
        var vm = new ResidentViewModel(_residentService, _accountService, _residenceService, _apartmentService);
        Assert.True(vm.IsDetailPanelOpen);

        vm.CloseDetailCommand.Execute(null);

        Assert.False(vm.IsDetailPanelOpen);
    }

    [Fact]
    public void ToggleDetailCommand_ShouldToggleIsDetailPanelOpen()
    {
        var vm = new ResidentViewModel(_residentService, _accountService, _residenceService, _apartmentService);
        Assert.True(vm.IsDetailPanelOpen);

        vm.ToggleDetailCommand.Execute(null);
        Assert.False(vm.IsDetailPanelOpen);

        vm.ToggleDetailCommand.Execute(null);
        Assert.True(vm.IsDetailPanelOpen);
    }

    [Fact]
    public async Task ViewResidentDetailCommand_ShouldSetSelectedItemAndOpenDetailPanel()
    {
        var (residents, histories, apartments) = CreateSampleData();
        _mockResidentRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(residents);
        _mockHistoryRepo.Setup(r => r.GetAllWithDetailsAsync()).ReturnsAsync(histories);
        _mockApartmentRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(apartments);

        var vm = new ResidentViewModel(_residentService, _accountService, _residenceService, _apartmentService);
        await vm.LoadDataAsync();

        // Close the panel first
        vm.IsDetailPanelOpen = false;

        var targetItem = vm.PagedResidents.First(r => r.Id == 2);
        vm.ViewResidentDetailCommand.Execute(targetItem);

        Assert.True(vm.IsDetailPanelOpen);
        Assert.Equal(2, vm.SelectedResidentItem?.Id);
    }
}


