using System.Threading.Tasks;

namespace ApartmentResidenceManagement.Application.Services;

public interface IEmailService
{
    /// <summary>
    /// Gửi email chứa mã xác thực OTP khôi phục mật khẩu đến người nhận.
    /// </summary>
    /// <param name="toEmail">Địa chỉ email người nhận</param>
    /// <param name="recipientName">Họ tên người nhận</param>
    /// <param name="otpCode">Mã OTP 6 số</param>
    /// <param name="expiryMinutes">Thời gian hiệu lực (phút)</param>
    /// <returns>True nếu gửi thành công hoặc mô phỏng thành công, False nếu thất bại</returns>
    Task<bool> SendOtpEmailAsync(string toEmail, string recipientName, string otpCode, int expiryMinutes = 5);
}

