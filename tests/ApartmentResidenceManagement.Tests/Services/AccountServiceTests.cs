using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ApartmentResidenceManagement.Domain.Entities;
using ApartmentResidenceManagement.Domain.Enums;
using ApartmentResidenceManagement.Domain.Exceptions;
using ApartmentResidenceManagement.Domain.Interfaces;
using ApartmentResidenceManagement.Application.Security;
using ApartmentResidenceManagement.Application.Services;
using Moq;
using Xunit;

namespace ApartmentResidenceManagement.Tests.Services;

public class AccountServiceTests
{
    private readonly Mock<IUnitOfWork> _mockUow;
    private readonly Mock<IUserAccountRepository> _mockAccountRepo;
    private readonly Mock<IResidentRepository> _mockResidentRepo;
    private readonly Mock<IPasswordHasher> _mockHasher;
    private readonly AccountService _service;

    public AccountServiceTests()
    {
        _mockUow = new Mock<IUnitOfWork>();
        _mockAccountRepo = new Mock<IUserAccountRepository>();
        _mockResidentRepo = new Mock<IResidentRepository>();
        _mockHasher = new Mock<IPasswordHasher>();

        _mockUow.Setup(u => u.UserAccounts).Returns(_mockAccountRepo.Object);
        _mockUow.Setup(u => u.Residents).Returns(_mockResidentRepo.Object);

        _service = new AccountService(_mockUow.Object, _mockHasher.Object);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ShouldReturnAccount()
    {
        // Arrange
        string username = "testuser";
        string password = "CorrectPassword@123";
        var account = new UserAccount { Username = username, PasswordHash = "hashed_pass", IsActive = true };

        _mockAccountRepo.Setup(r => r.GetByUsernameAsync(username)).ReturnsAsync(account);
        _mockHasher.Setup(h => h.VerifyPassword(password, account.PasswordHash)).Returns(true);

        // Act
        var result = await _service.LoginAsync(username, password);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(username, result.Username);
    }

    [Fact]
    public async Task Login_WithIncorrectPassword_ShouldThrowBusinessRuleException()
    {
        // Arrange
        string username = "testuser";
        string password = "WrongPassword@123";
        var account = new UserAccount { Username = username, PasswordHash = "hashed_pass", IsActive = true };

        _mockAccountRepo.Setup(r => r.GetByUsernameAsync(username)).ReturnsAsync(account);
        _mockHasher.Setup(h => h.VerifyPassword(password, account.PasswordHash)).Returns(false);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => _service.LoginAsync(username, password));
        Assert.Contains("không chính xác", ex.Message);
    }

