using System.Globalization;
using System.Text;

namespace ApartmentResidenceManagement.Application.Common;

/// <summary>
/// Tiện ích hỗ trợ tìm kiếm tiếng Việt thông minh: không phân biệt hoa thường,
/// hỗ trợ gõ không dấu lẫn có dấu, và tìm kiếm đa từ khóa (multi-token search).
/// </summary>
public static class VietnameseSearchHelper
{
    /// <summary>
    /// Loại bỏ dấu tiếng Việt (ví dụ: "Nguyễn Văn Đô" -> "Nguyen Van Do").
    /// </summary>
    public static string RemoveDiacritics(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        string normalizedString = text.Normalize(NormalizationForm.FormD);
        var stringBuilder = new StringBuilder(capacity: normalizedString.Length);

        for (int i = 0; i < normalizedString.Length; i++)
        {
            char c = normalizedString[i];
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }

        return stringBuilder.ToString().Normalize(NormalizationForm.FormC)
            .Replace('đ', 'd')
            .Replace('Đ', 'D');
    }

    /// <summary>
    /// Kiểm tra xem chuỗi tìm kiếm keyword có khớp với danh sách các trường dữ liệu sources hay không.
    /// Hỗ trợ:
    /// - Không phân biệt hoa/thường (OrdinalIgnoreCase)
    /// - Không phân biệt có dấu / không dấu tiếng Việt
    /// - Đa từ khóa (mỗi từ cách nhau bởi khoảng trắng đều phải xuất hiện trong tập dữ liệu)
    /// </summary>
    public static bool MatchesSearch(string? keyword, params string?[] sources)
    {
        return MatchesSearch(keyword, (IEnumerable<string?>)sources);
    }

    /// <summary>
    /// Kiểm tra xem chuỗi tìm kiếm keyword có khớp với tập hợp các trường dữ liệu sources hay không.
    /// </summary>
    public static bool MatchesSearch(string? keyword, IEnumerable<string?> sources)
    {
        if (string.IsNullOrWhiteSpace(keyword)) return true;
        if (sources == null) return false;

        var tokens = keyword.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length == 0) return true;

        var combinedSource = string.Join(" ", sources.Where(s => !string.IsNullOrWhiteSpace(s)));
        if (string.IsNullOrWhiteSpace(combinedSource)) return false;

        string combinedNoDiacritics = RemoveDiacritics(combinedSource);

        foreach (var token in tokens)
        {
            string tokenNoDiacritics = RemoveDiacritics(token);
            bool match = combinedSource.Contains(token, StringComparison.OrdinalIgnoreCase)
                      || combinedNoDiacritics.Contains(tokenNoDiacritics, StringComparison.OrdinalIgnoreCase);

            if (!match) return false;
        }

        return true;
    }
}
