using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApartmentResidenceManagement.Domain.Entities;
using ApartmentResidenceManagement.Domain.Enums;
using ApartmentResidenceManagement.Domain.Interfaces;
using ApartmentResidenceManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ApartmentResidenceManagement.Infrastructure.Repositories;

public class MemberRegistrationRequestRepository : Repository<MemberRegistrationRequest>, IMemberRegistrationRequestRepository
{
    public MemberRegistrationRequestRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<MemberRegistrationRequest>> GetByApartmentIdAsync(int apartmentId)
    {
        return await DbSet
            .AsNoTracking()
            .Include(r => r.Apartment)
            .Include(r => r.RequesterResident)
            .Where(r => r.ApartmentId == apartmentId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<MemberRegistrationRequest>> GetByRequesterResidentIdAsync(int residentId)
    {
        return await DbSet
            .AsNoTracking()
            .Include(r => r.Apartment)
            .Include(r => r.RequesterResident)
            .Where(r => r.RequesterResidentId == residentId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<MemberRegistrationRequest>> GetPendingRequestsAsync()
    {
        return await DbSet
            .AsNoTracking()
            .Include(r => r.Apartment)
            .Include(r => r.RequesterResident)
            .Where(r => r.Status == RequestStatus.Pending)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<MemberRegistrationRequest>> GetAllWithDetailsAsync()
    {
        return await DbSet
            .AsNoTracking()
            .Include(r => r.Apartment)
            .Include(r => r.RequesterResident)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }
}
