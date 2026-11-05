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
    public const int MaxCarsPerApartment = 1;
    public const int MaxMotosPerApartment = 2;
    public const int MaxBicyclesPerApartment = 2;

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
        if (string.IsNullOrWhiteSpace(vehicle.ImagePath))
        {
            throw new BusinessRuleException("Hình ảnh phương tiện là bắt buộc khi tạo đơn đăng ký.");
        }
        var result = await CreateVehicleCoreAsync(vehicle, VehicleRegistrationStatus.Pending);

        if (_unitOfWork.Notifications != null && _unitOfWork.UserAccounts != null)
        {
            try
            {
                var adminAccounts = (await _unitOfWork.UserAccounts.GetAllAsync())
                    .Where(u => u.Role == UserRole.Admin && u.IsActive)
                    .ToList();

                var owner = await _unitOfWork.Residents.GetByIdAsync(vehicle.OwnerId);
                string ownerName = owner?.FullName ?? "Cư dân";

                if (adminAccounts.Count > 0)
                {
                    var notif = new Notification
                    {
                        Title = $"Đơn đăng ký phương tiện mới: {vehicle.LicensePlate}",
                        Content = $"Cư dân <b>{ownerName}</b> đã gửi yêu cầu đăng ký phương tiện mới với biển số <color=#2563EB><b>{vehicle.LicensePlate}</b></color> ({vehicle.Brand} - {vehicle.Color}). Vui lòng kiểm tra và phê duyệt.",
                        Type = NotificationType.ApprovalRequest,
                        Priority = NotificationPriority.Important,
                        TargetAudience = TargetAudienceType.SystemAdmin,
                        RelatedEntityType = "Vehicle",
                        RelatedEntityId = vehicle.Id,
                        CreatedAt = DateTime.Now
                    };
                    foreach (var admin in adminAccounts)
                    {
                        notif.Recipients.Add(new NotificationRecipient
                        {
                            UserAccountId = admin.Id,
                            IsRead = false,
                            CreatedAt = notif.CreatedAt
                        });
                    }
                    await _unitOfWork.Notifications.AddAsync(notif);
                    await _unitOfWork.CompleteAsync();
                }
            }
            catch { }
        }

        return result;
    }

    public async Task UpdateVehicleAsync(Vehicle vehicle)
    {
        var existing = await _unitOfWork.Vehicles.GetByIdAsync(vehicle.Id);
        if (existing == null)
        {
            throw new BusinessRuleException("Không tìm thấy phương tiện cần cập nhật.");
        }

        await ValidateVehicleDataAsync(vehicle, existing.Id);
        await ValidateVehicleQuotaAsync(vehicle, excludeVehicleId: existing.Id, isApprovalCheck: existing.RegistrationStatus == VehicleRegistrationStatus.Approved);
        existing.LicensePlate = vehicle.LicensePlate;
        existing.VehicleType = vehicle.VehicleType;
        existing.Brand = vehicle.Brand;
        existing.Color = vehicle.Color;
        existing.ImagePath = vehicle.ImagePath;
        existing.OwnerId = vehicle.OwnerId;

        _unitOfWork.Vehicles.Update(existing);
        await _unitOfWork.CompleteAsync();

        // Gửi thông báo tới cư dân sở hữu khi Ban quản trị cập nhật thông tin xe
        if (_unitOfWork.Notifications != null && _unitOfWork.UserAccounts != null)
        {
            try
            {
                var ownerAcc = await _unitOfWork.UserAccounts.GetByResidentIdAsync(vehicle.OwnerId);
                if (ownerAcc != null && ownerAcc.IsActive)
                {
                    var notif = new Notification
                    {
                        Title = $"Cập nhật hồ sơ phương tiện: {vehicle.LicensePlate}",
                        Content = $"Ban quản lý tòa nhà đã cập nhật lại thông tin hồ sơ phương tiện biển số <color=#2563EB><b>{vehicle.LicensePlate}</b></color> của bạn. Vui lòng kiểm tra lại thông tin chi tiết trên ứng dụng.",
                        Type = NotificationType.General,
                        Priority = NotificationPriority.Normal,
                        TargetAudience = TargetAudienceType.SpecificResidents,
                        RelatedEntityType = "Vehicle",
                        RelatedEntityId = vehicle.Id,
                        CreatedAt = DateTime.Now
                    };
                    notif.Recipients.Add(new NotificationRecipient
                    {
                        UserAccountId = ownerAcc.Id,
                        IsRead = false,
                        CreatedAt = notif.CreatedAt
                    });
                    await _unitOfWork.Notifications.AddAsync(notif);
                    await _unitOfWork.CompleteAsync();
                }
            }
            catch { }
        }
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

        if (string.IsNullOrWhiteSpace(vehicle.ImagePath))
        {
            throw new BusinessRuleException("Hình ảnh phương tiện là bắt buộc khi cập nhật đơn đăng ký.");
        }

        await ValidateVehicleDataAsync(vehicle, existing.Id);
        await ValidateVehicleQuotaAsync(vehicle, excludeVehicleId: existing.Id, isApprovalCheck: false);
        existing.LicensePlate = vehicle.LicensePlate;
        existing.VehicleType = vehicle.VehicleType;
        existing.Brand = vehicle.Brand;
        existing.Color = vehicle.Color;
        existing.ImagePath = vehicle.ImagePath;
        existing.RegistrationStatus = VehicleRegistrationStatus.Pending;
        existing.RejectReason = null;

        _unitOfWork.Vehicles.Update(existing);
        await _unitOfWork.CompleteAsync();

        // Gửi thông báo tới Ban quản lý khi cư dân cập nhật lại thông tin/hình ảnh đơn đăng ký
        if (_unitOfWork.Notifications != null && _unitOfWork.UserAccounts != null)
        {
            try
            {
                var adminAccounts = (await _unitOfWork.UserAccounts.GetAllAsync())
                    .Where(u => u.Role == UserRole.Admin && u.IsActive)
                    .ToList();
                if (adminAccounts.Count > 0)
                {
                    var owner = await _unitOfWork.Residents.GetByIdAsync(ownerId);
                    string ownerName = owner?.FullName ?? "Cư dân";
                    var notif = new Notification
                    {
                        Title = $"Cập nhật đơn đăng ký xe: {vehicle.LicensePlate}",
                        Content = $"Cư dân <b>{ownerName}</b> đã cập nhật lại thông tin/hình ảnh cho đơn đăng ký phương tiện biển số <color=#2563EB><b>{vehicle.LicensePlate}</b></color>. Vui lòng kiểm tra và phê duyệt.",
                        Type = NotificationType.ApprovalRequest,
                        Priority = NotificationPriority.Important,
                        TargetAudience = TargetAudienceType.SystemAdmin,
                        RelatedEntityType = "Vehicle",
                        RelatedEntityId = existing.Id,
                        CreatedAt = DateTime.Now
                    };
                    foreach (var admin in adminAccounts)
                    {
                        notif.Recipients.Add(new NotificationRecipient
                        {
                            UserAccountId = admin.Id,
                            IsRead = false,
                            CreatedAt = notif.CreatedAt
                        });
                    }
                    await _unitOfWork.Notifications.AddAsync(notif);
                    await _unitOfWork.CompleteAsync();
                }
            }
            catch { }
        }
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
            await ValidateVehicleQuotaAsync(vehicle, excludeVehicleId: vehicle.Id, isApprovalCheck: true);
            vehicle.RejectReason = null;
        }
        else if (status == VehicleRegistrationStatus.Rejected)
        {
            vehicle.RejectReason = string.IsNullOrWhiteSpace(rejectReason) ? "Không đủ điều kiện phê duyệt" : rejectReason.Trim();
        }

        vehicle.RegistrationStatus = status;
        _unitOfWork.Vehicles.Update(vehicle);
        await _unitOfWork.CompleteAsync();

        // Gửi thông báo kết quả phê duyệt cho chủ sở hữu xe
        if (_unitOfWork.Notifications != null && _unitOfWork.UserAccounts != null)
        {
            try
            {
                var ownerAcc = await _unitOfWork.UserAccounts.GetByResidentIdAsync(vehicle.OwnerId);
                if (ownerAcc != null && ownerAcc.IsActive)
                {
                    bool isApproved = status == VehicleRegistrationStatus.Approved;
                    var notif = new Notification
                    {
                        Title = isApproved ? $"Phương tiện {vehicle.LicensePlate} đã được phê duyệt" : $"Đăng ký phương tiện {vehicle.LicensePlate} bị từ chối",
                        Content = isApproved
                            ? $"Phương tiện biển số <color=#2563EB><b>{vehicle.LicensePlate}</b></color> ({vehicle.Brand} - {vehicle.Color}) đã được Ban quản lý phê duyệt cấp phép đỗ xe."
                            : $"Yêu cầu đăng ký phương tiện biển số <b>{vehicle.LicensePlate}</b> đã bị từ chối. Lý do: <color=#DC2626><b>{vehicle.RejectReason}</b></color>.",
                        Type = NotificationType.ApprovalResult,
                        Priority = isApproved ? NotificationPriority.Normal : NotificationPriority.Important,
                        TargetAudience = TargetAudienceType.SpecificResidents,
                        RelatedEntityType = "Vehicle",
                        RelatedEntityId = vehicle.Id,
                        CreatedAt = DateTime.Now
                    };
                    notif.Recipients.Add(new NotificationRecipient
                    {
                        UserAccountId = ownerAcc.Id,
                        IsRead = false,
                        CreatedAt = notif.CreatedAt
                    });
                    await _unitOfWork.Notifications.AddAsync(notif);
                    await _unitOfWork.CompleteAsync();
                }
            }
            catch { }
        }
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

        if (vehicle.RegistrationStatus == VehicleRegistrationStatus.Approved)
        {
            throw new BusinessRuleException("Phương tiện đã được phê duyệt, cư dân không thể tự xóa khỏi hệ thống. Vui lòng gửi yêu cầu hủy tới Ban quản lý.");
        }

        _unitOfWork.Vehicles.Delete(vehicle);
        await _unitOfWork.CompleteAsync();
    }

    /// <summary>
    /// Cư dân chủ động hủy đơn khi đơn còn đang ở trạng thái Chờ duyệt (Pending) -> chuyển sang Đã hủy (Cancelled).
    /// </summary>
    public async Task CancelPendingVehicleRegistrationAsync(int ownerId, int vehicleId)
    {
        var vehicle = await _unitOfWork.Vehicles.GetByIdAsync(vehicleId);
        if (vehicle == null)
        {
            throw new BusinessRuleException("Không tìm thấy phương tiện cần hủy đơn.");
        }

        if (vehicle.OwnerId != ownerId)
        {
            throw new BusinessRuleException("Bạn chỉ được hủy đơn đăng ký phương tiện của chính mình.");
        }

        if (vehicle.RegistrationStatus == VehicleRegistrationStatus.Approved)
        {
            throw new BusinessRuleException("Phương tiện đã được phê duyệt, cư dân không thể tự hủy đăng ký. Vui lòng gửi yêu cầu hủy tới Ban quản lý.");
        }

        if (vehicle.RegistrationStatus == VehicleRegistrationStatus.Cancelled)
        {
            throw new BusinessRuleException("Đơn đăng ký phương tiện này đã được hủy trước đó.");
        }

        vehicle.RegistrationStatus = VehicleRegistrationStatus.Cancelled;
        vehicle.RejectReason = "Cư dân chủ động hủy đơn đăng ký";
        _unitOfWork.Vehicles.Update(vehicle);
        await _unitOfWork.CompleteAsync();
    }

    /// <summary>
    /// Cư dân gửi yêu cầu hủy đăng ký xe đã duyệt (Approved) tới Ban quản lý tòa nhà (kèm lý do).
    /// </summary>
    public async Task RequestVehicleCancellationAsync(int ownerId, int vehicleId, string? reason)
    {
        var vehicle = await _unitOfWork.Vehicles.GetByIdAsync(vehicleId);
        if (vehicle == null)
        {
            throw new BusinessRuleException("Không tìm thấy phương tiện cần yêu cầu hủy.");
        }

        if (vehicle.OwnerId != ownerId)
        {
            throw new BusinessRuleException("Bạn chỉ được gửi yêu cầu hủy phương tiện của chính mình.");
        }

        if (vehicle.RegistrationStatus != VehicleRegistrationStatus.Approved)
        {
            throw new BusinessRuleException("Chỉ phương tiện đang hoạt động (Đã duyệt) mới có thể gửi yêu cầu hủy đăng ký.");
        }

        string cancelReason = string.IsNullOrWhiteSpace(reason) ? "Cư dân yêu cầu hủy đăng ký xe" : reason.Trim();
        vehicle.RejectReason = $"[Yêu cầu hủy]: {cancelReason}";
        _unitOfWork.Vehicles.Update(vehicle);
        await _unitOfWork.CompleteAsync();

        // Gửi thông báo yêu cầu hủy tới Ban quản trị
        if (_unitOfWork.Notifications != null && _unitOfWork.UserAccounts != null)
        {
            try
            {
                var adminAccounts = (await _unitOfWork.UserAccounts.GetAllAsync())
                    .Where(u => u.Role == UserRole.Admin && u.IsActive)
                    .ToList();
                if (adminAccounts.Count > 0)
                {
                    var owner = await _unitOfWork.Residents.GetByIdAsync(ownerId);
                    string ownerName = owner?.FullName ?? "Cư dân";
                    var notif = new Notification
                    {
                        Title = $"Yêu cầu hủy đăng ký xe: {vehicle.LicensePlate}",
                        Content = $"Cư dân <b>{ownerName}</b> gửi yêu cầu hủy đăng ký phương tiện biển số <color=#DC2626><b>{vehicle.LicensePlate}</b></color>. Lý do: <b>{cancelReason}</b>. Vui lòng kiểm tra và xử lý.",
                        Type = NotificationType.ApprovalRequest,
                        Priority = NotificationPriority.Important,
                        TargetAudience = TargetAudienceType.SystemAdmin,
                        RelatedEntityType = "Vehicle",
                        RelatedEntityId = vehicle.Id,
                        CreatedAt = DateTime.Now
                    };
                    foreach (var admin in adminAccounts)
                    {
                        notif.Recipients.Add(new NotificationRecipient
                        {
                            UserAccountId = admin.Id,
                            IsRead = false,
                            CreatedAt = notif.CreatedAt
                        });
                    }
                    await _unitOfWork.Notifications.AddAsync(notif);
                    await _unitOfWork.CompleteAsync();
                }
            }
            catch { }
        }
    }

    /// <summary>
    /// Ban quản lý thực hiện Hủy đăng ký xe (chuyển sang Cancelled, giải phóng Quota căn hộ và thông báo cư dân).
    /// </summary>
    public async Task CancelVehicleRegistrationByAdminAsync(int vehicleId, string? reason)
    {
        var vehicle = await _unitOfWork.Vehicles.GetByIdAsync(vehicleId);
        if (vehicle == null)
        {
            throw new BusinessRuleException("Không tìm thấy phương tiện cần hủy đăng ký.");
        }

        string finalReason = string.IsNullOrWhiteSpace(reason) ? "Ban quản lý đã thu hồi đăng ký phương tiện" : reason.Trim();
        vehicle.RegistrationStatus = VehicleRegistrationStatus.Cancelled;
        vehicle.RejectReason = finalReason;
        _unitOfWork.Vehicles.Update(vehicle);
        await _unitOfWork.CompleteAsync();

        // Gửi thông báo kết quả hủy về tài khoản cư dân
        if (_unitOfWork.Notifications != null && _unitOfWork.UserAccounts != null)
        {
            try
            {
                var ownerAcc = await _unitOfWork.UserAccounts.GetByResidentIdAsync(vehicle.OwnerId);
                if (ownerAcc != null && ownerAcc.IsActive)
                {
                    var notif = new Notification
                    {
                        Title = $"Đã hủy đăng ký phương tiện {vehicle.LicensePlate}",
                        Content = $"Phương tiện biển số <color=#2563EB><b>{vehicle.LicensePlate}</b></color> ({vehicle.Brand} - {vehicle.Color}) đã được Ban quản lý hoàn tất thủ tục hủy đăng ký. Lý do: <color=#DC2626><b>{finalReason}</b></color>. Chỗ để xe của căn hộ đã được cập nhật lại.",
                        Type = NotificationType.ApprovalResult,
                        Priority = NotificationPriority.Important,
                        TargetAudience = TargetAudienceType.SpecificResidents,
                        RelatedEntityType = "Vehicle",
                        RelatedEntityId = vehicle.Id,
                        CreatedAt = DateTime.Now
                    };
                    notif.Recipients.Add(new NotificationRecipient
                    {
                        UserAccountId = ownerAcc.Id,
                        IsRead = false,
                        CreatedAt = notif.CreatedAt
                    });
                    await _unitOfWork.Notifications.AddAsync(notif);
                    await _unitOfWork.CompleteAsync();
                }
            }
            catch { }
        }
    }

    private async Task<Vehicle> CreateVehicleCoreAsync(Vehicle vehicle, VehicleRegistrationStatus status)
    {
        await ValidateVehicleDataAsync(vehicle);
        await ValidateVehicleQuotaAsync(vehicle, isApprovalCheck: status == VehicleRegistrationStatus.Approved);
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
/// <summary>
    /// Kiểm tra hạn mức phương tiện của căn hộ (Quota) theo quy định vận hành.
    /// - Ô tô: Tối đa 1 xe / căn hộ.
    /// - Xe máy: Tối đa 2 xe / căn hộ.
    /// - Xe đạp: Tối đa 2 xe / căn hộ.
    /// Các xe ở trạng thái Cancelled hoặc Rejected KHÔNG tính vào hạn mức (được hoàn trả).
    /// </summary>
    public async Task ValidateVehicleQuotaAsync(Vehicle vehicle, int? excludeVehicleId = null, bool isApprovalCheck = false)
    {
        var residence = await _unitOfWork.ResidenceHistories.GetActiveByResidentIdAsync(vehicle.OwnerId);
        if (residence == null)
        {
            throw new BusinessRuleException("Cư dân phải có cư trú đang hoạt động để đăng ký hoặc duyệt phương tiện.");
        }

        int apartmentId = residence.ApartmentId;
        string apartmentNumber = residence.Apartment?.ApartmentNumber ?? string.Empty;
        if (string.IsNullOrWhiteSpace(apartmentNumber) && _unitOfWork.Apartments != null)
        {
            try
            {
                var apt = await _unitOfWork.Apartments.GetByIdAsync(apartmentId);
                if (apt != null)
                {
                    apartmentNumber = apt.ApartmentNumber;
                }
            }
            catch { }
        }
        string aptDisplay = string.IsNullOrWhiteSpace(apartmentNumber) ? $"#{apartmentId}" : apartmentNumber;

        // Lấy tất cả cư dân đang cư trú hoạt động trong cùng căn hộ
        HashSet<int> residentIds = new() { vehicle.OwnerId };
        try
        {
            var activeHistories = await _unitOfWork.ResidenceHistories.GetActiveByApartmentIdAsync(apartmentId);
            if (activeHistories != null)
            {
                foreach (var h in activeHistories)
                {
                    residentIds.Add(h.ResidentId);
                }
            }
        }
        catch { }

        // Lấy tất cả phương tiện của căn hộ này
        IEnumerable<Vehicle>? allVehicles = null;
        try
        {
            allVehicles = await _unitOfWork.Vehicles.GetAllWithDetailsAsync();
        }
        catch { }
        if (allVehicles == null)
        {
            try
            {
                allVehicles = await _unitOfWork.Vehicles.GetAllAsync();
            }
            catch { }
        }

        var aptVehicles = (allVehicles ?? Enumerable.Empty<Vehicle>())
            .Where(v => residentIds.Contains(v.OwnerId) && (!excludeVehicleId.HasValue || v.Id != excludeVehicleId.Value))
            .ToList();

        int maxAllowed = vehicle.VehicleType switch
        {
            VehicleType.Car => MaxCarsPerApartment,
            VehicleType.Moto => MaxMotosPerApartment,
            VehicleType.Bicycle => MaxBicyclesPerApartment,
            _ => 1
        };

        string typeName = vehicle.VehicleType switch
        {
            VehicleType.Car => "ô tô",
            VehicleType.Moto => "xe máy",
            VehicleType.Bicycle => "xe đạp",
            _ => "phương tiện"
        };

        if (isApprovalCheck)
        {
            // Khi duyệt: Chỉ đếm các xe đã Approved
            int approvedCount = aptVehicles.Count(v => v.RegistrationStatus == VehicleRegistrationStatus.Approved && v.VehicleType == vehicle.VehicleType);
            if (approvedCount >= maxAllowed)
            {
                throw new BusinessRuleException($"Không thể phê duyệt: Căn hộ {aptDisplay} đã đạt tối đa hạn mức {typeName} ({maxAllowed}/{maxAllowed} xe đã cấp phép). Vui lòng kiểm tra lại hồ sơ xe của căn hộ.");
            }
        }
        else
        {
            // Khi đăng ký mới / cập nhật: Đếm các xe Approved HOẶC Pending (loại trừ Cancelled và Rejected)
            int activeOrPendingCount = aptVehicles.Count(v =>
                (v.RegistrationStatus == VehicleRegistrationStatus.Approved || v.RegistrationStatus == VehicleRegistrationStatus.Pending)
                && v.VehicleType == vehicle.VehicleType);

            if (activeOrPendingCount >= maxAllowed)
            {
                throw new BusinessRuleException($"Căn hộ {aptDisplay} đã sử dụng hết hạn mức {typeName} (tối đa {maxAllowed} xe/căn hộ). Căn hộ không thể đăng ký thêm {typeName}. Vui lòng liên hệ Ban quản lý nếu cần hỗ trợ.");
            }
        }
    }

    /// <summary>
    /// Lấy thống kê số lượng xe và hạn mức của căn hộ
    /// </summary>
    public async Task<(int approvedCar, int pendingCar, int maxCar, int approvedMoto, int pendingMoto, int maxMoto)> GetApartmentVehicleQuotaSummaryAsync(int apartmentId)
    {
        HashSet<int> residentIds = new();
        try
        {
            var activeHistories = await _unitOfWork.ResidenceHistories.GetActiveByApartmentIdAsync(apartmentId);
            if (activeHistories != null)
            {
                foreach (var h in activeHistories)
                {
                    residentIds.Add(h.ResidentId);
                }
            }
        }
        catch { }

        IEnumerable<Vehicle>? allVehicles = null;
        try { allVehicles = await _unitOfWork.Vehicles.GetAllWithDetailsAsync(); } catch { }
        if (allVehicles == null)
        {
            try { allVehicles = await _unitOfWork.Vehicles.GetAllAsync(); } catch { }
        }

        var aptVehicles = (allVehicles ?? Enumerable.Empty<Vehicle>())
            .Where(v => residentIds.Contains(v.OwnerId))
            .ToList();

        int approvedCar = aptVehicles.Count(v => v.VehicleType == VehicleType.Car && v.RegistrationStatus == VehicleRegistrationStatus.Approved);
        int pendingCar = aptVehicles.Count(v => v.VehicleType == VehicleType.Car && v.RegistrationStatus == VehicleRegistrationStatus.Pending);

        int approvedMoto = aptVehicles.Count(v => (v.VehicleType == VehicleType.Moto || v.VehicleType == VehicleType.Bicycle) && v.RegistrationStatus == VehicleRegistrationStatus.Approved);
        int pendingMoto = aptVehicles.Count(v => (v.VehicleType == VehicleType.Moto || v.VehicleType == VehicleType.Bicycle) && v.RegistrationStatus == VehicleRegistrationStatus.Pending);

        return (approvedCar, pendingCar, MaxCarsPerApartment, approvedMoto, pendingMoto, MaxMotosPerApartment);
    }
}