    [Fact]
    public async Task Login_AccountLocked_ShouldThrowBusinessRuleException()
    {
        // Arrange
        string username = "testuser";
        var account = new UserAccount { Username = username, IsActive = false, LockReason = "Chưa đóng phí quản lý" };

        _mockAccountRepo.Setup(r => r.GetByUsernameAsync(username)).ReturnsAsync(account);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => _service.LoginAsync(username, "Password@123"));
        Assert.Contains("đã bị khóa", ex.Message);
        Assert.Contains("Chưa đóng phí quản lý", ex.Message);
    }

    [Fact]
    public async Task ChangePassword_WithCorrectOldPassword_ShouldUpdatePassword()
    {
        // Arrange
        int accountId = 1;
        string oldPassword = "OldPassword@123";
        string newPassword = "NewPassword@456";
        var account = new UserAccount { Id = accountId, PasswordHash = "old_hashed_pass" };

        _mockAccountRepo.Setup(r => r.GetByIdAsync(accountId)).ReturnsAsync(account);
        _mockHasher.Setup(h => h.VerifyPassword(oldPassword, account.PasswordHash)).Returns(true);
        _mockHasher.Setup(h => h.HashPassword(newPassword)).Returns("new_hashed_pass");

        // Act
        await _service.ChangePasswordAsync(accountId, oldPassword, newPassword);

        // Assert
        Assert.Equal("new_hashed_pass", account.PasswordHash);
        _mockAccountRepo.Verify(r => r.Update(account), Times.Once);
        _mockUow.Verify(u => u.CompleteAsync(), Times.Once);
    }

    [Fact]
    public async Task ChangePassword_WithIncorrectOldPassword_ShouldThrowBusinessRuleException()
    {
        // Arrange
        int accountId = 1;
        string oldPassword = "WrongOldPassword@123";
        string newPassword = "NewPassword@456";
        var account = new UserAccount { Id = accountId, PasswordHash = "old_hashed_pass" };

        _mockAccountRepo.Setup(r => r.GetByIdAsync(accountId)).ReturnsAsync(account);
        _mockHasher.Setup(h => h.VerifyPassword(oldPassword, account.PasswordHash)).Returns(false);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => 
            _service.ChangePasswordAsync(accountId, oldPassword, newPassword));
        Assert.Contains("Mật khẩu cũ không chính xác", ex.Message);
    }

    [Fact]
    public async Task ChangePassword_WithShortNewPassword_ShouldThrowBusinessRuleException()
    {
        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => 
            _service.ChangePasswordAsync(1, "OldPass@123", "Short@1"));
        Assert.Contains("8 ký tự", ex.Message);
    }

    [Fact]
    public async Task CreateResidentAccount_ResidentAlreadyHasAccount_ShouldThrowBusinessRuleException()
    {
        // Arrange
        int residentId = 10;
        var resident = new Resident { Id = residentId, FullName = "Nguyen Van A" };
        var existingAccount = new UserAccount { ResidentId = residentId };

        _mockResidentRepo.Setup(r => r.GetByIdAsync(residentId)).ReturnsAsync(resident);
        _mockAccountRepo.Setup(r => r.GetByResidentIdAsync(residentId)).ReturnsAsync(existingAccount);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => 
            _service.CreateResidentAccountAsync(residentId, "newuser", "StrongPass@123"));

        Assert.Contains("đã có tài khoản đăng nhập", ex.Message);
    }

    [Fact]
    public async Task ToggleAccountStatus_AdminAccount_ShouldThrowBusinessRuleException()
    {
        // Arrange
        int accountId = 1;
        var adminAccount = new UserAccount { Id = accountId, Username = "admin", IsActive = true };

        _mockAccountRepo.Setup(r => r.GetByIdAsync(accountId)).ReturnsAsync(adminAccount);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => _service.ToggleAccountStatusAsync(accountId));
        Assert.Contains("Không được phép vô hiệu hóa tài khoản quản trị", ex.Message);
    }

    [Fact]
    public async Task ToggleAccountStatus_SoleActiveAdmin_ThrowsBusinessRuleException()
    {
        int accountId = 2;
        var adminAccount = new UserAccount { Id = accountId, Username = "admin_super", Role = UserRole.Admin, IsActive = true };

        _mockAccountRepo.Setup(r => r.GetByIdAsync(accountId)).ReturnsAsync(adminAccount);
        _mockAccountRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<UserAccount> { adminAccount });

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => _service.ToggleAccountStatusAsync(accountId));
        Assert.Contains("duy nhất của hệ thống", ex.Message);
    }

    [Fact]
    public async Task ToggleAccountStatus_WithLockReason_ShouldPersistReason()
    {
        int accountId = 5;
        var residentAccount = new UserAccount { Id = accountId, Username = "cudan_a", Role = UserRole.Resident, IsActive = true };

        _mockAccountRepo.Setup(r => r.GetByIdAsync(accountId)).ReturnsAsync(residentAccount);

        await _service.ToggleAccountStatusAsync(accountId, "Vi phạm nội quy chung cư");

        Assert.False(residentAccount.IsActive);
        Assert.Equal("Vi phạm nội quy chung cư", residentAccount.LockReason);
        _mockAccountRepo.Verify(r => r.Update(residentAccount), Times.Once);
        _mockUow.Verify(u => u.CompleteAsync(), Times.Once);
    }

    [Fact]
    public async Task Login_AccountDeactivated_ThrowsBusinessRuleException_Regression()
    {
        string username = "deactivated_user";
        var account = new UserAccount { Username = username, PasswordHash = "hashed_pass", IsActive = false };

        _mockAccountRepo.Setup(r => r.GetByUsernameAsync(username)).ReturnsAsync(account);

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => _service.LoginAsync(username, "Password@123"));
        Assert.Contains("đã bị khóa", ex.Message);
    }

    [Fact]
    public async Task Login_DatabaseConnectionFails_ThrowsBusinessRuleException()
    {
        // Arrange
        string username = "resident_a";
        _mockAccountRepo.Setup(r => r.GetByUsernameAsync(username))
            .ThrowsAsync(new System.Exception("Cannot connect to MySQL server"));

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => _service.LoginAsync(username, "Resident@123"));
        Assert.Contains("Không thể kết nối cơ sở dữ liệu", ex.Message);
    }

    [Fact]
    public async Task ResetPassword_WithValidPassword_ShouldUpdatePasswordHash()
    {
        // Arrange
        int accountId = 5;
        string newPassword = "NewPassword@123";
        var account = new UserAccount { Id = accountId, Username = "user5", PasswordHash = "old_hash" };

        _mockAccountRepo.Setup(r => r.GetByIdAsync(accountId)).ReturnsAsync(account);
        _mockHasher.Setup(h => h.HashPassword(newPassword)).Returns("new_hash_123");

        // Act
        await _service.ResetPasswordAsync(accountId, newPassword);

        // Assert
        Assert.Equal("new_hash_123", account.PasswordHash);
        _mockAccountRepo.Verify(r => r.Update(account), Times.Once);
        _mockUow.Verify(u => u.CompleteAsync(), Times.Once);
    }

    [Fact]
    public async Task ResetPassword_WithShortPassword_ShouldThrowBusinessRuleException()
    {
        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            _service.ResetPasswordAsync(1, "12345"));
        Assert.Contains("8 ký tự", ex.Message);
    }

    [Fact]
    public async Task CreateAccount_DuplicateUsername_ShouldThrowBusinessRuleException()
    {
        // Arrange
        string username = "existing_user";
        _mockAccountRepo.Setup(r => r.GetByUsernameAsync(username))
            .ReturnsAsync(new UserAccount { Username = username });

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            _service.CreateAccountAsync(username, "Admin@123", UserRole.Admin, null));
        Assert.Contains("đã được sử dụng", ex.Message);
    }

    #region Unit Tests cho Mô hình 2 (OTP Khôi phục 5 phút)
    [Fact]
    public async Task GenerateRecoveryOtpAsync_ValidAccount_ShouldGenerate6DigitOtpAnd5MinExpiry()
    {
        // Arrange
        int accountId = 10;
        var account = new UserAccount { Id = accountId, Username = "resident_10", IsActive = true };
        _mockAccountRepo.Setup(r => r.GetByIdAsync(accountId)).ReturnsAsync(account);

        // Act
        var result = await _service.GenerateRecoveryOtpAsync(accountId);

        // Assert
        Assert.NotNull(result.OtpCode);
        Assert.Equal(6, result.OtpCode.Length);
        Assert.True(int.TryParse(result.OtpCode, out _)); // Phải là 6 chữ số
        Assert.True(result.ExpiryTime > DateTime.Now.AddMinutes(4)); // Khoảng 5 phút
        Assert.True(result.ExpiryTime <= DateTime.Now.AddMinutes(5).AddSeconds(5));

        Assert.Equal(result.OtpCode, account.ResetOtpCode);
        Assert.Equal(result.ExpiryTime, account.ResetOtpExpiry);
        _mockAccountRepo.Verify(r => r.Update(account), Times.Once);
        _mockUow.Verify(u => u.CompleteAsync(), Times.Once);
    }

    [Fact]
    public async Task ResetPasswordWithOtpAsync_ValidOtpAndStrongPassword_ShouldResetPasswordAndClearOtp()
    {
        // Arrange
        string username = "resident_user";
        string otp = "123456";
        string newPass = "StrongPassword@2026";
        var account = new UserAccount
        {
            Id = 8,
            Username = username,
            ResetOtpCode = otp,
            ResetOtpExpiry = DateTime.Now.AddMinutes(4) // Còn hạn
        };

        _mockAccountRepo.Setup(r => r.GetByUsernameAsync(username)).ReturnsAsync(account);
        _mockHasher.Setup(h => h.HashPassword(newPass)).Returns("new_hash_strong");

        // Act
        await _service.ResetPasswordWithOtpAsync(username, otp, newPass);

        // Assert
        Assert.Equal("new_hash_strong", account.PasswordHash);
        Assert.Null(account.ResetOtpCode);
        Assert.Null(account.ResetOtpExpiry);
        _mockAccountRepo.Verify(r => r.Update(account), Times.Once);
        _mockUow.Verify(u => u.CompleteAsync(), Times.Once);
    }

    [Fact]
    public async Task ResetPasswordWithOtpAsync_IncorrectOtp_ShouldThrowBusinessRuleException()
    {
        // Arrange
        string username = "resident_user";
        var account = new UserAccount
        {
            Id = 8,
            Username = username,
            ResetOtpCode = "123456",
            ResetOtpExpiry = DateTime.Now.AddMinutes(4)
        };

        _mockAccountRepo.Setup(r => r.GetByUsernameAsync(username)).ReturnsAsync(account);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            _service.ResetPasswordWithOtpAsync(username, "999999", "StrongPassword@2026"));
        Assert.Contains("Mã xác thực OTP không chính xác", ex.Message);
    }

    [Fact]
    public async Task ResetPasswordWithOtpAsync_ExpiredOtp_ShouldThrowBusinessRuleException()
    {
        // Arrange
        string username = "resident_user";
        string otp = "123456";
        var account = new UserAccount
        {
            Id = 8,
            Username = username,
            ResetOtpCode = otp,
            ResetOtpExpiry = DateTime.Now.AddMinutes(-1) // Đã quá hạn 5 phút
        };

        _mockAccountRepo.Setup(r => r.GetByUsernameAsync(username)).ReturnsAsync(account);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            _service.ResetPasswordWithOtpAsync(username, otp, "StrongPassword@2026"));
        Assert.Contains("hết hạn", ex.Message);
    }

    [Fact]
    public async Task ResetPasswordWithOtpAsync_WeakPassword_ShouldThrowBusinessRuleException()
    {
        // Arrange
        string username = "resident_user";
        string otp = "123456";
        var account = new UserAccount
        {
            Id = 8,
            Username = username,
            ResetOtpCode = otp,
            ResetOtpExpiry = DateTime.Now.AddMinutes(4)
        };

        _mockAccountRepo.Setup(r => r.GetByUsernameAsync(username)).ReturnsAsync(account);

        // Act & Assert - Mật khẩu không có ký tự đặc biệt
        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            _service.ResetPasswordWithOtpAsync(username, otp, "WeakPassword123"));
        Assert.Contains("ký tự đặc biệt", ex.Message);
    }
    [Fact]
    public async Task SendRecoveryOtpByUsernameOrEmailAsync_ValidUsernameWithEmail_ShouldGenerateOtpAndSendEmail()
    {
        // Arrange
        string username = "resident_1";
        int residentId = 15;
        var account = new UserAccount { Id = 3, Username = username, ResidentId = residentId, IsActive = true };
        var resident = new Resident { Id = residentId, FullName = "Nguyen Van A", Email = "cudan1@gmail.com" };

        _mockAccountRepo.Setup(r => r.GetByUsernameAsync(username)).ReturnsAsync(account);
        _mockResidentRepo.Setup(r => r.GetByIdAsync(residentId)).ReturnsAsync(resident);

        // Act
        var (maskedEmail, otpCode) = await _service.SendRecoveryOtpByUsernameOrEmailAsync(username);

        // Assert
        Assert.Equal("c***1@gmail.com", maskedEmail);
        Assert.Equal(6, otpCode.Length);
        Assert.Equal(otpCode, account.ResetOtpCode);
        Assert.NotNull(account.ResetOtpExpiry);
        _mockAccountRepo.Verify(r => r.Update(account), Times.Once);
        _mockUow.Verify(u => u.CompleteAsync(), Times.Once);
    }

    [Fact]
    public async Task SendRecoveryOtpByUsernameOrEmailAsync_WithMockEmailService_ReturnsRealEmailSentStatus()
    {
        // Arrange
        string username = "resident_real_mail";
        int residentId = 18;
        var account = new UserAccount { Id = 8, Username = username, ResidentId = residentId, IsActive = true };
        var resident = new Resident { Id = residentId, FullName = "Le Thi Real", Email = "lethireal@gmail.com" };

        _mockAccountRepo.Setup(r => r.GetByUsernameAsync(username)).ReturnsAsync(account);
        _mockResidentRepo.Setup(r => r.GetByIdAsync(residentId)).ReturnsAsync(resident);

        var mockEmail = new Mock<IEmailService>();
        mockEmail.Setup(e => e.SendOtpEmailAsync("lethireal@gmail.com", "Le Thi Real", It.IsAny<string>(), 5))
                 .ReturnsAsync(true);

        var serviceWithEmail = new AccountService(_mockUow.Object, _mockHasher.Object, mockEmail.Object);

        // Act
        var (maskedEmail, otpCode, isRealEmailSent) = await serviceWithEmail.SendRecoveryOtpByUsernameOrEmailAsync(username);

        // Assert
        Assert.True(isRealEmailSent);
        Assert.Equal("l***l@gmail.com", maskedEmail);
        mockEmail.Verify(e => e.SendOtpEmailAsync("lethireal@gmail.com", "Le Thi Real", otpCode, 5), Times.Once);
    }

    [Fact]
    public async Task SendRecoveryOtpByUsernameOrEmailAsync_AccountHasNoEmail_ShouldThrowBusinessRuleException()
    {
        // Arrange
        string username = "resident_no_mail";
        int residentId = 16;
        var account = new UserAccount { Id = 4, Username = username, ResidentId = residentId, IsActive = true };
        var resident = new Resident { Id = residentId, FullName = "Nguyen Van B", Email = null };

        _mockAccountRepo.Setup(r => r.GetByUsernameAsync(username)).ReturnsAsync(account);
        _mockResidentRepo.Setup(r => r.GetByIdAsync(residentId)).ReturnsAsync(resident);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            _service.SendRecoveryOtpByUsernameOrEmailAsync(username));
        Assert.Contains("chưa có địa chỉ Email", ex.Message);
    }

    [Fact]
    public async Task SendRecoveryOtpByUsernameOrEmailAsync_AccountLocked_ShouldThrowBusinessRuleException()
    {
        // Arrange
        string username = "locked_user";
        int residentId = 17;
        var account = new UserAccount { Id = 5, Username = username, ResidentId = residentId, IsActive = false, LockReason = "Vi phạm" };

        _mockAccountRepo.Setup(r => r.GetByUsernameAsync(username)).ReturnsAsync(account);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            _service.SendRecoveryOtpByUsernameOrEmailAsync(username));
        Assert.Contains("đã bị khóa", ex.Message);
    }

    [Theory]
    [InlineData("nguyenvana@gmail.com", "n***a@gmail.com")]
    [InlineData("ab@gmail.com", "a***@gmail.com")]
    [InlineData("admin@phenikaa.edu.vn", "a***n@phenikaa.edu.vn")]
    public void MaskEmail_ShouldMaskCorrectly(string email, string expected)
    {
        string result = AccountService.MaskEmail(email);
        Assert.Equal(expected, result);
    }
    #endregion
}
