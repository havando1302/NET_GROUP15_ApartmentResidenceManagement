using System.Collections.Generic;
using System.Threading.Tasks;
using ApartmentResidenceManagement.Domain.Entities;

namespace ApartmentResidenceManagement.Domain.Interfaces;

public interface IMemberRegistrationRequestRepository : IRepository<MemberRegistrationRequest>
{
    Task<IEnumerable<MemberRegistrationRequest>> GetByApartmentIdAsync(int apartmentId);
    Task<IEnumerable<MemberRegistrationRequest>> GetByRequesterResidentIdAsync(int residentId);
    Task<IEnumerable<MemberRegistrationRequest>> GetPendingRequestsAsync();
    Task<IEnumerable<MemberRegistrationRequest>> GetAllWithDetailsAsync();
}
