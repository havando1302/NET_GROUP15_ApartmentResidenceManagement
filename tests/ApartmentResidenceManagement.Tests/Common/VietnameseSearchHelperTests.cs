using ApartmentResidenceManagement.Application.Common;
using Xunit;

namespace ApartmentResidenceManagement.Tests.Common;

public class VietnameseSearchHelperTests
{
    [Theory]
    [InlineData("Nguyễn Văn A", "Nguyen Van A")]
    [InlineData("Đặng Văn Đô", "Dang Van Do")]
    [InlineData("Lê Thị Ơn", "Le Thi On")]
    [InlineData("", "")]
    [InlineData(null, "")]
    public void RemoveDiacritics_ShouldRemoveVietnameseAccentsProperly(string? input, string expected)
    {
        string result = VietnameseSearchHelper.RemoveDiacritics(input);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void MatchesSearch_NullOrEmptyKeyword_ShouldReturnTrue()
    {
        Assert.True(VietnameseSearchHelper.MatchesSearch(null, "Nguyễn Văn A"));
        Assert.True(VietnameseSearchHelper.MatchesSearch("", "Nguyễn Văn A"));
        Assert.True(VietnameseSearchHelper.MatchesSearch("   ", "Nguyễn Văn A"));
    }

    [Fact]
    public void MatchesSearch_MatchingWithAccents_ShouldReturnTrue()
    {
        Assert.True(VietnameseSearchHelper.MatchesSearch("Nguyễn", "Nguyễn Văn A"));
        Assert.True(VietnameseSearchHelper.MatchesSearch("nguyễn", "Nguyễn Văn A"));
    }

    [Fact]
    public void MatchesSearch_MatchingWithoutAccents_ShouldReturnTrue()
    {
        Assert.True(VietnameseSearchHelper.MatchesSearch("nguyen van", "Nguyễn Văn A"));
        Assert.True(VietnameseSearchHelper.MatchesSearch("dang do", "Đặng Văn Đô"));
        Assert.True(VietnameseSearchHelper.MatchesSearch("bui minh", "Bùi Văn Minh"));
    }

    [Fact]
    public void MatchesSearch_MultiFieldAndTokens_ShouldMatchAcrossFields()
    {
        string fullName = "Bùi Văn Minh";
        string apartment = "1002";
        string code = "CD012";

        // Tìm tên kèm số phòng
        Assert.True(VietnameseSearchHelper.MatchesSearch("minh 1002", fullName, apartment, code));
        // Tìm không dấu kèm mã
        Assert.True(VietnameseSearchHelper.MatchesSearch("bui cd012", fullName, apartment, code));
        // Không khớp nếu từ khóa sai
        Assert.False(VietnameseSearchHelper.MatchesSearch("minh 9999", fullName, apartment, code));
    }
}
