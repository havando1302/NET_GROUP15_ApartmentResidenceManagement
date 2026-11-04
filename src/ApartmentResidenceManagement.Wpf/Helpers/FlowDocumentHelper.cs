using System;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;

namespace ApartmentResidenceManagement.Wpf.Helpers;

public static class FlowDocumentHelper
{
    public static readonly DependencyProperty DocumentContentProperty =
        DependencyProperty.RegisterAttached(
            "DocumentContent",
            typeof(string),
            typeof(FlowDocumentHelper),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnDocumentContentChanged));

    public static string? GetDocumentContent(DependencyObject obj)
    {
        return (string?)obj.GetValue(DocumentContentProperty);
    }

    public static void SetDocumentContent(DependencyObject obj, string? value)
    {
        obj.SetValue(DocumentContentProperty, value);
    }

    private static void OnDocumentContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var content = e.NewValue as string;

        if (d is RichTextBox rtb)
        {
            if (rtb.Document == null)
            {
                rtb.Document = new FlowDocument();
            }
            LoadContentIntoDocument(rtb.Document, content);
        }
        else if (d is FlowDocumentScrollViewer viewer)
        {
            if (viewer.Document == null)
            {
                viewer.Document = new FlowDocument();
            }
            LoadContentIntoDocument(viewer.Document, content);
        }
    }

    public static void LoadContentIntoDocument(FlowDocument doc, string? content)
    {
        doc.Blocks.Clear();
        doc.PagePadding = new Thickness(0);

        if (string.IsNullOrWhiteSpace(content))
        {
            return;
        }

        string trimmed = content.TrimStart();
        if (trimmed.StartsWith("<FlowDocument", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("<Section", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                if (XamlReader.Parse(content) is FlowDocument parsedDoc)
                {
                    while (parsedDoc.Blocks.Count > 0)
                    {
                        var block = parsedDoc.Blocks.FirstBlock;
                        parsedDoc.Blocks.Remove(block);
                        doc.Blocks.Add(block);
                    }
                    return;
                }
            }
            catch
            {
                // Parse XAML thất bại -> chuyển sang parse text thường
            }
        }

        // Parse dạng markup cũ hoặc văn bản thuần
        try
        {
            var para = new Paragraph { Margin = new Thickness(0, 0, 0, 4) };
            FormattedTextHelper.ParseAndAddInlines(para.Inlines, content);
            doc.Blocks.Add(para);
        }
        catch
        {
            doc.Blocks.Add(new Paragraph(new Run(content)));
        }
    }

    public static string SaveDocumentToXaml(FlowDocument doc)
    {
        if (doc == null || doc.Blocks.Count == 0)
        {
            return string.Empty;
        }

        try
        {
            return XamlWriter.Save(doc);
        }
        catch
        {
            // Fallback sang TextRange
            try
            {
                var range = new TextRange(doc.ContentStart, doc.ContentEnd);
                using var ms = new MemoryStream();
                range.Save(ms, DataFormats.Xaml);
                return Encoding.UTF8.GetString(ms.ToArray());
            }
            catch
            {
                var range = new TextRange(doc.ContentStart, doc.ContentEnd);
                return range.Text;
            }
        }
    }
}
