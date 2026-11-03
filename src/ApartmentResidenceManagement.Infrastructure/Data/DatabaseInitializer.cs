using System;
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

        if (!seedDemoData)
        {
            return;
        }

        // Chỉ seed dữ liệu khi bảng UserAccounts chưa có dữ liệu (lần đầu khởi động)
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
        // Mật khẩu được băm bằng BCrypt
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
