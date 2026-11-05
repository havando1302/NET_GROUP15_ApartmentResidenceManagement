using System.Threading.Tasks;

namespace ApartmentResidenceManagement.Application.Services;

public record OtpSendResult(string MaskedEmail, string OtpCode, bool IsRealEmailSent, string? ErrorMessage = null)
{
    public void Deconstruct(out string maskedEmail, out string otpCode)
    {
        maskedEmail = MaskedEmail;
        otpCode = OtpCode;
    }

    public void Deconstruct(out string maskedEmail, out string otpCode, out bool isRealEmailSent)
    {
        maskedEmail = MaskedEmail;
        otpCode = OtpCode;
        isRealEmailSent = IsRealEmailSent;
    }
}

public interface IEmailService
{
    /// <summary>
    /// Kiểm tra xem thông tin tài khoản SMTP đã được cấu hình hợp lệ hay chưa.
    /// </summary>
    bool IsConfigured { get; }

    /// <summary>
    /// Gửi email chứa mã xác thực OTP khôi phục mật khẩu đến người nhận.
    /// </summary>
    /// <param name="toEmail">Địa chỉ email người nhận</param>
    /// <param name="recipientName">Họ tên người nhận</param>
    /// <param name="otpCode">Mã OTP 6 số</param>
    /// <param name="expiryMinutes">Thời gian hiệu lực (phút)</param>
    /// <returns>True nếu gửi thực tế thành công qua SMTP, False nếu ở chế độ mô phỏng hoặc thất bại</returns>
    Task<bool> SendOtpEmailAsync(string toEmail, string recipientName, string otpCode, int expiryMinutes = 5);
}
