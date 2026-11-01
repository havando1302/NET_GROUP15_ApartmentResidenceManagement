using System.Collections.Generic;
using System.Threading.Tasks;
using ApartmentResidenceManagement.Domain.Entities;
using ApartmentResidenceManagement.Domain.Interfaces;

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

    public async Task<Resident> UpdateContactAsync(int residentId, string? phoneNumber, string? email)
    {
        var resident = await _unitOfWork.Residents.GetByIdAsync(residentId);
        if (resident == null)
        {
            throw new Domain.Exceptions.BusinessRuleException("Không tìm thấy thông tin cư dân trong hệ thống.");
        }

        // Validate Phone (nếu có nhập)
        if (!string.IsNullOrWhiteSpace(phoneNumber))
        {
            phoneNumber = phoneNumber.Trim();
            if (phoneNumber.Length > 20)
            {
                throw new Domain.Exceptions.BusinessRuleException("Số điện thoại không được vượt quá 20 ký tự.");
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
                throw new Domain.Exceptions.BusinessRuleException("Email không được vượt quá 100 ký tự.");
            }
            if (!email.Contains('@') || !email.Contains('.'))
            {
                throw new Domain.Exceptions.BusinessRuleException("Địa chỉ email không đúng định dạng.");
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
        if (string.IsNullOrWhiteSpace(resident.FullName))
            throw new Domain.Exceptions.BusinessRuleException("Họ tên cư dân không được để trống.");

        if (resident.DateOfBirth > System.DateTime.Today)
            throw new Domain.Exceptions.BusinessRuleException("Ngày sinh không được ở tương lai.");

        if (!string.IsNullOrWhiteSpace(resident.IdentityCard))
        {
            resident.IdentityCard = resident.IdentityCard.Trim();
            var existing = await _unitOfWork.Residents.FindAsync(r => r.IdentityCard == resident.IdentityCard);
            if (System.Linq.Enumerable.Any(existing))
                throw new Domain.Exceptions.BusinessRuleException($"CCCD '{resident.IdentityCard}' đã tồn tại trong hệ thống.");
        }

        await _unitOfWork.Residents.AddAsync(resident);
        await _unitOfWork.CompleteAsync();
        return resident;
    }

    public async Task<Resident> UpdateResidentAsync(Resident resident)
    {
        var existing = await _unitOfWork.Residents.GetByIdAsync(resident.Id);
        if (existing == null)
            throw new Domain.Exceptions.BusinessRuleException("Không tìm thấy cư dân cần cập nhật.");

        if (string.IsNullOrWhiteSpace(resident.FullName))
            throw new Domain.Exceptions.BusinessRuleException("Họ tên cư dân không được để trống.");

        if (!string.IsNullOrWhiteSpace(resident.IdentityCard))
        {
            resident.IdentityCard = resident.IdentityCard.Trim();
            var duplicate = await _unitOfWork.Residents.FindAsync(r => r.IdentityCard == resident.IdentityCard && r.Id != resident.Id);
            if (System.Linq.Enumerable.Any(duplicate))
                throw new Domain.Exceptions.BusinessRuleException($"CCCD '{resident.IdentityCard}' đã thuộc về cư dân khác.");
        }

        existing.FullName = resident.FullName.Trim();
        existing.DateOfBirth = resident.DateOfBirth;
        existing.IdentityCard = string.IsNullOrWhiteSpace(resident.IdentityCard) ? null : resident.IdentityCard.Trim();
        existing.PhoneNumber = string.IsNullOrWhiteSpace(resident.PhoneNumber) ? null : resident.PhoneNumber.Trim();
        existing.Email = string.IsNullOrWhiteSpace(resident.Email) ? null : resident.Email.Trim();

        _unitOfWork.Residents.Update(existing);
        await _unitOfWork.CompleteAsync();
        return existing;
    }

    public async Task DeleteResidentAsync(int residentId)
    {
        var resident = await _unitOfWork.Residents.GetByIdAsync(residentId);
        if (resident == null)
            throw new Domain.Exceptions.BusinessRuleException("Không tìm thấy cư dân cần xóa.");

        var activeResidence = await _unitOfWork.ResidenceHistories.GetActiveByResidentIdAsync(residentId);
        if (activeResidence != null)
            throw new Domain.Exceptions.BusinessRuleException("Không thể xóa cư dân đang có hồ sơ cư trú hoạt động.");

        _unitOfWork.Residents.Delete(resident);
        await _unitOfWork.CompleteAsync();
    }
}
