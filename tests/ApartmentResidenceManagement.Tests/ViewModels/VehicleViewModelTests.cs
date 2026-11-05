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

public class VehicleViewModelTests
{
    private readonly Mock<IUnitOfWork> _mockUow;
    private readonly Mock<IVehicleRepository> _mockVehicleRepo;
    private readonly Mock<IResidentRepository> _mockResidentRepo;
    private readonly Mock<IResidenceHistoryRepository> _mockResidenceRepo;

    private readonly VehicleService _vehicleService;
    private readonly ResidentService _residentService;
    private readonly ResidenceService _residenceService;

    public VehicleViewModelTests()
    {
        _mockUow = new Mock<IUnitOfWork>();
        _mockVehicleRepo = new Mock<IVehicleRepository>();
        _mockResidentRepo = new Mock<IResidentRepository>();
        _mockResidenceRepo = new Mock<IResidenceHistoryRepository>();

        _mockUow.Setup(u => u.Vehicles).Returns(_mockVehicleRepo.Object);
        _mockUow.Setup(u => u.Residents).Returns(_mockResidentRepo.Object);
        _mockUow.Setup(u => u.ResidenceHistories).Returns(_mockResidenceRepo.Object);

        _vehicleService = new VehicleService(_mockUow.Object);
        _residentService = new ResidentService(_mockUow.Object);
        _residenceService = new ResidenceService(_mockUow.Object);
    }

    [Fact]
    public void VehicleItem_StatusHelpers_ShouldReflectRegistrationStatusCorrectly()
    {
        // 1. Pending vehicle
        var pendingVehicle = new Vehicle
        {
            Id = 1,
            LicensePlate = "29A1-11111",
            RegistrationStatus = VehicleRegistrationStatus.Pending
        };
        var itemPending = new VehicleItem(pendingVehicle, "101");
        Assert.True(itemPending.IsPending);
        Assert.False(itemPending.IsApproved);
        Assert.False(itemPending.IsRejected);
        Assert.False(itemPending.IsReviewed);

        // 2. Rejected vehicle with reason
        var rejectedVehicle = new Vehicle
        {
            Id = 2,
            LicensePlate = "29A1-22222",
            RegistrationStatus = VehicleRegistrationStatus.Rejected,
            RejectReason = "Hầm xe đã hết chỗ để ô tô"
        };
        var itemRejected = new VehicleItem(rejectedVehicle, "102");
        Assert.False(itemRejected.IsPending);
        Assert.False(itemRejected.IsApproved);
        Assert.True(itemRejected.IsRejected);
        Assert.True(itemRejected.IsReviewed);
        Assert.Equal("Hầm xe đã hết chỗ để ô tô", itemRejected.RejectReason);
        Assert.Equal("Hầm xe đã hết chỗ để ô tô", itemRejected.RejectReasonDisplay);

        // 3. Approved vehicle
        var approvedVehicle = new Vehicle
        {
            Id = 3,
            LicensePlate = "29A1-33333",
            RegistrationStatus = VehicleRegistrationStatus.Approved
        };
        var itemApproved = new VehicleItem(approvedVehicle, "103");
        Assert.False(itemApproved.IsPending);
        Assert.True(itemApproved.IsApproved);
        Assert.False(itemApproved.IsRejected);
        Assert.True(itemApproved.IsReviewed);
    }

    [Fact]
    public void OpenRejectDialog_ShouldSetVehicleToRejectAndOpenDialog()
    {
        var vm = new VehicleViewModel(_vehicleService, _residentService, _residenceService);
        var vehicle = new Vehicle { Id = 1, LicensePlate = "29A1-12345", RegistrationStatus = VehicleRegistrationStatus.Pending };
        var item = new VehicleItem(vehicle, "201");

        vm.OpenRejectDialogCommand.Execute(item);

        Assert.True(vm.IsRejectDialogOpen);
        Assert.Same(item, vm.VehicleToReject);
        Assert.Equal(string.Empty, vm.RejectReasonInput);
    }

    [Fact]
    public async Task ConfirmReject_WithEmptyReason_ShouldSetErrorMessageAndStayOpen()
    {
        var vm = new VehicleViewModel(_vehicleService, _residentService, _residenceService);
        var vehicle = new Vehicle { Id = 1, LicensePlate = "29A1-12345", RegistrationStatus = VehicleRegistrationStatus.Pending };
        var item = new VehicleItem(vehicle, "201");

        vm.OpenRejectDialogCommand.Execute(item);
        vm.RejectReasonInput = "   ";

        if (vm.ConfirmRejectCommand.CanExecute(null))
        {
            await ((ApartmentResidenceManagement.Wpf.Commands.AsyncRelayCommand)vm.ConfirmRejectCommand).ExecuteAsync(null);
        }

        Assert.True(vm.IsRejectDialogOpen);
        Assert.False(string.IsNullOrEmpty(vm.ErrorMessage));
        _mockUow.Verify(u => u.CompleteAsync(), Times.Never);
    }

