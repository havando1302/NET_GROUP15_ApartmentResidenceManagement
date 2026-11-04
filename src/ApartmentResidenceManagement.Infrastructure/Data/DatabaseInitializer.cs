using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        bool isFemale = gender == GenderType.Female ||
                        (fullName != null && (
                            fullName.Contains("Thị") ||
                            fullName.Contains("Thúy") ||
                            fullName.Contains("Lan") ||
                            fullName.Contains("Hoa") ||
                            fullName.Contains("Hương") ||
                            fullName.Contains("Mai") ||
                            fullName.EndsWith(" B") ||
                            fullName.EndsWith(" Y")));

        if (isFemale)
        {
            return (id % 2 == 0) ? "/Assets/avatar_daughter.png" : "/Assets/avatar_young_woman.png";
        }
        else
        {
            if (id % 4 == 0) return "/Assets/avatar_young_man.png";
            if (id % 4 == 1) return "/Assets/avatar_toddler_boy.png";
            return "/Assets/avatar_son.png";
        }
    }

    private static string GenerateUsernameFromName(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName)) return "user";

        string normalized = fullName.Normalize(System.Text.NormalizationForm.FormD);
        var sb = new System.Text.StringBuilder();
        foreach (char c in normalized)
        {
            var category = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
            if (category != System.Globalization.UnicodeCategory.NonSpacingMark)
            {
                if (c == 'đ' || c == 'Đ') sb.Append('d');
                else if (char.IsLetterOrDigit(c)) sb.Append(char.ToLowerInvariant(c));
            }
        }
        string result = sb.ToString();
        return string.IsNullOrWhiteSpace(result) ? "user" : result;
    }
}
