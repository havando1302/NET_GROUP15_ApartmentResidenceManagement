using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApartmentResidenceManagement.Domain.Entities;
using ApartmentResidenceManagement.Domain.Enums;
using ApartmentResidenceManagement.Domain.Exceptions;
using ApartmentResidenceManagement.Domain.Interfaces;
using ApartmentResidenceManagement.Application.Validators;

namespace ApartmentResidenceManagement.Application.Services;

public class VehicleService
{
    private readonly IUnitOfWork _unitOfWork;

    public VehicleService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<Vehicle>> GetAllVehiclesAsync()
    {
        return await _unitOfWork.Vehicles.GetAllWithDetailsAsync();
    }

    public async Task<Vehicle?> GetVehicleByIdAsync(int id)
    {
        return await _unitOfWork.Vehicles.GetByIdAsync(id);
    }

    public async Task<IEnumerable<Vehicle>> GetVehiclesByOwnerIdAsync(int ownerId)
    {
        return await _unitOfWork.Vehicles.GetVehiclesByOwnerIdAsync(ownerId);
    }

    public async Task<Vehicle> CreateVehicleAsync(Vehicle vehicle)
    {
        return await CreateVehicleCoreAsync(vehicle, VehicleRegistrationStatus.Approved);
    }

    public async Task<Vehicle> SubmitVehicleRegistrationAsync(Vehicle vehicle)
    {
        return await CreateVehicleCoreAsync(vehicle, VehicleRegistrationStatus.Pending);
    }

    public async Task UpdateVehicleAsync(Vehicle vehicle)
    {
        var existing = await _unitOfWork.Vehicles.GetByIdAsync(vehicle.Id);
        if (existing == null)
        {
            throw new BusinessRuleException("Không tìm thấy phương tiện cần cập nhật.");
        }

        await ValidateVehicleDataAsync(vehicle, existing.Id);
        existing.LicensePlate = vehicle.LicensePlate;
        existing.VehicleType = vehicle.VehicleType;
        existing.Brand = vehicle.Brand;
        existing.Color = vehicle.Color;
        existing.ImagePath = vehicle.ImagePath;
        existing.OwnerId = vehicle.OwnerId;

        _unitOfWork.Vehicles.Update(existing);
        await _unitOfWork.CompleteAsync();
    }

    public async Task UpdateVehicleRegistrationAsync(int ownerId, Vehicle vehicle)
    {
        var existing = await _unitOfWork.Vehicles.GetByIdAsync(vehicle.Id);
        if (existing == null)
        {
            throw new BusinessRuleException("Không tìm thấy phương tiện cần cập nhật.");
        }

        if (existing.OwnerId != ownerId || vehicle.OwnerId != ownerId)
        {
            throw new BusinessRuleException("Bạn chỉ được cập nhật phương tiện thuộc sở hữu của mình.");
        }

        await ValidateVehicleDataAsync(vehicle, existing.Id);
        existing.LicensePlate = vehicle.LicensePlate;
        existing.VehicleType = vehicle.VehicleType;
        existing.Brand = vehicle.Brand;
        existing.Color = vehicle.Color;
        existing.ImagePath = vehicle.ImagePath;
        existing.RegistrationStatus = VehicleRegistrationStatus.Pending;

        _unitOfWork.Vehicles.Update(existing);
        await _unitOfWork.CompleteAsync();
    }

    public async Task ReviewVehicleAsync(int vehicleId, VehicleRegistrationStatus status, string? rejectReason = null)
    {
        if (status is not VehicleRegistrationStatus.Approved and not VehicleRegistrationStatus.Rejected)
        {
            throw new BusinessRuleException("Trạng thái duyệt phương tiện không hợp lệ.");
        }

        var vehicle = await _unitOfWork.Vehicles.GetByIdAsync(vehicleId);
        if (vehicle == null)
        {
            throw new BusinessRuleException("Không tìm thấy phương tiện cần duyệt.");
        }

        if (status == VehicleRegistrationStatus.Approved)
        {
            var owner = await _unitOfWork.Residents.GetByIdAsync(vehicle.OwnerId);
            if (owner == null)
            {
                throw new BusinessRuleException("Không thể duyệt xe vì không tìm thấy cư dân sở hữu.");
            }
        }

        if (status == VehicleRegistrationStatus.Approved)
        {
            await EnsureActiveResidenceAsync(vehicle.OwnerId);
            vehicle.RejectReason = null;
        }
        else if (status == VehicleRegistrationStatus.Rejected)
        {
            vehicle.RejectReason = rejectReason;
        }

        vehicle.RegistrationStatus = status;
        _unitOfWork.Vehicles.Update(vehicle);
        await _unitOfWork.CompleteAsync();
    }

