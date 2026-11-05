using System;
using System.Diagnostics;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using ApartmentResidenceManagement.Application.Services;
using Microsoft.Extensions.Configuration;

namespace ApartmentResidenceManagement.Infrastructure.Services;

public class SmtpEmailService : IEmailService
{
    private readonly IConfiguration _configuration;

    public SmtpEmailService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public bool IsConfigured
    {
        get
        {
            string username = _configuration["EmailSettings:Username"] ?? "";
            string password = _configuration["EmailSettings:Password"] ?? "";
            return !string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password);
        }
    }

    public async Task<bool> SendOtpEmailAsync(string toEmail, string recipientName, string otpCode, int expiryMinutes = 5)
    {
        if (string.IsNullOrWhiteSpace(toEmail))
        {
            return false;
        }

        // Nếu chưa cấu hình tài khoản SMTP thực tế (môi trường phát triển / thử nghiệm)
        if (!IsConfigured)
        {
            Debug.WriteLine($"[EMAIL SIMULATION] Sent OTP [{otpCode}] to {toEmail} for {recipientName} (SMTP credentials not configured in appsettings.json)");
            return false;
        }

        string host = _configuration["EmailSettings:SmtpServer"] ?? "smtp.gmail.com";
        int port = int.TryParse(_configuration["EmailSettings:Port"], out int p) ? p : 587;
        string senderEmail = _configuration["EmailSettings:SenderEmail"] ?? "residence.manager.bql@gmail.com";
        string senderName = _configuration["EmailSettings:SenderName"] ?? "Ban Quản Lý Chung Cư - ResidenceManager";
        string username = _configuration["EmailSettings:Username"] ?? "";
        string password = _configuration["EmailSettings:Password"] ?? "";
        bool enableSsl = bool.TryParse(_configuration["EmailSettings:EnableSsl"], out bool ssl) ? ssl : true;

        string subject = $"[{otpCode}] Mã xác thực OTP đặt lại mật khẩu - Chung cư ResidenceManager";
        string body = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'/>
    <style>
        body {{ font-family: 'Segoe UI', Arial, sans-serif; background-color: #f8fafc; margin: 0; padding: 24px; }}
        .card {{ max-width: 540px; margin: 0 auto; background: #ffffff; border-radius: 14px; padding: 32px; box-shadow: 0 4px 16px rgba(15, 23, 42, 0.08); border: 1px solid #e2e8f0; }}
        .header {{ text-align: center; margin-bottom: 24px; padding-bottom: 16px; border-bottom: 1px solid #f1f5f9; }}
        .title {{ font-size: 22px; font-weight: bold; color: #0f172a; margin: 8px 0 4px 0; }}
        .subtitle {{ font-size: 13px; color: #64748b; font-weight: 500; }}
        .greeting {{ font-size: 14.5px; color: #334155; margin-bottom: 16px; line-height: 1.6; }}
        .otp-box {{ background: #eff6ff; border: 2px dashed #3b82f6; border-radius: 10px; text-align: center; padding: 20px; margin: 24px 0; }}
        .otp-label {{ font-size: 12px; font-weight: bold; color: #1e40af; text-transform: uppercase; letter-spacing: 1px; margin-bottom: 8px; }}
        .otp-code {{ font-size: 36px; font-weight: 800; letter-spacing: 8px; color: #1d4ed8; font-family: Consolas, monospace; }}
        .expiry {{ font-size: 12.5px; color: #dc2626; margin-top: 8px; font-weight: 600; }}
        .notice {{ font-size: 12.5px; color: #64748b; line-height: 1.6; background: #fef3c7; border-left: 4px solid #f59e0b; padding: 12px 14px; border-radius: 6px; margin-top: 20px; }}
        .footer {{ text-align: center; font-size: 11.5px; color: #94a3b8; margin-top: 28px; border-top: 1px solid #f1f5f9; padding-top: 16px; }}
    </style>
</head>
<body>
    <div class='card'>
        <div class='header'>
            <div style='font-size: 34px;'>🏢</div>
            <div class='title'>ResidenceManager</div>
            <div class='subtitle'>Hệ Thống Quản Lý Cư Dân & Căn Hộ Chung Cư</div>
        </div>
        <div class='greeting'>
            Kính gửi cư dân <strong>{recipientName}</strong>,
        </div>
        <p style='font-size: 14px; color: #334155; line-height: 1.6; margin: 0 0 16px 0;'>
            Hệ thống nhận được yêu cầu đặt lại mật khẩu cho tài khoản cư dân của bạn. Dưới đây là <strong>mã xác thực OTP</strong> để hoàn tất quá trình này:
        </p>
        <div class='otp-box'>
            <div class='otp-label'>Mã OTP Khôi Phục Mật Khẩu</div>
            <div class='otp-code'>{otpCode}</div>
            <div class='expiry'>⏱ Hiệu lực trong {expiryMinutes} phút</div>
        </div>
        <div class='notice'>
            🔒 <strong>Lưu ý an toàn:</strong> Không chia sẻ mã xác thực này cho bất kỳ ai. Ban quản lý tòa nhà không bao giờ yêu cầu cung cấp mã OTP hoặc mật khẩu của bạn.<br/>
            Nếu bạn không yêu cầu đổi mật khẩu, vui lòng bỏ qua thư này hoặc thông báo ngay cho Ban quản lý.
        </div>
        <div class='footer'>
            Thư này được tạo tự động từ Cổng thông tin Cư dân ResidenceManager.<br/>
            © {DateTime.Now.Year} ResidenceManager. All rights reserved.
        </div>
    </div>
</body>
</html>";

        try
        {
            using var message = new MailMessage
            {
                From = new MailAddress(senderEmail, senderName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };
            message.To.Add(new MailAddress(toEmail, recipientName));

            using var client = new SmtpClient(host, port)
            {
                Credentials = new NetworkCredential(username, password),
                EnableSsl = enableSsl,
                Timeout = 10000
            };

            await client.SendMailAsync(message);
            Debug.WriteLine($"[EMAIL SUCCESS] Real email sent with OTP [{otpCode}] to {toEmail}");
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[EMAIL WARNING] Could not send via SMTP to {toEmail}: {ex.Message}");
            throw new InvalidOperationException($"Lỗi kết nối máy chủ gửi thư SMTP ({ex.Message}). Vui lòng kiểm tra lại thông tin tài khoản Gmail / Mật khẩu ứng dụng trong appsettings.json.", ex);
        }
    }
}
