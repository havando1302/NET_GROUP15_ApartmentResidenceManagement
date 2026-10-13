using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApartmentResidenceManagement.Application.Common;
using ApartmentResidenceManagement.Domain.Entities;
using ApartmentResidenceManagement.Domain.Enums;
using ApartmentResidenceManagement.Domain.Exceptions;
using ApartmentResidenceManagement.Domain.Interfaces;

namespace ApartmentResidenceManagement.Application.Services;

public class ResidenceService
{
    private readonly IUnitOfWork _unitOfWork;

    public ResidenceService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<ResidenceHistory>> GetActiveResidencesByApartmentIdAsync(int apartmentId)
    {
        return await _unitOfWork.ResidenceHistories.GetActiveByApartmentIdAsync(apartmentId);
    }

    public async Task<ResidenceHistory?> GetActiveResidenceByResidentIdAsync(int residentId)
    {
        return await _unitOfWork.ResidenceHistories.GetActiveByResidentIdAsync(residentId);
    }

    public async Task<IEnumerable<ResidenceHistory>> GetHistoryByResidentIdAsync(int residentId)
    {
        return await _unitOfWork.ResidenceHistories.GetHistoryByResidentIdAsync(residentId);
    }

    public async Task<OperationResult<ResidenceHistory>> RegisterResidenceAsync(
        int apartmentId,
        int residentId,
        RelationshipType relationshipType,
        DateTime startDate)
    {
        startDate = startDate.Date;

        if (!IsValidRelationshipType(relationshipType))
            return OperationResult<ResidenceHistory>.Failure("Quan hệ cư trú không hợp lệ.");

        if (startDate > DateTime.Today)
            return OperationResult<ResidenceHistory>.Failure("Ngày bắt đầu cư trú không được ở tương lai.");

        var apartment = await _unitOfWork.Apartments.GetByIdAsync(apartmentId);
        if (apartment == null)
            return OperationResult<ResidenceHistory>.Failure("Không tìm thấy căn hộ.");

        if (apartment.Status == ApartmentStatus.UnderMaintenance)
            return OperationResult<ResidenceHistory>.Failure("Không thể đăng ký cư trú vào căn hộ đang sửa chữa.");

        var resident = await _unitOfWork.Residents.GetByIdAsync(residentId);
        if (resident == null)
            return OperationResult<ResidenceHistory>.Failure("Không tìm thấy cư dân.");

        var activeResidence = await _unitOfWork.ResidenceHistories.GetActiveByResidentIdAsync(residentId);
        if (activeResidence != null)
        {
            return OperationResult<ResidenceHistory>.Failure(
                $"Cư dân này hiện đang cư trú tại căn hộ {activeResidence.Apartment?.ApartmentNumber ?? activeResidence.ApartmentId.ToString()}. " +
                "Vui lòng kết thúc cư trú cũ trước.");
        }

        if (relationshipType == RelationshipType.Owner)
        {
            var activeResidences = await _unitOfWork.ResidenceHistories.GetActiveByApartmentIdAsync(apartmentId);
            if (activeResidences.Any(history => history.RelationshipType == RelationshipType.Owner))
            {
                return OperationResult<ResidenceHistory>.Failure(
                    $"Căn hộ {apartment.ApartmentNumber} đã có Chủ hộ. Không thể thêm Chủ hộ mới.");
            }
        }

        if (startDate < resident.DateOfBirth.Date)
        {
            return OperationResult<ResidenceHistory>.Failure(
                "Ngày bắt đầu cư trú không được nhỏ hơn ngày sinh của cư dân.");
        }

        var residenceHistory = await _unitOfWork.ResidenceHistories.GetHistoryByResidentIdAsync(residentId)
            ?? Enumerable.Empty<ResidenceHistory>();
        if (residenceHistory.Any(history =>
                history.StartDate.Date >= startDate ||
                !history.EndDate.HasValue ||
                history.EndDate.Value.Date > startDate))
        {
            return OperationResult<ResidenceHistory>.Failure(
                "Ngày bắt đầu cư trú bị chồng lấn với lịch sử cư trú đã có của cư dân.");
        }

        var residence = new ResidenceHistory
        {
            ApartmentId = apartmentId,
            ResidentId = residentId,
            RelationshipType = relationshipType,
            StartDate = startDate,
            IsActive = true
        };

        apartment.Status = ApartmentStatus.Occupied;
        _unitOfWork.Apartments.Update(apartment);

        try
        {
            await _unitOfWork.ResidenceHistories.AddAsync(residence);
            await _unitOfWork.CompleteAsync();
            return OperationResult<ResidenceHistory>.Success(residence);
        }
        catch (BusinessRuleException ex)
        {
            return OperationResult<ResidenceHistory>.Failure(ex.Message);
        }
    }

    public async Task<OperationResult> TerminateResidenceAsync(int residenceId, DateTime endDate)
    {
        endDate = endDate.Date;

        var residence = await _unitOfWork.ResidenceHistories.GetByIdAsync(residenceId);
        if (residence == null)
            return OperationResult.Failure("Không tìm thấy thông tin cư trú.");

        if (!residence.IsActive)
            return OperationResult.Failure("Thông tin cư trú này đã được kết thúc trước đó.");

        if (endDate > DateTime.Today)
            return OperationResult.Failure("Ngày kết thúc cư trú không được ở tương lai.");

        if (endDate < residence.StartDate.Date)
            return OperationResult.Failure("Ngày kết thúc cư trú không được nhỏ hơn ngày bắt đầu.");

        residence.IsActive = false;
        residence.EndDate = endDate;
        _unitOfWork.ResidenceHistories.Update(residence);

        var activeResidences = await _unitOfWork.ResidenceHistories
            .GetActiveByApartmentIdAsync(residence.ApartmentId);
        var countActive = activeResidences.Count(history => history.Id != residenceId);

        var apartment = await _unitOfWork.Apartments.GetByIdAsync(residence.ApartmentId);
        if (apartment != null)
        {
            apartment.Status = countActive == 0 ? ApartmentStatus.Empty : ApartmentStatus.Occupied;
            _unitOfWork.Apartments.Update(apartment);
        }

        try
        {
            await _unitOfWork.CompleteAsync();
            return OperationResult.Success();
        }
        catch (BusinessRuleException ex)
        {
            return OperationResult.Failure(ex.Message);
        }
    }

    public async Task<IEnumerable<ResidenceHistory>> GetAllResidencesAsync()
    {
        return await _unitOfWork.ResidenceHistories.GetAllWithDetailsAsync();
    }

    private static bool IsValidRelationshipType(RelationshipType relationshipType) =>
        Enum.IsDefined(relationshipType);
}
