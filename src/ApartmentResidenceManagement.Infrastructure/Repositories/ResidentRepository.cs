using ApartmentResidenceManagement.Domain.Entities;
using ApartmentResidenceManagement.Domain.Interfaces;
using ApartmentResidenceManagement.Infrastructure.Data;

namespace ApartmentResidenceManagement.Infrastructure.Repositories;

public class ResidentRepository : Repository<Resident>, IResidentRepository
{
    public ResidentRepository(AppDbContext context) : base(context)
    {
    }
}

