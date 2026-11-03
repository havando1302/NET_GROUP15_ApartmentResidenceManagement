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

public class AccountManagementViewModelTests
{
    private readonly Mock<IUnitOfWork> _mockUow;
    private readonly Mock<IUserAccountRepository> _mockAccountRepo;
    private readonly Mock<IResidentRepository> _mockResidentRepo;
    private readonly Mock<IPasswordHasher> _mockHasher;
    private readonly AccountService _accountService;
    private readonly ResidentService _residentService;

    public AccountManagementViewModelTests()
    {
        _mockUow = new Mock<IUnitOfWork>();
        _mockAccountRepo = new Mock<IUserAccountRepository>();
        _mockResidentRepo = new Mock<IResidentRepository>();
        _mockHasher = new Mock<IPasswordHasher>();

        _mockUow.Setup(u => u.UserAccounts).Returns(_mockAccountRepo.Object);
        _mockUow.Setup(u => u.Residents).Returns(_mockResidentRepo.Object);

        _accountService = new AccountService(_mockUow.Object, _mockHasher.Object);
        _residentService = new ResidentService(_mockUow.Object);
    }

    private List<UserAccount> CreateSampleAccounts()
    {
        var resident1 = new Resident { Id = 1, FullName = "Nguyễn Văn A" };
        var resident2 = new Resident { Id = 2, FullName = "Trần Thị B" };

        return new List<UserAccount>
        {
            new UserAccount { Id = 1, Username = "admin", Role = UserRole.Admin, IsActive = true, CreatedAt = DateTime.Now },
            new UserAccount { Id = 2, Username = "nguyenvana", Role = UserRole.Resident, ResidentId = 1, Resident = resident1, IsActive = true, CreatedAt = DateTime.Now },
            new UserAccount { Id = 3, Username = "tranthib", Role = UserRole.Resident, ResidentId = 2, Resident = resident2, IsActive = false, CreatedAt = DateTime.Now }
        };
    }

    [Fact]
    public async Task LoadDataAsync_ShouldPopulateStatsAndPagedAccounts()
    {
        // Arrange
        var sampleAccounts = CreateSampleAccounts();
        _mockAccountRepo.Setup(r => r.GetAllWithResidentAsync()).ReturnsAsync(sampleAccounts);

        var vm = new AccountManagementViewModel(_accountService, _residentService);

        // Act
        await vm.LoadDataAsync();

        // Assert
        Assert.Equal(3, vm.TotalAccountsCount);
        Assert.Equal(2, vm.ActiveAccountsCount);
        Assert.Equal(1, vm.LockedAccountsCount);
        Assert.Equal(3, vm.PagedAccounts.Count);
        Assert.NotNull(vm.SelectedAccount);
        Assert.Equal("admin", vm.SelectedAccount.Username);
    }

    [Fact]
    public async Task SearchText_ShouldFilterAccounts()
    {
        // Arrange
        var sampleAccounts = CreateSampleAccounts();
        _mockAccountRepo.Setup(r => r.GetAllWithResidentAsync()).ReturnsAsync(sampleAccounts);

        var vm = new AccountManagementViewModel(_accountService, _residentService);
        await vm.LoadDataAsync();

        // Act
        vm.SearchText = "nguyenvana";

        // Assert
        Assert.Single(vm.PagedAccounts);
        Assert.Equal("nguyenvana", vm.PagedAccounts[0].Username);
    }

    [Fact]
    public async Task RoleFilter_ShouldFilterByAdminOrResident()
    {
        // Arrange
        var sampleAccounts = CreateSampleAccounts();
        _mockAccountRepo.Setup(r => r.GetAllWithResidentAsync()).ReturnsAsync(sampleAccounts);

        var vm = new AccountManagementViewModel(_accountService, _residentService);
        await vm.LoadDataAsync();

        // Act
        vm.SelectedRoleFilter = "Admin";

        // Assert
        Assert.Single(vm.PagedAccounts);
        Assert.Equal("admin", vm.PagedAccounts[0].Username);

        // Act 2
        vm.SelectedRoleFilter = "Cư dân";

        // Assert 2
        Assert.Equal(2, vm.PagedAccounts.Count);
    }

    [Fact]
    public async Task StatusFilter_ShouldFilterByActiveOrLocked()
    {
        // Arrange
        var sampleAccounts = CreateSampleAccounts();
        _mockAccountRepo.Setup(r => r.GetAllWithResidentAsync()).ReturnsAsync(sampleAccounts);

        var vm = new AccountManagementViewModel(_accountService, _residentService);
        await vm.LoadDataAsync();

        // Act
        vm.SelectedStatusFilter = "Đã khóa";

        // Assert
        Assert.Single(vm.PagedAccounts);
        Assert.Equal("tranthib", vm.PagedAccounts[0].Username);
    }

    [Fact]
    public async Task MouseWheelNavigation_ShouldSelectNextAndPreviousItems()
    {
        // Arrange
        var sampleAccounts = CreateSampleAccounts();
        _mockAccountRepo.Setup(r => r.GetAllWithResidentAsync()).ReturnsAsync(sampleAccounts);

        var vm = new AccountManagementViewModel(_accountService, _residentService);
        await vm.LoadDataAsync();

        // Current is admin (index 0)
        Assert.Equal("admin", vm.SelectedAccount?.Username);

        // Lăn chuột xuống -> chuyển sang nguyenvana (index 1)
        vm.SelectNextItemOrNextPage();
        Assert.Equal("nguyenvana", vm.SelectedAccount?.Username);

        // Lăn chuột lên -> chuyển về admin (index 0)
        vm.SelectPreviousItemOrPreviousPage();
        Assert.Equal("admin", vm.SelectedAccount?.Username);
    }

    [Fact]
    public async Task ResetPasswordDialog_OpenAndCancel_ShouldWorkCorrectly()
    {
        // Arrange
        var sampleAccounts = CreateSampleAccounts();
        _mockAccountRepo.Setup(r => r.GetAllWithResidentAsync()).ReturnsAsync(sampleAccounts);

        var vm = new AccountManagementViewModel(_accountService, _residentService);
        await vm.LoadDataAsync();

        // Act
        vm.OpenResetPasswordDialogCommand.Execute(vm.PagedAccounts[1]);

        // Assert
        Assert.True(vm.IsResetPasswordDialogOpen);
        Assert.Equal("nguyenvana", vm.AccountToReset?.Username);

        // Close
        vm.CloseResetPasswordDialogCommand.Execute(null);
        Assert.False(vm.IsResetPasswordDialogOpen);
        Assert.Null(vm.AccountToReset);
    }
}
