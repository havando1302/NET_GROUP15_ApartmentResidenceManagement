using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ApartmentResidenceManagement.Domain.Entities;
using ApartmentResidenceManagement.Domain.Enums;
using ApartmentResidenceManagement.Domain.Exceptions;
using ApartmentResidenceManagement.Domain.Interfaces;

namespace ApartmentResidenceManagement.Application.Services;

public class ApartmentService
{
    private readonly IUnitOfWork _unitOfWork;

    public ApartmentService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<Apartment>> GetAllApartmentsAsync()
    {
        return await _unitOfWork.Apartments.GetAllAsync();
    }

    public async Task<Apartment?> GetApartmentByIdAsync(int id)
    {
        return await _unitOfWork.Apartments.GetByIdAsync(id);
    }

    public async Task<Apartment?> GetApartmentByNumberAsync(string apartmentNumber)
    {
        if (string.IsNullOrWhiteSpace(apartmentNumber)) return null;
        return await _unitOfWork.Apartments.GetByApartmentNumberAsync(apartmentNumber.Trim().ToUpperInvariant());
    }

    public async Task<Apartment> CreateApartmentAsync(Apartment apartment)
    {
        apartment.ApartmentNumber = apartment.ApartmentNumber?.Trim().ToUpperInvariant() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(apartment.ApartmentNumber))
        {
            throw new BusinessRuleException("Số căn hộ không được để trống.");
        }

        if (apartment.ApartmentNumber.Length > 20)
        {
            throw new BusinessRuleException("Số căn hộ không được vượt quá 20 ký tự.");
        }

        var existing = await _unitOfWork.Apartments.GetByApartmentNumberAsync(apartment.ApartmentNumber);
        if (existing != null)
        {
            throw new BusinessRuleException($"Số căn hộ '{apartment.ApartmentNumber}' đã tồn tại trong hệ thống.");
        }

        if (apartment.Floor <= 0)
        {
            throw new BusinessRuleException("Số tầng phải lớn hơn 0.");
        }

        if (!double.IsFinite(apartment.Area) || apartment.Area <= 0)
        {
            throw new BusinessRuleException("Diện tích căn hộ phải lớn hơn 0.");
        }

        apartment.Status = ApartmentStatus.Empty; // Mặc định khi tạo mới là Trống

        await _unitOfWork.Apartments.AddAsync(apartment);
        await _unitOfWork.CompleteAsync();

        return apartment;
    }

    public async Task UpdateApartmentAsync(Apartment apartment)
    {
        apartment.ApartmentNumber = apartment.ApartmentNumber?.Trim().ToUpperInvariant() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(apartment.ApartmentNumber))
        {
            throw new BusinessRuleException("Số căn hộ không được để trống.");
        }

        if (apartment.ApartmentNumber.Length > 20)
        {
            throw new BusinessRuleException("Số căn hộ không được vượt quá 20 ký tự.");
        }

        if (!Enum.IsDefined(apartment.Status))
        {
            throw new BusinessRuleException("Trạng thái căn hộ không hợp lệ.");
        }

        var existing = await _unitOfWork.Apartments.GetByIdAsync(apartment.Id);
        if (existing == null)
        {
            throw new BusinessRuleException("Không tìm thấy căn hộ cần cập nhật.");
        }

        // Nếu thay đổi số căn hộ, kiểm tra xem có trùng với căn hộ khác không
        if (existing.ApartmentNumber != apartment.ApartmentNumber)
        {
            var duplicate = await _unitOfWork.Apartments.GetByApartmentNumberAsync(apartment.ApartmentNumber);
            if (duplicate != null && duplicate.Id != existing.Id)
            {
                throw new BusinessRuleException($"Số căn hộ '{apartment.ApartmentNumber}' đã được sử dụng bởi căn hộ khác.");
            }
        }

        if (apartment.Floor <= 0)
        {
            throw new BusinessRuleException("Số tầng phải lớn hơn 0.");
        }

        if (!double.IsFinite(apartment.Area) || apartment.Area <= 0)
        {
            throw new BusinessRuleException("Diện tích căn hộ phải lớn hơn 0.");
        }

        existing.ApartmentNumber = apartment.ApartmentNumber;
        existing.Floor = apartment.Floor;
        existing.Area = apartment.Area;
        existing.Status = apartment.Status;

        _unitOfWork.Apartments.Update(existing);
        await _unitOfWork.CompleteAsync();
    }

    public async Task DeleteApartmentAsync(int id)
    {
        var apartment = await _unitOfWork.Apartments.GetByIdAsync(id);
        if (apartment == null)
        {
            throw new BusinessRuleException("Không tìm thấy căn hộ cần xóa.");
        }

        _unitOfWork.Apartments.Delete(apartment);
        await _unitOfWork.CompleteAsync();
    }
}
