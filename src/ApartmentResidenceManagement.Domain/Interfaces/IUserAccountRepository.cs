using System.Collections.Generic;
using System.Threading.Tasks;
using ApartmentResidenceManagement.Domain.Entities;

namespace ApartmentResidenceManagement.Domain.Interfaces;

public interface IUserAccountRepository : IRepository<UserAccount>
{
    Task<UserAccount?> GetByUsernameAsync(string username);
    Task<UserAccount?> GetByResidentIdAsync(int residentId);
    Task<IEnumerable<UserAccount>> GetAllWithResidentAsync();
}
