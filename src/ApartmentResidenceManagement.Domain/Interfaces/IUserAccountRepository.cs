using System.Threading.Tasks;
using ApartmentResidenceManagement.Domain.Entities;

namespace ApartmentResidenceManagement.Domain.Interfaces;

public interface IUserAccountRepository
{
    Task<UserAccount?> GetByIdAsync(int id);
    Task<UserAccount?> GetByUsernameAsync(string username);
    Task<UserAccount?> GetByResidentIdAsync(int residentId);
    Task AddAsync(UserAccount account);
    void Update(UserAccount account);
}
