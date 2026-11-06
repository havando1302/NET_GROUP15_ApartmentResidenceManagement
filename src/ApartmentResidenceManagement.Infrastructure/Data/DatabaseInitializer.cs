using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApartmentResidenceManagement.Application.Common;
using ApartmentResidenceManagement.Domain.Entities;
using ApartmentResidenceManagement.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ApartmentResidenceManagement.Infrastructure.Data;

public static class DatabaseInitializer
{
    public static async Task InitializeAsync(AppDbContext context, bool seedDemoData = true)
    {
        // Đảm bảo database đã được tạo và áp dụng migration mới nhất
        await context.Database.MigrateAsync();

        // Cập nhật giá trị mặc định cho cột RegisteredAtUtc nếu có bản ghi cũ đang null
        try
        {
            await context.Database.ExecuteSqlRawAsync(
                "UPDATE Vehicles SET RegisteredAtUtc = CURRENT_TIMESTAMP WHERE RegisteredAtUtc IS NULL;");
        }
        catch { }

        // Đảm bảo cột RejectReason tồn tại trong Vehicles (tránh throw exception duplicate column)
        try
        {
            var hasCol = await context.Database.SqlQueryRaw<int>(
                "SELECT COUNT(1) AS Value FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Vehicles' AND COLUMN_NAME = 'RejectReason';").FirstOrDefaultAsync();
            if (hasCol == 0)
            {
                await context.Database.ExecuteSqlRawAsync("ALTER TABLE Vehicles ADD COLUMN RejectReason varchar(500) NULL;");
            }
        }
        catch { }

        // Đảm bảo 2 bảng Notifications và NotificationRecipients tồn tại
        try
        {
            await context.Database.ExecuteSqlRawAsync(@"
                CREATE TABLE IF NOT EXISTS Notifications (
                    Id INT AUTO_INCREMENT PRIMARY KEY,
                    Title VARCHAR(250) NOT NULL,
                    Content TEXT NOT NULL,
                    Type INT NOT NULL,
                    Priority INT NOT NULL,
                    TargetAudience INT NOT NULL,
                    SenderId INT NULL,
                    RelatedEntityType VARCHAR(100) NULL,
                    RelatedEntityId INT NULL,
                    CreatedAt DATETIME NOT NULL,
                    CONSTRAINT FK_Notifications_SenderId FOREIGN KEY (SenderId) REFERENCES UserAccounts(Id) ON DELETE SET NULL
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
            ");

            await context.Database.ExecuteSqlRawAsync(@"
                CREATE TABLE IF NOT EXISTS NotificationRecipients (
                    Id INT AUTO_INCREMENT PRIMARY KEY,
                    NotificationId INT NOT NULL,
                    UserAccountId INT NOT NULL,
                    IsRead TINYINT(1) NOT NULL DEFAULT 0,
                    ReadAt DATETIME NULL,
                    CreatedAt DATETIME NOT NULL,
                    CONSTRAINT FK_NotificationRecipients_NotificationId FOREIGN KEY (NotificationId) REFERENCES Notifications(Id) ON DELETE CASCADE,
                    CONSTRAINT FK_NotificationRecipients_UserAccountId FOREIGN KEY (UserAccountId) REFERENCES UserAccounts(Id) ON DELETE CASCADE,
                    INDEX IX_NotificationRecipients_User_Read (UserAccountId, IsRead)
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
            ");
        }
        catch { }


        // Bổ sung AvatarPath cho các cư dân hiện có trong CSDL nếu đang null
        var residentsWithoutAvatar = await context.Residents
            .Where(r => r.AvatarPath == null || r.AvatarPath == "")
            .ToListAsync();
        if (residentsWithoutAvatar.Count > 0)
        {
            foreach (var r in residentsWithoutAvatar)
            {
                r.AvatarPath = GetDefaultAvatar(r.FullName, r.Gender, r.Id);
            }
            await context.SaveChangesAsync();
        }

        // Tự động cấp tài khoản cho các cư dân có thật trong CSDL chưa có tài khoản
        var allResidents = await context.Residents.Include(r => r.UserAccount).ToListAsync();
        var residentsWithoutAccount = allResidents.Where(r => r.UserAccount == null).ToList();

        if (residentsWithoutAccount.Count > 0)
        {
            string defaultResidentPasswordHash = BCrypt.Net.BCrypt.HashPassword("123456");
            var existingUsernames = new System.Collections.Generic.HashSet<string>(
                await context.UserAccounts.Select(u => u.Username).ToListAsync(),
                System.StringComparer.OrdinalIgnoreCase);

            int count = 0;
            foreach (var r in residentsWithoutAccount)
            {
                count++;
                string baseUsername = GenerateUsernameFromName(r.FullName);
                string username = baseUsername;
                int suffix = 1;
                while (existingUsernames.Contains(username))
                {
                    username = $"{baseUsername}{suffix++}";
                }
                existingUsernames.Add(username);

                // Mô phỏng 4 tài khoản bị khóa như ảnh mẫu (tài khoản thứ 3, 7, 18, 25)
                bool isActive = !(count == 3 || count == 7 || count == 18 || count == 25);
                var createdAt = new DateTime(2026, 1, 10).AddDays(r.Id % 15);
                DateTime? lastLoginAt = isActive ? new DateTime(2026, 1, 28, 9, 21, 0).AddHours(-(r.Id * 2 % 48)) : null;

                var account = new UserAccount
                {
                    Username = username,
                    PasswordHash = defaultResidentPasswordHash,
                    Role = UserRole.Resident,
                    ResidentId = r.Id,
                    IsActive = isActive,
                    CreatedAt = createdAt,
                    LastLoginAt = lastLoginAt
                };

                context.UserAccounts.Add(account);
            }
            await context.SaveChangesAsync();
        }

        // Bổ sung và đồng bộ dữ liệu phương tiện (Vehicles) 48 xe khớp 100% ảnh mẫu
        if (seedDemoData)
        {
            await SeedVehiclesIfMissingAsync(context);
            await SeedNotificationsIfMissingAsync(context);
        }

        if (!seedDemoData)
        {
            return;
        }

        // Chỉ seed dữ liệu tài khoản admin ban đầu khi bảng UserAccounts chưa có dữ liệu (lần đầu khởi động)
        if (await context.UserAccounts.AnyAsync())
        {
            return;
        }

        // 1. Tạo cư dân mẫu (Resident)
        var residentA = new Resident
        {
            FullName = "Nguyễn Văn A",
            DateOfBirth = new DateTime(1980, 1, 1),
            Gender = GenderType.Male,
            IdentityCard = "123456789012",
            PhoneNumber = "0901234567",
            HomeTown = "Hà Nội",
            AvatarPath = "/Assets/avatar_young_man.png"
        };
        var residentB = new Resident
        {
            FullName = "Trần Thị B",
            DateOfBirth = new DateTime(1985, 2, 2),
            Gender = GenderType.Female,
            IdentityCard = "123456789013",
            PhoneNumber = "0901234568",
            HomeTown = "Nam Định",
            AvatarPath = "/Assets/avatar_young_woman.png"
        };

        context.Residents.AddRange(residentA, residentB);
        await context.SaveChangesAsync();

        // 2. Tạo tài khoản mẫu (UserAccount)
        string adminPasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123");
        string residentPasswordHash = BCrypt.Net.BCrypt.HashPassword("resident123");

        var userAccounts = new[]
        {
            new UserAccount
            {
                Username = "admin",
                PasswordHash = adminPasswordHash,
                Role = UserRole.Admin,
                ResidentId = null,
                IsActive = true
            },
            new UserAccount
            {
                Username = "demo.admin",
                PasswordHash = adminPasswordHash,
                Role = UserRole.Admin,
                ResidentId = null,
                IsActive = true
            },
            new UserAccount
            {
                Username = "resident_a",
                PasswordHash = residentPasswordHash,
                Role = UserRole.Resident,
                ResidentId = residentA.Id,
                IsActive = true
            },
            new UserAccount
            {
                Username = "demo.resident",
                PasswordHash = residentPasswordHash,
                Role = UserRole.Resident,
                ResidentId = residentA.Id,
                IsActive = true
            }
        };

        context.UserAccounts.AddRange(userAccounts);
        await context.SaveChangesAsync();
    }

    private static async Task SeedVehiclesIfMissingAsync(AppDbContext context)
    {
        int existingVehicleCount = await context.Vehicles.CountAsync();
        if (existingVehicleCount >= 48)
        {
            return;
        }

        // Lấy danh sách cư dân & căn hộ sẵn có trong hệ thống
        var residents = await context.Residents.Include(r => r.ResidenceHistories).ThenInclude(rh => rh.Apartment).ToListAsync();
        var apartments = await context.Apartments.ToListAsync();

        // Danh sách mẫu 8 xe đầu tiên khớp chính xác 100% ảnh mẫu
        var seedSpecs = new (string Plate, VehicleType VType, string Brand, string Color, string OwnerName, string AptNum, VehicleRegistrationStatus Status, DateTime RegDate)[]
        {
            ("29A-123.45", VehicleType.Moto, "Honda", "Đen", "Nguyễn Văn A", "A-1203", VehicleRegistrationStatus.Pending, new DateTime(2026, 1, 10)),
            ("30G-456.78", VehicleType.Car, "Toyota", "Trắng", "Trần Thị B", "A-1008", VehicleRegistrationStatus.Approved, new DateTime(2026, 1, 8)),
            ("29B-789.12", VehicleType.Moto, "Yamaha", "Xanh", "Lê Văn C", "A-1102", VehicleRegistrationStatus.Rejected, new DateTime(2026, 1, 5)),
            ("29C-234.56", VehicleType.Moto, "Honda", "Đỏ", "Nguyễn Văn A", "A-1203", VehicleRegistrationStatus.Approved, new DateTime(2026, 1, 4)),
            ("30H-567.89", VehicleType.Car, "Hyundai", "Đen", "Phạm Thị F", "A-1101", VehicleRegistrationStatus.Approved, new DateTime(2026, 1, 3)),
            ("29D-678.90", VehicleType.Moto, "SYM", "Trắng", "Hoàng Văn G", "A-1202", VehicleRegistrationStatus.Pending, new DateTime(2026, 1, 2)),
            ("29E-111.22", VehicleType.Moto, "Honda", "Xanh", "Vũ Thị H", "A-1009", VehicleRegistrationStatus.Approved, new DateTime(2026, 1, 1)),
            ("30K-333.44", VehicleType.Car, "Mazda", "Xám", "Đỗ Văn K", "A-1204", VehicleRegistrationStatus.Pending, new DateTime(2026, 1, 1)),
            // Xe thứ 9: Ô tô của căn hộ A-1203 để hạn mức ô tô A-1203 đạt 1/1 xe như ảnh mẫu
            ("30A-999.88", VehicleType.Car, "Mercedes", "Đen", "Nguyễn Văn A", "A-1203", VehicleRegistrationStatus.Approved, new DateTime(2026, 1, 1))
        };

        var existingPlates = new HashSet<string>(await context.Vehicles.Select(v => v.LicensePlate).ToListAsync(), StringComparer.OrdinalIgnoreCase);

        // Helper lấy hoặc tạo căn hộ
        Apartment GetOrCreateApartment(string aptNumber)
        {
            var apt = apartments.FirstOrDefault(a => a.ApartmentNumber.Equals(aptNumber, StringComparison.OrdinalIgnoreCase));
            if (apt == null)
            {
                apt = new Apartment
                {
                    ApartmentNumber = aptNumber,
                    Floor = int.TryParse(aptNumber.Replace("A-", "").Substring(0, Math.Min(2, aptNumber.Length - 2)), out int fl) ? fl : 10,
                    Area = 75.5,
                    Status = ApartmentStatus.Occupied
                };
                context.Apartments.Add(apt);
                apartments.Add(apt);
            }
            return apt;
        }

        // Helper lấy hoặc tạo cư dân & gán căn hộ active
        Resident GetOrCreateResident(string fullName, string aptNumber)
        {
            var res = residents.FirstOrDefault(r => r.FullName.Equals(fullName, StringComparison.OrdinalIgnoreCase));
            if (res == null)
            {
                res = new Resident
                {
                    FullName = fullName,
                    DateOfBirth = new DateTime(1985, 1, 1),
                    Gender = fullName.Contains("Thị") ? GenderType.Female : GenderType.Male,
                    IdentityCard = "00108" + Math.Abs(fullName.GetHashCode() % 10000000).ToString("D7"),
                    PhoneNumber = "091" + Math.Abs(fullName.GetHashCode() % 10000000).ToString("D7"),
                    HomeTown = "Hà Nội",
                    AvatarPath = GetDefaultAvatar(fullName, fullName.Contains("Thị") ? GenderType.Female : GenderType.Male, 1)
                };
                context.Residents.Add(res);
                residents.Add(res);
            }

            var apt = GetOrCreateApartment(aptNumber);
            bool hasActiveHistory = res.ResidenceHistories.Any(rh => rh.ApartmentId == apt.Id && rh.IsActive);
            if (!hasActiveHistory)
            {
                var history = new ResidenceHistory
                {
                    Resident = res,
                    Apartment = apt,
                    StartDate = new DateTime(2024, 1, 1),
                    IsActive = true,
                    RelationshipType = RelationshipType.Owner
                };
                context.ResidenceHistories.Add(history);
                res.ResidenceHistories.Add(history);
            }

            return res;
        }

        // Tạo 9 xe đặc biệt đầu tiên
        foreach (var spec in seedSpecs)
        {
            if (existingPlates.Contains(spec.Plate))
            {
                var v = await context.Vehicles.FirstOrDefaultAsync(x => x.LicensePlate == spec.Plate);
                if (v != null)
                {
                    v.Color = spec.Color;
                    v.RegistrationDate = spec.RegDate;
                }
                continue;
            }

            var owner = GetOrCreateResident(spec.OwnerName, spec.AptNum);
            var veh = new Vehicle
            {
                LicensePlate = spec.Plate,
                VehicleType = spec.VType,
                Brand = spec.Brand,
                Color = spec.Color,
                Owner = owner,
                RegistrationStatus = spec.Status,
                RegistrationDate = spec.RegDate
            };
            context.Vehicles.Add(veh);
            existingPlates.Add(spec.Plate);
        }

        await context.SaveChangesAsync();

        // Tạo thêm các xe còn lại để đủ tổng 48 xe:
        // Đã có từ specs: 3 Pending (29A-123.45, 29D-678.90, 30K-333.44), 1 Rejected (29B-789.12), 5 Approved.
        // Cần thêm: 3 Pending (để đủ 6), 2 Rejected (để đủ 3), 34 Approved (để đủ 39 Approved) -> Tổng = 48 xe.
        int currentCount = await context.Vehicles.CountAsync();
        int pendingNeeded = 6 - await context.Vehicles.CountAsync(v => v.RegistrationStatus == VehicleRegistrationStatus.Pending);
        int rejectedNeeded = 3 - await context.Vehicles.CountAsync(v => v.RegistrationStatus == VehicleRegistrationStatus.Rejected);
        int totalNeeded = 48 - currentCount;

        string[] brands = { "Honda", "Yamaha", "Toyota", "Hyundai", "Kia", "Mazda", "VinFast", "Mercedes" };
        string[] colors = { "Đen", "Trắng", "Xanh", "Đỏ", "Xám", "Bạc" };

        for (int i = 1; i <= totalNeeded; i++)
        {
            string plate = $"29F-{(500 + i):D3}.{(i * 11 % 100):D2}";
            if (existingPlates.Contains(plate)) plate = $"30E-{(700 + i):D3}.{(i * 13 % 100):D2}";

            var status = VehicleRegistrationStatus.Approved;
            if (pendingNeeded > 0)
            {
                status = VehicleRegistrationStatus.Pending;
                pendingNeeded--;
            }
            else if (rejectedNeeded > 0)
            {
                status = VehicleRegistrationStatus.Rejected;
                rejectedNeeded--;
            }

            var resident = residents[(i - 1) % residents.Count];
            var vType = (i % 3 == 0) ? VehicleType.Car : VehicleType.Moto;

            var veh = new Vehicle
            {
                LicensePlate = plate,
                VehicleType = vType,
                Brand = brands[i % brands.Length],
                Color = colors[i % colors.Length],
                OwnerId = resident.Id,
                RegistrationStatus = status,
                RegistrationDate = new DateTime(2026, 1, 1).AddDays(-(i * 2))
            };
            context.Vehicles.Add(veh);
            existingPlates.Add(plate);
        }

        await context.SaveChangesAsync();
    }

    public static string GetDefaultAvatar(string? fullName, GenderType gender, int id)
    {
        return ResidentProfileHelper.GetDefaultAvatar(fullName, gender, id);
    }

    private static string GenerateUsernameFromName(string fullName)
    {
        return ResidentProfileHelper.GenerateUsernameFromName(fullName);
    }

    public static async Task SeedNotificationsIfMissingAsync(AppDbContext context)
    {
        if (await context.Notifications.AnyAsync()) return;

        var adminAccount = await context.UserAccounts.FirstOrDefaultAsync(u => u.Role == UserRole.Admin);
        var activeResidents = await context.Residents
            .Include(r => r.UserAccount)
            .Where(r => r.UserAccount != null && r.UserAccount.IsActive)
            .ToListAsync();

        var pendingVehicle = await context.Vehicles
            .Include(v => v.Owner)
            .FirstOrDefaultAsync(v => v.RegistrationStatus == VehicleRegistrationStatus.Pending);

        var pendingRequest = await context.MemberRegistrationRequests
            .Include(r => r.RequesterResident)
            .FirstOrDefaultAsync(r => r.Status == RequestStatus.Pending);

        var notifications = new List<Notification>();

        // 1. Thông báo đơn duyệt xe cho Admin
        if (pendingVehicle != null && adminAccount != null)
        {
            var notif = new Notification
            {
                Title = $"Đơn đăng ký phương tiện mới: {pendingVehicle.LicensePlate}",
                Content = $"Cư dân <b>{pendingVehicle.Owner.FullName}</b> đã gửi yêu cầu đăng ký phương tiện mới với biển số <color=#2563EB><b>{pendingVehicle.LicensePlate}</b></color> ({pendingVehicle.Brand} - {pendingVehicle.Color}). Vui lòng kiểm tra và phê duyệt.",
                Type = NotificationType.ApprovalRequest,
                Priority = NotificationPriority.Important,
                TargetAudience = TargetAudienceType.SystemAdmin,
                SenderId = pendingVehicle.Owner.UserAccount?.Id,
                RelatedEntityType = "Vehicle",
                RelatedEntityId = pendingVehicle.Id,
                CreatedAt = DateTime.Now.AddHours(-2)
            };
            notif.Recipients.Add(new NotificationRecipient
            {
                UserAccountId = adminAccount.Id,
                IsRead = false,
                CreatedAt = notif.CreatedAt
            });
            notifications.Add(notif);
        }

        // 2. Thông báo đơn duyệt thành viên cho Admin
        if (pendingRequest != null && adminAccount != null)
        {
            var notif = new Notification
            {
                Title = $"Yêu cầu thêm thành viên mới: {pendingRequest.FullName}",
                Content = $"Cư dân <b>{pendingRequest.RequesterResident.FullName}</b> gửi yêu cầu đăng ký thêm thành viên <color=#059669><b>{pendingRequest.FullName}</b></color> vào căn hộ. Vui lòng xem xét hồ sơ.",
                Type = NotificationType.ApprovalRequest,
                Priority = NotificationPriority.Normal,
                TargetAudience = TargetAudienceType.SystemAdmin,
                SenderId = pendingRequest.RequesterResident.UserAccount?.Id,
                RelatedEntityType = "MemberRegistrationRequest",
                RelatedEntityId = pendingRequest.Id,
                CreatedAt = DateTime.Now.AddHours(-5)
            };
            notif.Recipients.Add(new NotificationRecipient
            {
                UserAccountId = adminAccount.Id,
                IsRead = false,
                CreatedAt = notif.CreatedAt
            });
            notifications.Add(notif);
        }

        // 3. Thông báo chung từ Admin gửi toàn bộ cư dân có tài khoản đang hoạt động
        if (adminAccount != null && activeResidents.Count > 0)
        {
            var announcement = new Notification
            {
                Title = "Thông báo bảo trì hệ thống thang máy và tiện ích toà nhà",
                Content = "<b>KÍNH GỬI QUÝ CƯ DÂN TOÀ NHÀ RESIDENCEMANAGER</b><br/><br/>Ban quản lý xin trân trọng thông báo lịch bảo trì định kỳ hệ thống kỹ thuật toà nhà:<br/>• Thời gian bảo trì: <color=#2563EB><b>08:30 - 11:30 ngày 15/11/2026</b></color><br/>• Khu vực thực hiện: Hệ thống thang máy Tháp A và Tháp B.<br/>• Phạm vi ảnh hưởng: Tạm ngưng hoạt động thang máy số 01 và 02 sảnh chính.<br/><br/>Quý cư dân vui lòng sắp xếp thời gian di chuyển phù hợp. Mọi thắc mắc xin liên hệ Lễ tân qua hotline: <color=#DC2626><b>1900 6868</b></color>.<br/><br/>Trân trọng cảm ơn sự phối hợp của Quý cư dân!<br/><i>Ban Quản Lý Toà Nhà</i>",
                Type = NotificationType.Maintenance,
                Priority = NotificationPriority.Important,
                TargetAudience = TargetAudienceType.All,
                SenderId = adminAccount.Id,
                CreatedAt = DateTime.Now.AddDays(-1)
            };

            foreach (var r in activeResidents)
            {
                if (r.UserAccount != null)
                {
                    announcement.Recipients.Add(new NotificationRecipient
                    {
                        UserAccountId = r.UserAccount.Id,
                        IsRead = false,
                        CreatedAt = announcement.CreatedAt
                    });
                }
            }
            notifications.Add(announcement);
        }

        if (notifications.Count > 0)
        {
            context.Notifications.AddRange(notifications);
            await context.SaveChangesAsync();
        }
    }

}