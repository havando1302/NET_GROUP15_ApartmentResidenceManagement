using System;
using System.Globalization;
using System.Text;
using ApartmentResidenceManagement.Domain.Enums;

namespace ApartmentResidenceManagement.Application.Common;

public static class ResidentProfileHelper
{
    public static GenderType DetermineGender(string? fullName, RelationshipType relationship, string? specificRelationship = null)
    {
        string specific = (specificRelationship ?? string.Empty).Trim().ToLowerInvariant();
        if (!string.IsNullOrEmpty(specific))
        {
            if (specific.Contains("vợ") || specific.Contains("mẹ") || specific.Contains("bà") ||
                specific.Contains("con gái") || specific.Contains("chị") || specific.Contains("em gái") ||
                specific.Contains("cô") || specific.Contains("dì") || specific.Contains("thím") || specific.Contains("mợ"))
            {
                return GenderType.Female;
            }

            if (specific.Contains("chồng") || specific.Contains("bố") || specific.Contains("cha") || specific.Contains("ông") ||
                specific.Contains("con trai") || specific.Contains("anh") || specific.Contains("em trai") ||
                specific.Contains("chú") || specific.Contains("bác") || specific.Contains("dượng") || specific.Contains("cậu"))
            {
                return GenderType.Male;
            }
        }

        if (!string.IsNullOrWhiteSpace(fullName))
        {
            string name = fullName.Trim();
            if (name.Contains("Thị") || name.Contains("Thúy") || name.Contains("Lan") ||
                name.Contains("Hoa") || name.Contains("Hương") || name.Contains("Mai") ||
                name.Contains("Huyền") || name.Contains("Trang") || name.Contains("Linh") ||
                name.Contains("Nhung") || name.Contains("Ngọc") || name.Contains("Diệp") ||
                name.EndsWith(" B") || name.EndsWith(" Y"))
            {
                return GenderType.Female;
            }
        }

        return GenderType.Male;
    }

    public static string GetDefaultAvatar(string? fullName, GenderType gender, int seedId)
    {
        if (gender == GenderType.Female)
        {
            return (seedId % 2 == 0) ? "/Assets/avatar_daughter.png" : "/Assets/avatar_young_woman.png";
        }
        else
        {
            int mod = Math.Abs(seedId) % 3;
            if (mod == 0) return "/Assets/avatar_young_man.png";
            if (mod == 1) return "/Assets/avatar_toddler_boy.png";
            return "/Assets/avatar_son.png";
        }
    }

    public static string GenerateUsernameFromName(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName)) return "resident";

        string normalized = fullName.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (char c in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(c);
            if (category != UnicodeCategory.NonSpacingMark)
            {
                if (c == 'đ' || c == 'Đ') sb.Append('d');
                else if (char.IsLetterOrDigit(c)) sb.Append(char.ToLowerInvariant(c));
            }
        }
        string result = sb.ToString();
        return string.IsNullOrWhiteSpace(result) ? "resident" : result;
    }
}

