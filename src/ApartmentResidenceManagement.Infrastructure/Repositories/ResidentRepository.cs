using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApartmentResidenceManagement.Domain.Entities;
using ApartmentResidenceManagement.Domain.Interfaces;
using ApartmentResidenceManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ApartmentResidenceManagement.Infrastructure.Repositories;

public class ResidentRepository : Repository<Resident>, IResidentRepository
{
    public ResidentRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<Resident?> GetByIdentityCardAsync(string identityCard)
    {
        return await DbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.IdentityCard == identityCard);
    }

    public async Task<IEnumerable<Resident>> GetResidentsByApartmentIdAsync(int apartmentId)
    {
        // Khi bảng ResidenceHistories được Đô tạo, sẽ nối bảng để lấy danh sách cư dân theo căn hộ
        return await Task.FromResult(Enumerable.Empty<Resident>());
    }
}
