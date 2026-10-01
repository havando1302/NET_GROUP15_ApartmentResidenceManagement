using System;
using System.Threading.Tasks;

namespace ApartmentResidenceManagement.Domain.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IUserAccountRepository UserAccounts { get; }
    IResidentRepository Residents { get; }
    Task<int> CompleteAsync();
}
