using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApartmentResidenceManagement.Domain.Entities;
using ApartmentResidenceManagement.Domain.Exceptions;
using ApartmentResidenceManagement.Domain.Interfaces;
using ApartmentResidenceManagement.Application.Validators;
using ApartmentResidenceManagement.Application.Common;

namespace ApartmentResidenceManagement.Application.Services;

public class ResidentService
{
    private readonly IUnitOfWork _unitOfWork;

    public ResidentService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<Resident>> GetAllResidentsAsync()
    {
        return await _unitOfWork.Residents.GetAllAsync();
    }

    public async Task<IEnumerable<Resident>> GetAllWithDetailsAsync()
    {
        var result = await _unitOfWork.Residents.GetAllWithDetailsAsync();
        if (result != null && result.Any()) return result;
        return await _unitOfWork.Residents.GetAllAsync();
    }

    public async Task<Resident?> GetResidentByIdAsync(int id)
    {
        return await _unitOfWork.Residents.GetByIdAsync(id);
    }

    public async Task<Resident?> GetResidentByIdentityCardAsync(string identityCard)
    {
        if (string.IsNullOrWhiteSpace(identityCard)) return null;
        return await _unitOfWork.Residents.GetByIdentityCardAsync(identityCard.Trim());
    }

    public async Task<Resident> UpdateContactAsync(int residentId, string? phoneNumber, string? email)
    {
        var resident = await _unitOfWork.Residents.GetByIdAsync(residentId);
        if (resident == null)
        {
            throw new BusinessRuleException("Không tìm thấy thông tin cư dân trong hệ thống.");
        }

        // Validate Phone (nếu có nhập)
        if (!string.IsNullOrWhiteSpace(phoneNumber))
        {
            phoneNumber = phoneNumber.Trim();
            if (!System.Text.RegularExpressions.Regex.IsMatch(phoneNumber, @"^0\d{9}$"))
            {
                throw new BusinessRuleException("Số điện thoại không hợp lệ (phải gồm 10 chữ số và bắt đầu bằng số 0).");
            }
            resident.PhoneNumber = phoneNumber;
        }
        else
        {
            resident.PhoneNumber = null;
        }

        // Validate Email (nếu có nhập)
        if (!string.IsNullOrWhiteSpace(email))
        {
            email = email.Trim();
            if (email.Length > 100)
            {
                throw new BusinessRuleException("Email không được vượt quá 100 ký tự.");
            }
            if (!System.Text.RegularExpressions.Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                throw new BusinessRuleException("Địa chỉ email không đúng định dạng hợp lệ.");
            }
            resident.Email = email;
        }
        else
        {
            resident.Email = null;
        }

        _unitOfWork.Residents.Update(resident);
        await _unitOfWork.CompleteAsync();

        return resident;
    }

    public async Task<Resident> CreateResidentAsync(Resident resident)
    {
        Normalize(resident);

        if (string.IsNullOrWhiteSpace(resident.FullName))
        {
            throw new BusinessRuleException("Họ và tên cư dân không được để trống.");
        }

        ValidateFieldLengthsAndGender(resident);

        if (!InputValidator.ValidateDateOfBirth(resident.DateOfBirth))
        {
            throw new BusinessRuleException("Ngày sinh không hợp lệ (không được ở tương lai hoặc quá 120 tuổi).");
        }

        // Validate số điện thoại nếu có
        if (!string.IsNullOrWhiteSpace(resident.PhoneNumber) && !InputValidator.ValidatePhone(resident.PhoneNumber))
        {
            throw new BusinessRuleException("Số điện thoại không hợp lệ.");
        }

        // Validate CCCD nếu có
        if (!string.IsNullOrWhiteSpace(resident.IdentityCard))
        {
            if (!InputValidator.ValidateIdentityCard(resident.IdentityCard))
            {
                throw new BusinessRuleException("Số CCCD/CMND không hợp lệ (bắt buộc phải là 9 hoặc 12 số).");
            }

            var existing = await _unitOfWork.Residents.GetByIdentityCardAsync(resident.IdentityCard);
            if (existing != null)
            {
                throw new BusinessRuleException($"Cư dân có số CCCD/CMND '{resident.IdentityCard}' đã tồn tại trong hệ thống.");
            }
        }

        if (string.IsNullOrWhiteSpace(resident.AvatarPath))
        {
            resident.AvatarPath = ResidentProfileHelper.GetDefaultAvatar(resident.FullName, resident.Gender, resident.Id);
        }

        await _unitOfWork.Residents.AddAsync(resident);
        await _unitOfWork.CompleteAsync();

        return resident;
    }

