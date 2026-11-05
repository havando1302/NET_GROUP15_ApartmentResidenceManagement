using ApartmentResidenceManagement.Application.Validators;
using System;
using System.Linq;
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
            string msg = !string.IsNullOrWhiteSpace(account.LockReason)
                ? $"Tài khoản này đã bị khóa. Lý do: {account.LockReason}. Vui lòng liên hệ Ban quản lý tòa nhà."
                : "Tài khoản này đã bị khóa. Vui lòng liên hệ Ban quản lý tòa nhà.";
            throw new BusinessRuleException(msg);
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

        if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
        {
            throw new BusinessRuleException("Mật khẩu không được để trống và phải có ít nhất 8 ký tự.");
        }

        if (!InputValidator.ValidateStrongPassword(password))
        {
            throw new BusinessRuleException("Mật khẩu phải có tối thiểu 8 ký tự, gồm ít nhất 1 chữ in hoa, 1 chữ thường, 1 chữ số và 1 ký tự đặc biệt (@, #, %, !).");
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
    public async Task ToggleAccountStatusAsync(int accountId, string? lockReason = null)
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

        // An toàn thông tin: Không cho phép vô hiệu hóa tài khoản admin hoạt động duy nhất
        if (account.Role == UserRole.Admin && account.IsActive)
        {
            var allAccounts = await _unitOfWork.UserAccounts.GetAllAsync();
            var activeAdmins = allAccounts.Count(u => u.Role == UserRole.Admin && u.IsActive && u.Id != account.Id);
            if (activeAdmins == 0)
            {
                throw new BusinessRuleException("Không được phép vô hiệu hóa tài khoản quản trị viên đang hoạt động duy nhất của hệ thống.");
            }
        }

        account.IsActive = !account.IsActive;
        if (!account.IsActive)
        {
            account.LockReason = string.IsNullOrWhiteSpace(lockReason) ? "Tạm khóa bởi Ban quản lý" : lockReason.Trim();
        }
        else
        {
            account.LockReason = null;
        }
        _unitOfWork.UserAccounts.Update(account);
        await _unitOfWork.CompleteAsync();
    }

    // 4. ĐỔI MẬT KHẨU
    public async Task ChangePasswordAsync(int accountId, string oldPassword, string newPassword)
    {
        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 8)
        {
            throw new BusinessRuleException("Mật khẩu mới phải có ít nhất 8 ký tự.");
        }

        if (!InputValidator.ValidateStrongPassword(newPassword))
        {
            throw new BusinessRuleException("Mật khẩu phải có tối thiểu 8 ký tự, gồm ít nhất 1 chữ in hoa, 1 chữ thường, 1 chữ số và 1 ký tự đặc biệt (@, #, %, !).");
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
        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 8)
        {
            throw new BusinessRuleException("Mật khẩu mới phải có ít nhất 8 ký tự.");
        }

        if (!InputValidator.ValidateStrongPassword(newPassword))
        {
            throw new BusinessRuleException("Mật khẩu phải có tối thiểu 8 ký tự, gồm ít nhất 1 chữ in hoa, 1 chữ thường, 1 chữ số và 1 ký tự đặc biệt (@, #, %, !).");
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
        else if (role == UserRole.Admin)
        {
            residentId = null;
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

    /// <summary>
    /// Ban quản lý cấp mã OTP khôi phục tài khoản (Recovery Code 6 số ngẫu nhiên, hiệu lực 5 phút).
    /// </summary>
    public async Task<(string OtpCode, DateTime ExpiryTime)> GenerateRecoveryOtpAsync(int accountId)
    {
        var account = await _unitOfWork.UserAccounts.GetByIdAsync(accountId);
        if (account == null)
        {
            throw new BusinessRuleException("Không tìm thấy tài khoản cần cấp mã khôi phục.");
        }

        // Sinh mã OTP 6 chữ số ngẫu nhiên
        string otpCode = Random.Shared.Next(100000, 999999).ToString();
        DateTime expiryTime = DateTime.Now.AddMinutes(5);

        account.ResetOtpCode = otpCode;
        account.ResetOtpExpiry = expiryTime;

        _unitOfWork.UserAccounts.Update(account);
        await _unitOfWork.CompleteAsync();

        return (otpCode, expiryTime);
    }

    /// <summary>
    /// Cư dân tự đặt lại mật khẩu mới bằng Mã xác thực OTP do Ban quản lý cấp (kiểm tra hạn 5 phút và mật khẩu mạnh).
    /// </summary>
    public async Task ResetPasswordWithOtpAsync(string username, string otpCode, string newPassword)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            throw new BusinessRuleException("Vui lòng nhập tên đăng nhập.");
        }

        if (string.IsNullOrWhiteSpace(otpCode))
        {
            throw new BusinessRuleException("Vui lòng nhập mã xác thực OTP (6 chữ số).");
        }

        username = username.Trim();
        otpCode = otpCode.Trim();

        var account = await _unitOfWork.UserAccounts.GetByUsernameAsync(username);
        if (account == null)
        {
            throw new BusinessRuleException("Tên đăng nhập hoặc mã xác thực OTP không chính xác.");
        }

        if (string.IsNullOrWhiteSpace(account.ResetOtpCode) || account.ResetOtpCode != otpCode)
        {
            throw new BusinessRuleException("Mã xác thực OTP không chính xác. Vui lòng kiểm tra lại mã được BQL cấp.");
        }

        if (!account.ResetOtpExpiry.HasValue || DateTime.Now > account.ResetOtpExpiry.Value)
        {
            throw new BusinessRuleException("Mã xác thực OTP đã hết hạn hiệu lực (chỉ có giá trị trong 5 phút). Vui lòng liên hệ Lễ tân tòa nhà để cấp mã mới.");
        }

        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 8)
        {
            throw new BusinessRuleException("Mật khẩu mới phải có tối thiểu 8 ký tự.");
        }

        if (!InputValidator.ValidateStrongPassword(newPassword))
        {
            throw new BusinessRuleException("Mật khẩu mới phải có tối thiểu 8 ký tự, gồm ít nhất 1 chữ in hoa, 1 chữ thường, 1 chữ số và 1 ký tự đặc biệt (@, #, %, !).");
        }

        account.PasswordHash = _passwordHasher.HashPassword(newPassword.Trim());
        // Sau khi đặt lại mật khẩu thành công, hủy mã OTP
        account.ResetOtpCode = null;
        account.ResetOtpExpiry = null;

        _unitOfWork.UserAccounts.Update(account);
        await _unitOfWork.CompleteAsync();
    }
}
