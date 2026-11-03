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
    public const int MotorcycleLimitPerApartment = 3;
    public const int CarLimitPerApartment = 1;
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
        await ExecuteWithQuotaLockAsync(vehicle.OwnerId, async () =>
        {
            // Re-read within the lock rather than updating an old pre-lock snapshot.
            var existing = await _unitOfWork.Vehicles.GetByIdAsync(vehicle.Id)
                ?? throw new BusinessRuleException("Không tìm thấy phương tiện cần cập nhật.");
            await ValidateVehicleDataAsync(vehicle, existing.Id);
            if (existing.RegistrationStatus == VehicleRegistrationStatus.Approved)
                await EnsureQuotaAvailableAsync(vehicle.OwnerId, vehicle.VehicleType, existing.Id);
            existing.LicensePlate = vehicle.LicensePlate;
            existing.VehicleType = vehicle.VehicleType;
            existing.Brand = vehicle.Brand;
            existing.Color = vehicle.Color;
            existing.OwnerId = vehicle.OwnerId;
            _unitOfWork.Vehicles.Update(existing);
            await _unitOfWork.CompleteAsync();
            return true;
        });
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
        existing.RegistrationStatus = VehicleRegistrationStatus.Pending;

        _unitOfWork.Vehicles.Update(existing);
        await _unitOfWork.CompleteAsync();
    }

    public async Task ReviewVehicleAsync(int vehicleId, VehicleRegistrationStatus status)
    {
        if (status is not VehicleRegistrationStatus.Approved and not VehicleRegistrationStatus.Rejected)
            throw new BusinessRuleException("Trạng thái duyệt phương tiện không hợp lệ.");

        var vehicle = await _unitOfWork.Vehicles.GetByIdAsync(vehicleId)
            ?? throw new BusinessRuleException("Không tìm thấy phương tiện cần duyệt.");
        if (vehicle.RegistrationStatus != VehicleRegistrationStatus.Pending)
            throw new BusinessRuleException("Chỉ có thể duyệt hoặc từ chối phương tiện đang chờ duyệt.");

        if (status == VehicleRegistrationStatus.Approved)
        {
            var owner = await _unitOfWork.Residents.GetByIdAsync(vehicle.OwnerId);
            if (owner == null)
                throw new BusinessRuleException("Không thể duyệt xe vì không tìm thấy cư dân sở hữu.");
            await ExecuteWithQuotaLockAsync(vehicle.OwnerId, async () =>
            {
                // Re-read after taking the apartment lock to handle concurrent reviews.
                var current = await _unitOfWork.Vehicles.GetByIdAsync(vehicleId)
                    ?? throw new BusinessRuleException("Không tìm thấy phương tiện cần duyệt.");
                if (current.RegistrationStatus != VehicleRegistrationStatus.Pending)
                    throw new BusinessRuleException("Phương tiện đã được xử lý.");
                await EnsureQuotaAvailableAsync(current.OwnerId, current.VehicleType, current.Id);
                current.RegistrationStatus = status;
                _unitOfWork.Vehicles.Update(current);
                await _unitOfWork.CompleteAsync();
                return true;
            });
            return;
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
        async Task<Vehicle> CreateAsync()
        {
            await ValidateVehicleDataAsync(vehicle);
            if (status == VehicleRegistrationStatus.Approved)
                await EnsureQuotaAvailableAsync(vehicle.OwnerId, vehicle.VehicleType);
            vehicle.RegistrationStatus = status;
            vehicle.RegisteredAtUtc = DateTime.UtcNow;
            await _unitOfWork.Vehicles.AddAsync(vehicle);
            await _unitOfWork.CompleteAsync();
            return vehicle;
        }

        return status == VehicleRegistrationStatus.Approved
            ? await ExecuteWithQuotaLockAsync(vehicle.OwnerId, CreateAsync)
            : await CreateAsync();
    }

    private async Task<T> ExecuteWithQuotaLockAsync<T>(int ownerId, Func<Task<T>> action)
    {
        var initial = await _unitOfWork.ResidenceHistories.GetActiveByResidentIdAsync(ownerId)
            ?? throw new BusinessRuleException("Cư dân phải có cư trú đang hoạt động để đăng ký hoặc duyệt phương tiện.");
        return await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var apartment = await _unitOfWork.Apartments.LockForVehicleQuotaAsync(initial.ApartmentId);
            if (apartment == null)
                throw new BusinessRuleException("Không tìm thấy căn hộ của cư dân.");
            var current = await _unitOfWork.ResidenceHistories.GetActiveByResidentIdAsync(ownerId);
            if (current == null || current.ApartmentId != initial.ApartmentId)
                throw new BusinessRuleException("Thông tin cư trú đã thay đổi. Vui lòng thử lại.");
            return await action();
        });
    }

    private async Task EnsureQuotaAvailableAsync(int ownerId, VehicleType type, int? excludeVehicleId = null)
    {
        int? limit = type switch
        {
            VehicleType.Moto => MotorcycleLimitPerApartment,
            VehicleType.Car => CarLimitPerApartment,
            _ => null
        };
        if (limit == null) return;
        var residence = await _unitOfWork.ResidenceHistories.GetActiveByResidentIdAsync(ownerId)
            ?? throw new BusinessRuleException("Cư dân chưa có cư trú đang hoạt động.");
        var residents = await _unitOfWork.ResidenceHistories.GetActiveByApartmentIdAsync(residence.ApartmentId);
        var ownerIds = residents.Select(r => r.ResidentId).ToHashSet();
        var vehicles = await _unitOfWork.Vehicles.GetAllWithDetailsAsync();
        var count = vehicles.Count(v => ownerIds.Contains(v.OwnerId) && v.VehicleType == type
            && v.RegistrationStatus == VehicleRegistrationStatus.Approved && v.Id != excludeVehicleId);
        if (count >= limit)
            throw new BusinessRuleException($"Căn hộ đã đạt hạn mức {limit} {(type == VehicleType.Moto ? "xe máy" : "ô tô")} đã duyệt.");
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

        if (vehicle.Color?.Length > 50)
            throw new BusinessRuleException("Màu xe không được vượt quá 50 ký tự.");

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