    public async Task DeleteVehicleAsync(int id)
    {
        var vehicle = await _unitOfWork.Vehicles.GetByIdAsync(id);
        if (vehicle == null)
        {
            throw new BusinessRuleException("Không tìm thấy phương tiện cần xóa.");
        }

        _unitOfWork.Vehicles.Delete(vehicle);
        await _unitOfWork.CompleteAsync();
    }

    public async Task DeleteVehicleByOwnerAsync(int ownerId, int vehicleId)
    {
        var vehicle = await _unitOfWork.Vehicles.GetByIdAsync(vehicleId);
        if (vehicle == null)
        {
            throw new BusinessRuleException("Không tìm thấy phương tiện cần xóa.");
        }

        if (vehicle.OwnerId != ownerId)
        {
            throw new BusinessRuleException("Bạn chỉ được xóa phương tiện thuộc sở hữu của mình.");
        }

        _unitOfWork.Vehicles.Delete(vehicle);
        await _unitOfWork.CompleteAsync();
    }

    private async Task<Vehicle> CreateVehicleCoreAsync(Vehicle vehicle, VehicleRegistrationStatus status)
    {
        await ValidateVehicleDataAsync(vehicle);
        vehicle.RegistrationStatus = status;
        if (vehicle.RegistrationDate == default)
        {
            vehicle.RegistrationDate = DateTime.Now;
        }

        await _unitOfWork.Vehicles.AddAsync(vehicle);
        await _unitOfWork.CompleteAsync();

        return vehicle;
    }

    private async Task ValidateVehicleDataAsync(Vehicle vehicle, int? currentVehicleId = null)
    {
        if (string.IsNullOrWhiteSpace(vehicle.LicensePlate))
        {
            throw new BusinessRuleException("Biển số xe không được để trống.");
        }

        vehicle.LicensePlate = vehicle.LicensePlate.Replace(" ", "").ToUpperInvariant();
        vehicle.Brand = string.IsNullOrWhiteSpace(vehicle.Brand) ? null : vehicle.Brand.Trim();
        vehicle.Color = string.IsNullOrWhiteSpace(vehicle.Color) ? null : vehicle.Color.Trim();
        vehicle.ImagePath = string.IsNullOrWhiteSpace(vehicle.ImagePath) ? null : vehicle.ImagePath.Trim();

        if (!Enum.IsDefined(vehicle.VehicleType))
        {
            throw new BusinessRuleException("Loại phương tiện không hợp lệ.");
        }

        if (!InputValidator.ValidateLicensePlate(vehicle.LicensePlate))
        {
            throw new BusinessRuleException($"Biển số xe '{vehicle.LicensePlate}' không đúng định dạng (ví dụ: 29A1-12345 hoặc 29A-1234).");
        }

        if (vehicle.Brand?.Length > 50)
        {
            throw new BusinessRuleException("Nhãn hiệu phương tiện không được vượt quá 50 ký tự.");
        }

        if (vehicle.Color?.Length > 30)
        {
            throw new BusinessRuleException("Màu xe không được vượt quá 30 ký tự.");
        }

        var duplicate = await _unitOfWork.Vehicles.GetByLicensePlateAsync(vehicle.LicensePlate);
        if (duplicate != null && duplicate.Id != currentVehicleId)
        {
            throw new BusinessRuleException($"Biển số xe '{vehicle.LicensePlate}' đã tồn tại trong hệ thống.");
        }

        var owner = await _unitOfWork.Residents.GetByIdAsync(vehicle.OwnerId);
        if (owner == null)
        {
            throw new BusinessRuleException("Không tìm thấy cư dân sở hữu phương tiện.");
        }

        await EnsureActiveResidenceAsync(vehicle.OwnerId);
    }

    private async Task EnsureActiveResidenceAsync(int ownerId)
    {
        var residence = await _unitOfWork.ResidenceHistories.GetActiveByResidentIdAsync(ownerId);
        if (residence == null)
        {
            throw new BusinessRuleException("Cư dân phải có cư trú đang hoạt động để đăng ký hoặc duyệt phương tiện.");
        }
    }
}
