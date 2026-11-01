using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApartmentResidenceManagement.Application.Common;
using ApartmentResidenceManagement.Domain.Entities;
using ApartmentResidenceManagement.Domain.Enums;
using ApartmentResidenceManagement.Domain.Interfaces;

namespace ApartmentResidenceManagement.Application.Services;

public class MemberRequestService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ResidenceService _residenceService;

    public MemberRequestService(IUnitOfWork unitOfWork, ResidenceService residenceService)
    {
        _unitOfWork = unitOfWork;
        _residenceService = residenceService;
    }

    public async Task<OperationResult<MemberRegistrationRequest>> CreateRequestAsync(
        int apartmentId,
        int requesterResidentId,
        string fullName,
        DateTime dateOfBirth,
        string? identityCard,
        string? phoneNumber,
        string? email,
        RelationshipType relationshipType)
    {
        if (string.IsNullOrWhiteSpace(fullName) || fullName.Trim().Length < 2 || fullName.Trim().Length > 100)
            return OperationResult<MemberRegistrationRequest>.Failure("Họ và tên bắt buộc từ 2 đến 100 ký tự và không chỉ chứa khoảng trắng.");

        if (dateOfBirth > DateTime.Today)
            return OperationResult<MemberRegistrationRequest>.Failure("Ngày sinh không được lớn hơn ngày hiện tại.");

        if (!string.IsNullOrWhiteSpace(identityCard))
        {
            identityCard = identityCard.Trim();
            if (!System.Text.RegularExpressions.Regex.IsMatch(identityCard, @"^\d{12}$"))
                return OperationResult<MemberRegistrationRequest>.Failure("Số CCCD phải gồm đúng 12 chữ số.");
        }
        else
        {
            identityCard = null;
        }

        if (!string.IsNullOrWhiteSpace(phoneNumber))
        {
            phoneNumber = phoneNumber.Trim();
            if (!System.Text.RegularExpressions.Regex.IsMatch(phoneNumber, @"^0\d{9}$"))
                return OperationResult<MemberRegistrationRequest>.Failure("Số điện thoại phải đúng định dạng 10 chữ số bắt đầu bằng số 0.");
        }
        else
        {
            phoneNumber = null;
        }

        if (!string.IsNullOrWhiteSpace(email))
        {
            email = email.Trim();
            if (!System.Text.RegularExpressions.Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                return OperationResult<MemberRegistrationRequest>.Failure("Địa chỉ email không đúng định dạng hợp lệ.");
        }
        else
        {
            email = null;
        }

        var apartment = await _unitOfWork.Apartments.GetByIdAsync(apartmentId);
        if (apartment == null)
            return OperationResult<MemberRegistrationRequest>.Failure("Không tìm thấy căn hộ.");

        var requester = await _unitOfWork.Residents.GetByIdAsync(requesterResidentId);
        if (requester == null)
            return OperationResult<MemberRegistrationRequest>.Failure("Không tìm thấy thông tin cư dân gửi yêu cầu.");

        var activeResidences = await _unitOfWork.ResidenceHistories.GetActiveByApartmentIdAsync(apartmentId);
        if (!activeResidences.Any(r => r.ResidentId == requesterResidentId))
            return OperationResult<MemberRegistrationRequest>.Failure("Bạn không phải là cư dân đang cư trú tại căn hộ này.");

        // Kiểm tra trùng yêu cầu đang chờ duyệt (Pending)
        var existingRequests = await _unitOfWork.MemberRegistrationRequests.GetByApartmentIdAsync(apartmentId);
        if (existingRequests.Any(r => r.Status == RequestStatus.Pending && 
            (r.FullName.Trim().Equals(fullName.Trim(), StringComparison.OrdinalIgnoreCase) ||
             (!string.IsNullOrEmpty(identityCard) && !string.IsNullOrEmpty(r.IdentityCard) && r.IdentityCard == identityCard))))
        {
            return OperationResult<MemberRegistrationRequest>.Failure("Đã có yêu cầu thêm thành viên này (trùng họ tên hoặc CCCD) đang chờ phê duyệt.");
        }

        // Kiểm tra tình trạng cư trú hiện tại của người được thêm (nếu đã có CCCD trong hệ thống)
        if (!string.IsNullOrWhiteSpace(identityCard))
        {
            var foundResidents = await _unitOfWork.Residents.FindAsync(r => r.IdentityCard == identityCard);
            var existingResident = foundResidents.FirstOrDefault();
            if (existingResident != null)
            {
                var activeRes = await _unitOfWork.ResidenceHistories.GetActiveByResidentIdAsync(existingResident.Id);
                if (activeRes != null)
                {
                    if (activeRes.ApartmentId == apartmentId)
                    {
                        return OperationResult<MemberRegistrationRequest>.Failure($"Cư dân {existingResident.FullName} (CCCD: {identityCard}) hiện đã đang cư trú tại căn hộ này.");
                    }
                    else
                    {
                        return OperationResult<MemberRegistrationRequest>.Failure($"Cư dân {existingResident.FullName} (CCCD: {identityCard}) hiện đang cư trú tại căn hộ {activeRes.Apartment?.ApartmentNumber ?? activeRes.ApartmentId.ToString()}. Vui lòng hoàn tất chuyển cư trú trước.");
                    }
                }
            }
        }

        var request = new MemberRegistrationRequest
        {
            ApartmentId = apartmentId,
            RequesterResidentId = requesterResidentId,
            FullName = fullName.Trim(),
            DateOfBirth = dateOfBirth.Date,
            IdentityCard = identityCard,
            PhoneNumber = phoneNumber,
            Email = email,
            RelationshipType = relationshipType,
            Status = RequestStatus.Pending,
            CreatedAt = DateTime.Now
        };

        try
        {
            await _unitOfWork.MemberRegistrationRequests.AddAsync(request);
            await _unitOfWork.CompleteAsync();
            return OperationResult<MemberRegistrationRequest>.Success(request);
        }
        catch (Exception ex)
        {
            return OperationResult<MemberRegistrationRequest>.Failure($"Lỗi khi lưu yêu cầu: {ex.Message}");
        }
    }

    public async Task<IEnumerable<MemberRegistrationRequest>> GetRequestsByApartmentIdAsync(int apartmentId)
    {
        return await _unitOfWork.MemberRegistrationRequests.GetByApartmentIdAsync(apartmentId);
    }

    public async Task<IEnumerable<MemberRegistrationRequest>> GetRequestsByResidentIdAsync(int residentId)
    {
        return await _unitOfWork.MemberRegistrationRequests.GetByRequesterResidentIdAsync(residentId);
    }

    public async Task<IEnumerable<MemberRegistrationRequest>> GetPendingRequestsAsync()
    {
        return await _unitOfWork.MemberRegistrationRequests.GetPendingRequestsAsync();
    }

    public async Task<IEnumerable<MemberRegistrationRequest>> GetAllRequestsWithDetailsAsync()
    {
        return await _unitOfWork.MemberRegistrationRequests.GetAllWithDetailsAsync();
    }

    public async Task<OperationResult> ApproveRequestAsync(int requestId)
    {
        try
        {
            return await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var request = await _unitOfWork.MemberRegistrationRequests.GetByIdAsync(requestId);
                if (request == null)
                    return OperationResult.Failure("Không tìm thấy yêu cầu.");

                if (request.Status != RequestStatus.Pending)
                    return OperationResult.Failure("Yêu cầu này đã được xử lý trước đó.");

                Resident? resident = null;
                if (!string.IsNullOrWhiteSpace(request.IdentityCard))
                {
                    var found = await _unitOfWork.Residents.FindAsync(r => r.IdentityCard == request.IdentityCard);
                    resident = found.FirstOrDefault();
                }

                if (resident == null)
                {
                    resident = new Resident
                    {
                        FullName = request.FullName,
                        DateOfBirth = request.DateOfBirth,
                        IdentityCard = request.IdentityCard,
                        PhoneNumber = request.PhoneNumber,
                        Email = request.Email
                    };
                    await _unitOfWork.Residents.AddAsync(resident);
                    await _unitOfWork.CompleteAsync();
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(resident.PhoneNumber) && !string.IsNullOrWhiteSpace(request.PhoneNumber))
                        resident.PhoneNumber = request.PhoneNumber;
                    if (string.IsNullOrWhiteSpace(resident.Email) && !string.IsNullOrWhiteSpace(request.Email))
                        resident.Email = request.Email;
                    _unitOfWork.Residents.Update(resident);
                    await _unitOfWork.CompleteAsync();
                }

                var regResult = await _residenceService.RegisterResidenceAsync(
                    request.ApartmentId,
                    resident.Id,
                    request.RelationshipType,
                    DateTime.Today);

                if (!regResult.IsSuccess)
                {
                    throw new Domain.Exceptions.BusinessRuleException(regResult.ErrorMessage ?? "Không thể tạo bản ghi cư trú.");
                }

                request.Status = RequestStatus.Approved;
                request.ProcessedAt = DateTime.Now;
                _unitOfWork.MemberRegistrationRequests.Update(request);
                await _unitOfWork.CompleteAsync();

                return OperationResult.Success();
            });
        }
        catch (Exception ex)
        {
            return OperationResult.Failure(ex.Message);
        }
    }

    public async Task<OperationResult> RejectRequestAsync(int requestId, string? reason)
    {
        var request = await _unitOfWork.MemberRegistrationRequests.GetByIdAsync(requestId);
        if (request == null)
            return OperationResult.Failure("Không tìm thấy yêu cầu.");

        if (request.Status != RequestStatus.Pending)
            return OperationResult.Failure("Yêu cầu này đã được xử lý trước đó.");

        request.Status = RequestStatus.Rejected;
        request.ProcessedAt = DateTime.Now;
        request.RejectReason = string.IsNullOrWhiteSpace(reason) ? "Không đủ điều kiện phê duyệt" : reason.Trim();

        _unitOfWork.MemberRegistrationRequests.Update(request);
        await _unitOfWork.CompleteAsync();

        return OperationResult.Success();
    }
}