    [Fact]
    public async Task ConfirmReject_WithValidReason_ShouldCallServiceAndUpdateVehicle()
    {
        var vehicle = new Vehicle { Id = 1, OwnerId = 10, LicensePlate = "29A1-12345", RegistrationStatus = VehicleRegistrationStatus.Pending };
        _mockVehicleRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(vehicle);
        _mockVehicleRepo.Setup(r => r.GetAllWithDetailsAsync()).ReturnsAsync(new List<Vehicle> { vehicle });

        var vm = new VehicleViewModel(_vehicleService, _residentService, _residenceService);
        var item = new VehicleItem(vehicle, "201");

        vm.OpenRejectDialogCommand.Execute(item);
        vm.RejectReasonInput = "Không cung cấp đủ giấy tờ xe";

        if (vm.ConfirmRejectCommand.CanExecute(null))
        {
            await ((ApartmentResidenceManagement.Wpf.Commands.AsyncRelayCommand)vm.ConfirmRejectCommand).ExecuteAsync(null);
        }

        Assert.False(vm.IsRejectDialogOpen);
        Assert.Equal(VehicleRegistrationStatus.Rejected, vehicle.RegistrationStatus);
        Assert.Equal("Không cung cấp đủ giấy tờ xe", vehicle.RejectReason);
        _mockUow.Verify(u => u.CompleteAsync(), Times.Once);
    }

    [Fact]
    public void VehicleItem_HasImage_ReturnsExpectedResult()
    {
        var v1 = new Vehicle { Id = 1, ImagePath = "C:\\path\\car.jpg" };
        var item1 = new VehicleItem(v1, "101");
        Assert.True(item1.HasImage);

        var v2 = new Vehicle { Id = 2, ImagePath = null };
        var item2 = new VehicleItem(v2, "102");
        Assert.False(item2.HasImage);
    }

    [Fact]
    public void OpenImageZoomCommand_SetsPropertiesAndOpensZoomModal()
    {
        var vm = new VehicleViewModel(_vehicleService, _residentService, _residenceService);
        var v = new Vehicle { Id = 1, LicensePlate = "30A-99999", ImagePath = "C:\\photos\\car.jpg" };
        var item = new VehicleItem(v, "301");

        vm.OpenImageZoomCommand.Execute(item);

        Assert.True(vm.IsImageZoomOpen);
        Assert.Equal("C:\\photos\\car.jpg", vm.ZoomedImagePath);
        Assert.Contains("30A-99999", vm.ZoomedImageTitle);

        vm.CloseImageZoomCommand.Execute(null);
        Assert.False(vm.IsImageZoomOpen);
        Assert.Null(vm.ZoomedImagePath);
    }

    [Fact]
    public async Task SaveVehicleAsync_WithoutImage_SetsErrorMessage()
    {
        var vm = new VehicleViewModel(_vehicleService, _residentService, _residenceService);
        vm.EditingVehicle = new Vehicle
        {
            LicensePlate = "30A-11122",
            OwnerId = 10,
            ImagePath = null
        };

        if (vm.SaveVehicleCommand.CanExecute(null))
        {
            await ((ApartmentResidenceManagement.Wpf.Commands.AsyncRelayCommand)vm.SaveVehicleCommand).ExecuteAsync(null);
        }

        Assert.Contains("bắt buộc", vm.ErrorMessage);
    }

    [Fact]
    public void VehicleItem_RegistrationStatusCancelled_ReturnsCorrectTextAndColors()
    {
        var v = new Vehicle { Id = 10, LicensePlate = "29A-12345", RegistrationStatus = VehicleRegistrationStatus.Cancelled };
        var item = new VehicleItem(v, "101");

        Assert.Equal("Đã hủy", item.RegistrationStatusText);
        Assert.Equal("#64748B", item.StatusDotColor);
        Assert.Equal("#F1F5F9", item.StatusBgColor);
        Assert.True(item.IsCancelled);
    }
[Fact]
    public void CancelledVehiclesCount_PropertyChange_ShouldUpdateValue()
    {
        var vm = new VehicleViewModel(_vehicleService, _residentService, _residenceService);
        vm.CancelledVehiclesCount = 7;
        Assert.Equal(7, vm.CancelledVehiclesCount);
    }
}

