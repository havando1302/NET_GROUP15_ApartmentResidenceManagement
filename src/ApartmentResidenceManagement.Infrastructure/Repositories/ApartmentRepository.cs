using System.Threading.Tasks;
using System.Linq;
using ApartmentResidenceManagement.Domain.Entities;
using ApartmentResidenceManagement.Domain.Interfaces;
using ApartmentResidenceManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ApartmentResidenceManagement.Infrastructure.Repositories;

public class ApartmentRepository : Repository<Apartment>, IApartmentRepository
{
    public ApartmentRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<Apartment?> LockForVehicleQuotaAsync(int apartmentId)
    {
        var rows = await DbSet.FromSqlInterpolated($"SELECT * FROM `Apartments` WHERE `Id` = {apartmentId} FOR UPDATE")
            .AsNoTracking().ToListAsync();
        return rows.FirstOrDefault();
    }

    public async Task<Apartment?> GetByApartmentNumberAsync(string apartmentNumber)
    {
        return await DbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.ApartmentNumber == apartmentNumber);
    }
}
