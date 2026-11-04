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
}
