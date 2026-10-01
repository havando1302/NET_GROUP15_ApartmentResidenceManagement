using System.Threading.Tasks;
using ApartmentResidenceManagement.Domain.Entities;

namespace ApartmentResidenceManagement.Domain.Interfaces;

public interface IResidentRepository
{
    Task<Resident?> GetByIdAsync(int id);
}