    public async Task<Resident> UpdateResidentAsync(Resident resident)
    {
        Normalize(resident);

        var existing = await _unitOfWork.Residents.GetByIdAsync(resident.Id);
        if (existing == null)
        {
            throw new BusinessRuleException("Không tìm thấy cư dân cần cập nhật.");
        }

        if (string.IsNullOrWhiteSpace(resident.FullName))
        {
            throw new BusinessRuleException("Họ và tên cư dân không được để trống.");
        }

        ValidateFieldLengthsAndGender(resident);

        if (!InputValidator.ValidateDateOfBirth(resident.DateOfBirth))
        {
            throw new BusinessRuleException("Ngày sinh không hợp lệ.");
        }

        if (!string.IsNullOrWhiteSpace(resident.PhoneNumber) && !InputValidator.ValidatePhone(resident.PhoneNumber))
        {
            throw new BusinessRuleException("Số điện thoại không hợp lệ.");
        }

        // Validate và kiểm tra trùng CCCD
        if (!string.IsNullOrWhiteSpace(resident.IdentityCard))
        {
            if (!InputValidator.ValidateIdentityCard(resident.IdentityCard))
            {
                throw new BusinessRuleException("Số CCCD/CMND không hợp lệ.");
            }

            if (existing.IdentityCard != resident.IdentityCard)
            {
                var duplicate = await _unitOfWork.Residents.GetByIdentityCardAsync(resident.IdentityCard);
                if (duplicate != null && duplicate.Id != existing.Id)
                {
                    throw new BusinessRuleException($"Số CCCD/CMND '{resident.IdentityCard}' đã được đăng ký bởi cư dân khác.");
                }
            }
        }

        existing.FullName = resident.FullName;
        existing.DateOfBirth = resident.DateOfBirth;
        existing.Gender = resident.Gender;
        existing.IdentityCard = resident.IdentityCard;
        existing.PhoneNumber = resident.PhoneNumber;
        existing.HomeTown = resident.HomeTown;
        existing.Email = NormalizeOptional(resident.Email);
        existing.AvatarPath = resident.AvatarPath;

        _unitOfWork.Residents.Update(existing);
        await _unitOfWork.CompleteAsync();

        return existing;
    }

    public async Task<Resident> UpdateAvatarAsync(int residentId, string? avatarPath)
    {
        var resident = await _unitOfWork.Residents.GetByIdAsync(residentId);
        if (resident == null)
        {
            throw new BusinessRuleException("Không tìm thấy cư dân cần cập nhật ảnh đại diện.");
        }

        resident.AvatarPath = avatarPath;
        _unitOfWork.Residents.Update(resident);
        await _unitOfWork.CompleteAsync();

        return resident;
    }

    public async Task DeleteResidentAsync(int id)
    {
        var resident = await _unitOfWork.Residents.GetByIdAsync(id);
        if (resident == null)
        {
            throw new BusinessRuleException("Không tìm thấy cư dân cần xóa.");
        }

        if (_unitOfWork.ResidenceHistories != null)
        {
            var activeResidence = await _unitOfWork.ResidenceHistories.GetActiveByResidentIdAsync(id);
            if (activeResidence != null)
            {
                throw new BusinessRuleException("Không thể xóa cư dân đang có hồ sơ cư trú hoạt động.");
            }

            var allHistories = await _unitOfWork.ResidenceHistories.GetHistoryByResidentIdAsync(id);
            if (allHistories != null && allHistories.Any())
            {
                throw new BusinessRuleException("Không thể xóa cư dân đã có dữ liệu lịch sử cư trú trong hệ thống. Vui lòng lưu trữ hồ sơ.");
            }
        }

        if (_unitOfWork.MemberRegistrationRequests != null)
        {
            var requests = await _unitOfWork.MemberRegistrationRequests.GetByRequesterResidentIdAsync(id);
            if (requests != null && requests.Any())
            {
                throw new BusinessRuleException("Không thể xóa cư dân đã phát sinh các yêu cầu thành viên.");
            }
        }

        if (_unitOfWork.Vehicles != null)
        {
            var vehicles = await _unitOfWork.Vehicles.GetVehiclesByOwnerIdAsync(id);
            if (vehicles != null && vehicles.Any())
            {
                throw new BusinessRuleException("Không thể xóa cư dân đang có phương tiện đăng ký trên hệ thống.");
            }
        }

        if (_unitOfWork.UserAccounts != null)
        {
            var account = await _unitOfWork.UserAccounts.GetByResidentIdAsync(id);
            if (account != null)
            {
                _unitOfWork.UserAccounts.Delete(account);
            }
        }

        _unitOfWork.Residents.Delete(resident);
        await _unitOfWork.CompleteAsync();
    }

    private static void Normalize(Resident resident)
    {
        resident.FullName = resident.FullName?.Trim() ?? string.Empty;
        resident.DateOfBirth = resident.DateOfBirth.Date;
        resident.IdentityCard = NormalizeOptional(resident.IdentityCard);
        resident.PhoneNumber = NormalizeOptional(resident.PhoneNumber);
        resident.HomeTown = NormalizeOptional(resident.HomeTown);
        resident.Email = NormalizeOptional(resident.Email);
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static void ValidateFieldLengthsAndGender(Resident resident)
    {
        if (resident.FullName.Length > 100)
        {
            throw new BusinessRuleException("Họ và tên không được vượt quá 100 ký tự.");
        }

        if (resident.HomeTown?.Length > 200)
        {
            throw new BusinessRuleException("Quê quán không được vượt quá 200 ký tự.");
        }

        if (!Enum.IsDefined(resident.Gender))
        {
            throw new BusinessRuleException("Giới tính không hợp lệ.");
        }
    }
}
