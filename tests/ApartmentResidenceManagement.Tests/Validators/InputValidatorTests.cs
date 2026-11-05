using ApartmentResidenceManagement.Application.Validators;
using System;
using Xunit;

namespace ApartmentResidenceManagement.Tests.Validators;

public class InputValidatorTests
{
    [Theory]
    [InlineData("0321234567")]
    [InlineData("0551234567")]
    [InlineData("0987654321")]
    public void ValidatePhone_ValidVietnameseMobileNumber_ReturnsTrue(string phone)
    {
        Assert.True(InputValidator.ValidatePhone(phone));
    }

    [Theory]
    [InlineData("0|21234567")]
    [InlineData("0212345678")]
    [InlineData("098765432")]
    [InlineData("09876543210")]
    public void ValidatePhone_InvalidNumber_ReturnsFalse(string phone)
    {
        Assert.False(InputValidator.ValidatePhone(phone));
    }

    [Fact]
    public void ValidateDateOfBirth_Today_ReturnsTrue()
    {
        Assert.True(InputValidator.ValidateDateOfBirth(DateTime.Today));
    }

    [Theory]
    [InlineData("Admin@123")]
    [InlineData("Resident#2026")]
    [InlineData("Phenikaa%9")]
    [InlineData("StrongPass!1")]
    public void ValidateStrongPassword_ValidStrongPassword_ReturnsTrue(string password)
    {
        Assert.True(InputValidator.ValidateStrongPassword(password));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Short@1")] // Dưới 8 ký tự
    [InlineData("alllowercase@123")] // Không có chữ in hoa
    [InlineData("ALLUPPERCASE#123")] // Không có chữ thường
    [InlineData("NoDigitAtAll@Pass")] // Không có chữ số
    [InlineData("NoSpecialChar123")] // Không có ký tự đặc biệt
    [InlineData("DollarSpecial$123")] // Ký tự $ không nằm trong tập [@, #, %, !]
    public void ValidateStrongPassword_InvalidPassword_ReturnsFalse(string? password)
    {
        Assert.False(InputValidator.ValidateStrongPassword(password));
    }
}
