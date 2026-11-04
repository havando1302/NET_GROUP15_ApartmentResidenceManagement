using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace ApartmentResidenceManagement.Wpf.Helpers;

public static class FormattedTextHelper
{
    public static readonly DependencyProperty FormattedTextProperty =
        DependencyProperty.RegisterAttached(
            "FormattedText",
            typeof(string),
            typeof(FormattedTextHelper),
            new PropertyMetadata(null, OnFormattedTextChanged));

    public static string GetFormattedText(DependencyObject obj)
    {
        return (string)obj.GetValue(FormattedTextProperty);
    }

    public static void SetFormattedText(DependencyObject obj, string value)
    {
        obj.SetValue(FormattedTextProperty, value);
    }

    private static void OnFormattedTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TextBlock textBlock)
            return;

        textBlock.Inlines.Clear();
        var rawText = e.NewValue as string;
        if (string.IsNullOrEmpty(rawText))
            return;

        try
        {
            ParseAndAddInlines(textBlock.Inlines, rawText);
        }
        catch
        {
            // Dự phòng an toàn nếu chuỗi markup có cú pháp đặc biệt: hiển thị dạng text thuần
            textBlock.Inlines.Clear();
            textBlock.Inlines.Add(new Run(rawText));
        }
    }

    private static void ParseAndAddInlines(InlineCollection inlines, string text)
    {
        // Chuẩn hóa line break
        text = text.Replace("\r\n", "\n").Replace("<br>", "\n").Replace("<br/>", "\n").Replace("<br />", "\n");

        // Biểu thức regex tìm các token: 
        // <b>...</b>, <i>...</i>, <u>...</u>, <color=...></color>, hoặc \n
        var pattern = @"(<b>.*?</b>|<i>.*?</i>|<u>.*?</u>|<color=#[0-9a-fA-F]{6}>.*?</color>|\n)";
        var parts = Regex.Split(text, pattern, RegexOptions.Singleline | RegexOptions.IgnoreCase);

        foreach (var part in parts)
        {
            if (string.IsNullOrEmpty(part))
                continue;

            if (part == "\n")
            {
                inlines.Add(new LineBreak());
            }
            else if (part.StartsWith("<b>", StringComparison.OrdinalIgnoreCase) && part.EndsWith("</b>", StringComparison.OrdinalIgnoreCase))
            {
                var inner = part.Substring(3, part.Length - 7);
                var bold = new Bold();
                AddFormattedInner(bold.Inlines, inner);
                inlines.Add(bold);
            }
            else if (part.StartsWith("<i>", StringComparison.OrdinalIgnoreCase) && part.EndsWith("</i>", StringComparison.OrdinalIgnoreCase))
            {
                var inner = part.Substring(3, part.Length - 7);
                var italic = new Italic();
                AddFormattedInner(italic.Inlines, inner);
                inlines.Add(italic);
            }
            else if (part.StartsWith("<u>", StringComparison.OrdinalIgnoreCase) && part.EndsWith("</u>", StringComparison.OrdinalIgnoreCase))
            {
                var inner = part.Substring(3, part.Length - 7);
                var underline = new Underline();
                AddFormattedInner(underline.Inlines, inner);
                inlines.Add(underline);
            }
            else if (part.StartsWith("<color=#", StringComparison.OrdinalIgnoreCase) && part.EndsWith("</color>", StringComparison.OrdinalIgnoreCase))
            {
                int closeTagIdx = part.IndexOf('>');
                if (closeTagIdx > 7)
                {
                    var colorCode = part.Substring(7, closeTagIdx - 7);
                    var inner = part.Substring(closeTagIdx + 1, part.Length - closeTagIdx - 9);
                    var span = new Span();
                    try
                    {
                        var brush = (SolidColorBrush)new BrushConverter().ConvertFromString(colorCode)!;
                        span.Foreground = brush;
                    }
                    catch { }
                    AddFormattedInner(span.Inlines, inner);
                    inlines.Add(span);
                }
                else
                {
                    inlines.Add(new Run(part));
                }
            }
            else
            {
                inlines.Add(new Run(part));
            }
        }
    }

    private static void AddFormattedInner(InlineCollection inlines, string inner)
    {
        // Xử lý các thẻ lồng nhau đơn giản nếu có
        if (inner.Contains("<b>") || inner.Contains("<i>") || inner.Contains("<u>") || inner.Contains("<color="))
        {
            ParseAndAddInlines(inlines, inner);
        }
        else
        {
            inlines.Add(new Run(inner));
        }
    }
}
