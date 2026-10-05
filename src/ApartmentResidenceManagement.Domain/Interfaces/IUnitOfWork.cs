using System;
using System.Threading.Tasks;

namespace ApartmentResidenceManagement.Domain.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IResidentRepository Residents { get; }
    IUserAccountRepository UserAccounts { get; }
    
    Task<int> CompleteAsync();
    Task<TResult> ExecuteInTransactionAsync<TResult>(Func<Task<TResult>> operation);
}
