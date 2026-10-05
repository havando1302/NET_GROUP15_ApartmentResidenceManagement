using System;
using System.Threading.Tasks;
using ApartmentResidenceManagement.Domain.Exceptions;
using ApartmentResidenceManagement.Domain.Interfaces;
using ApartmentResidenceManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ApartmentResidenceManagement.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;
    private IResidentRepository? _residents;
    private IUserAccountRepository? _userAccounts;

    public UnitOfWork(AppDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public IResidentRepository Residents =>
        _residents ??= new ResidentRepository(_context);

    public IUserAccountRepository UserAccounts =>
        _userAccounts ??= new UserAccountRepository(_context);

    public async Task<int> CompleteAsync()
    {
        try
        {
            var affectedRows = await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();
            return affectedRows;
        }
        catch (DbUpdateException dbEx)
        {
            _context.ChangeTracker.Clear();
            var inner = dbEx.InnerException?.Message ?? string.Empty;

            if (inner.Contains("Duplicate entry", StringComparison.OrdinalIgnoreCase)
                || inner.Contains("duplicate key", StringComparison.OrdinalIgnoreCase)
                || inner.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase))
            {
                if (inner.Contains("Username", StringComparison.OrdinalIgnoreCase)
                    || inner.Contains("IX_UserAccounts_Username", StringComparison.OrdinalIgnoreCase))
                    throw new BusinessRuleException("Tên đăng nhập đã tồn tại trong hệ thống.", dbEx);

                throw new BusinessRuleException("Dữ liệu bị trùng lặp. Vui lòng kiểm tra lại thông tin đã nhập.", dbEx);
            }

            if (inner.Contains("foreign key", StringComparison.OrdinalIgnoreCase)
                || inner.Contains("FOREIGN KEY", StringComparison.OrdinalIgnoreCase))
                throw new BusinessRuleException("Không thể thực hiện vì dữ liệu đang được tham chiếu bởi bảng khác.", dbEx);

            throw new BusinessRuleException("Đã xảy ra lỗi khi lưu dữ liệu vào cơ sở dữ liệu. Vui lòng thử lại.", dbEx);
        }
    }

    public async Task<TResult> ExecuteInTransactionAsync<TResult>(Func<Task<TResult>> operation)
    {
        ArgumentNullException.ThrowIfNull(operation);

        var executionStrategy = _context.Database.CreateExecutionStrategy();
        return await executionStrategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var result = await operation();
                await transaction.CommitAsync();
                return result;
            }
            catch
            {
                await transaction.RollbackAsync();
                _context.ChangeTracker.Clear();
                throw;
            }
        });
    }

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }
}
