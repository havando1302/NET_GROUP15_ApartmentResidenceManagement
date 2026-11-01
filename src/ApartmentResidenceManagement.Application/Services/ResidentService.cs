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
}
