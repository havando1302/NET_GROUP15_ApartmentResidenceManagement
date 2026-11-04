namespace ApartmentResidenceManagement.Domain.Enums;

public enum NotificationType
{
    General = 0,         // Thông báo chung
    ApprovalRequest = 1, // Đơn duyệt cần xử lý (gửi cho Admin)
    ApprovalResult = 2,  // Kết quả phê duyệt đơn (gửi cho Cư dân)
    Maintenance = 3,     // Bảo trì, kỹ thuật
    FeeNotice = 4,       // Nhắc nhở phí dịch vụ
    UrgentNotice = 5     // Thông báo khẩn cấp
}

public enum NotificationPriority
{
    Normal = 0,    // Bình thường
    Important = 1, // Quan trọng
    Urgent = 2     // Khẩn cấp
}

public enum TargetAudienceType
{
    All = 0,               // Toàn bộ cư dân có tài khoản đang hoạt động
    ActiveResidents = 1,   // Nhóm cư dân đang cư trú
    MovedOutResidents = 2, // Nhóm cư dân đã chuyển đi
    SpecificResidents = 3, // Cư dân cụ thể được chọn
    SystemAdmin = 4        // Ban quản lý / Quản trị viên
}
