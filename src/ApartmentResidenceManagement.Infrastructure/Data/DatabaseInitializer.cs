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
            HomeTown = "Hà Nội"
        };
        var residentB = new Resident
        {
            FullName = "Trần Thị B",
            DateOfBirth = new DateTime(1985, 2, 2),
            Gender = GenderType.Female,
            IdentityCard = "123456789013",
            PhoneNumber = "0901234568",
            HomeTown = "Nam Định"
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
                Username = "resident_a",
                PasswordHash = residentPasswordHash,
                Role = UserRole.Resident,
                ResidentId = residentA.Id,
                IsActive = true
            }
        };

        context.UserAccounts.AddRange(userAccounts);
        await context.SaveChangesAsync();
    }
}
