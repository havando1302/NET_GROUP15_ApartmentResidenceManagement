using System;
using System.Threading.Tasks;
using ApartmentResidenceManagement.Domain.Entities;
using ApartmentResidenceManagement.Domain.Enums;
using ApartmentResidenceManagement.Domain.Exceptions;
using ApartmentResidenceManagement.Domain.Interfaces;
using ApartmentResidenceManagement.Application.Security;

namespace ApartmentResidenceManagement.Application.Services;

public class AccountService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;

    public AccountService(IUnitOfWork unitOfWork, IPasswordHasher passwordHasher)
    {
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
    }

    // 1. ĐĂNG NHẬP
    public async Task<UserAccount?> LoginAsync(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            throw new BusinessRuleException("Tên đăng nhập và mật khẩu không được để trống.");
        }

        username = username.Trim();
        if (username.Length > 50)
        {
            throw new BusinessRuleException("Tên đăng nhập hoặc mật khẩu không chính xác.");
        }

        UserAccount? account;
        try
        {
            account = await _unitOfWork.UserAccounts.GetByUsernameAsync(username);
        }
        catch (BusinessRuleException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new BusinessRuleException("Không thể kết nối cơ sở dữ liệu. Vui lòng thử lại sau.", ex);
        }

        if (account == null)
        {
            throw new BusinessRuleException("Tên đăng nhập hoặc mật khẩu không chính xác.");
        }

        if (!account.IsActive)
        {
            throw new BusinessRuleException("Tài khoản này đã bị khóa. Vui lòng liên hệ Admin.");
        }

        bool isValid = false;
        try
        {
            isValid = _passwordHasher.VerifyPassword(password, account.PasswordHash);
        }
        catch
        {
            isValid = false;
        }

        if (!isValid)
        {
            throw new BusinessRuleException("Tên đăng nhập hoặc mật khẩu không chính xác.");
        }

        account.LastLoginAt = DateTime.Now;
        _unitOfWork.UserAccounts.Update(account);
        await _unitOfWork.CompleteAsync();

        return account;
    }

    // 2. CẤP TÀI KHOẢN CHO CƯ DÂN (Chỉ Admin thực hiện)
    public async Task<UserAccount> CreateResidentAccountAsync(int residentId, string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            throw new BusinessRuleException("Tên đăng nhập không được để trống.");
        }

        if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
        {
            throw new BusinessRuleException("Mật khẩu không được để trống và phải có ít nhất 6 ký tự.");
        }

        username = username.Trim();
        if (username.Length > 50)
        {
            throw new BusinessRuleException("Tên đăng nhập không được vượt quá 50 ký tự.");
        }

        var resident = await _unitOfWork.Residents.GetByIdAsync(residentId);
        if (resident == null)
        {
            throw new BusinessRuleException("Không tìm thấy thông tin cư dân.");
        }

        // Kiểm tra xem Resident đã có tài khoản chưa (Ràng buộc 1 - 0..1)
        var existingAccount = await _unitOfWork.UserAccounts.GetByResidentIdAsync(residentId);
        if (existingAccount != null)
        {
            throw new BusinessRuleException($"Cư dân '{resident.FullName}' đã có tài khoản đăng nhập trên hệ thống.");
        }

        // Kiểm tra Username trùng lặp
        var duplicateUsername = await _unitOfWork.UserAccounts.GetByUsernameAsync(username);
        if (duplicateUsername != null)
        {
            throw new BusinessRuleException($"Tên đăng nhập '{username}' đã được sử dụng. Vui lòng chọn tên khác.");
        }

        var account = new UserAccount
        {
            Username = username,
            PasswordHash = _passwordHasher.HashPassword(password),
            Role = UserRole.Resident,
            ResidentId = residentId,
            IsActive = true
        };

        await _unitOfWork.UserAccounts.AddAsync(account);
        await _unitOfWork.CompleteAsync();

        return account;
    }

    // 3. KHÓA / MỞ KHÓA TÀI KHOẢN
    public async Task ToggleAccountStatusAsync(int accountId)
    {
        var account = await _unitOfWork.UserAccounts.GetByIdAsync(accountId);
        if (account == null)
        {
            throw new BusinessRuleException("Không tìm thấy tài khoản cần thay đổi trạng thái.");
        }

        // Chặn tự khóa tài khoản admin mặc định (để tránh mất quyền quản trị)
        if (account.Username.ToLower() == "admin")
        {
            throw new BusinessRuleException("Không được phép vô hiệu hóa tài khoản quản trị viên mặc định.");
        }

        account.IsActive = !account.IsActive;
        _unitOfWork.UserAccounts.Update(account);
        await _unitOfWork.CompleteAsync();
    }

    // 4. ĐỔI MẬT KHẨU
    public async Task ChangePasswordAsync(int accountId, string oldPassword, string newPassword)
    {
        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
        {
            throw new BusinessRuleException("Mật khẩu mới phải có ít nhất 6 ký tự.");
        }

        var account = await _unitOfWork.UserAccounts.GetByIdAsync(accountId);
        if (account == null)
        {
            throw new BusinessRuleException("Không tìm thấy tài khoản.");
        }

        if (!_passwordHasher.VerifyPassword(oldPassword, account.PasswordHash))
        {
            throw new BusinessRuleException("Mật khẩu cũ không chính xác.");
        }

        account.PasswordHash = _passwordHasher.HashPassword(newPassword);
        _unitOfWork.UserAccounts.Update(account);
        await _unitOfWork.CompleteAsync();
    }

    // 5. LẤY TÀI KHOẢN THEO RESIDENT ID
    public async Task<UserAccount?> GetAccountByResidentIdAsync(int residentId)
    {
        return await _unitOfWork.UserAccounts.GetByResidentIdAsync(residentId);
    }

    // 6. LẤY TẤT CẢ TÀI KHOẢN KÈM THÔNG TIN CƯ DÂN
    public async Task<System.Collections.Generic.IEnumerable<UserAccount>> GetAllAccountsWithResidentAsync()
    {
        return await _unitOfWork.UserAccounts.GetAllWithResidentAsync();
    }

    // 7. ADMIN ĐẶT LẠI MẬT KHẨU
    public async Task ResetPasswordAsync(int accountId, string newPassword)
    {
        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
        {
            throw new BusinessRuleException("Mật khẩu mới phải có ít nhất 6 ký tự.");
        }

        var account = await _unitOfWork.UserAccounts.GetByIdAsync(accountId);
        if (account == null)
        {
            throw new BusinessRuleException("Không tìm thấy tài khoản cần đặt lại mật khẩu.");
        }

        account.PasswordHash = _passwordHasher.HashPassword(newPassword);
        _unitOfWork.UserAccounts.Update(account);
        await _unitOfWork.CompleteAsync();
    }

    // 8. TẠO TÀI KHOẢN TỔNG QUÁT (ADMIN HOẶC CƯ DÂN)
    public async Task<UserAccount> CreateAccountAsync(string username, string password, UserRole role, int? residentId)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            throw new BusinessRuleException("Tên đăng nhập không được để trống.");
        }

        if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
        {
            throw new BusinessRuleException("Mật khẩu không được để trống và phải có ít nhất 6 ký tự.");
        }

        username = username.Trim();
        if (username.Length > 50)
        {
            throw new BusinessRuleException("Tên đăng nhập không được vượt quá 50 ký tự.");
        }

        // Kiểm tra Username trùng lặp
        var duplicateUsername = await _unitOfWork.UserAccounts.GetByUsernameAsync(username);
        if (duplicateUsername != null)
        {
            throw new BusinessRuleException($"Tên đăng nhập '{username}' đã được sử dụng. Vui lòng chọn tên khác.");
        }

        if (role == UserRole.Resident)
        {
            if (!residentId.HasValue)
            {
                throw new BusinessRuleException("Vui lòng chọn cư dân liên kết với tài khoản này.");
            }

            var resident = await _unitOfWork.Residents.GetByIdAsync(residentId.Value);
            if (resident == null)
            {
                throw new BusinessRuleException("Không tìm thấy thông tin cư dân được chọn.");
            }

            var existingAccount = await _unitOfWork.UserAccounts.GetByResidentIdAsync(residentId.Value);
            if (existingAccount != null)
            {
                throw new BusinessRuleException($"Cư dân '{resident.FullName}' đã có tài khoản trên hệ thống.");
            }
        }

        var account = new UserAccount
        {
            Username = username,
            PasswordHash = _passwordHasher.HashPassword(password),
            Role = role,
            ResidentId = role == UserRole.Resident ? residentId : null,
            IsActive = true,
            CreatedAt = DateTime.Now
        };

        await _unitOfWork.UserAccounts.AddAsync(account);
        await _unitOfWork.CompleteAsync();

        return account;
    }
}
