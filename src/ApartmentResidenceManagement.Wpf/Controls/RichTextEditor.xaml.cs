using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ApartmentResidenceManagement.Wpf.Helpers;
using Microsoft.Win32;

namespace ApartmentResidenceManagement.Wpf.Controls;

public partial class RichTextEditor : UserControl
{
    private bool _isUpdating;

    public static readonly DependencyProperty DocumentXamlProperty =
        DependencyProperty.Register(
            nameof(DocumentXaml),
            typeof(string),
            typeof(RichTextEditor),
            new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnDocumentXamlChanged));

    public string DocumentXaml
    {
        get => (string)GetValue(DocumentXamlProperty);
        set => SetValue(DocumentXamlProperty, value);
    }

    public RichTextEditor()
    {
        InitializeComponent();
        EditorBox.Document.PagePadding = new Thickness(0);
    }

    private static void OnDocumentXamlChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not RichTextEditor editor || editor._isUpdating)
            return;

        editor._isUpdating = true;
        try
        {
            var content = e.NewValue as string;
            FlowDocumentHelper.LoadContentIntoDocument(editor.EditorBox.Document, content);
        }
        finally
        {
            editor._isUpdating = false;
        }
    }

    private void EditorBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isUpdating) return;

        _isUpdating = true;
        try
        {
            // Kiểm tra xem document có rỗng hoàn toàn không
            var textRange = new TextRange(EditorBox.Document.ContentStart, EditorBox.Document.ContentEnd);
            if (string.IsNullOrWhiteSpace(textRange.Text) && !HasNonTextBlocks(EditorBox.Document))
            {
                DocumentXaml = string.Empty;
            }
            else
            {
                DocumentXaml = FlowDocumentHelper.SaveDocumentToXaml(EditorBox.Document);
            }
        }
        finally
        {
            _isUpdating = false;
        }
    }

    private static bool HasNonTextBlocks(FlowDocument doc)
    {
        foreach (var block in doc.Blocks)
        {
            if (block is Table || block is BlockUIContainer)
                return true;
        }
        return false;
    }

    private void FontSizeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (FontSizeCombo.SelectedItem is ComboBoxItem item &&
            item.Tag != null &&
            double.TryParse(item.Tag.ToString(), out double size))
        {
            if (EditorBox != null)
            {
                if (!EditorBox.Selection.IsEmpty)
                {
                    EditorBox.Selection.ApplyPropertyValue(TextElement.FontSizeProperty, size);
                }
                else
                {
                    EditorBox.FontSize = size;
                }
                EditorBox.Focus();
            }
        }
    }

    private void TextColorBtn_Click(object sender, RoutedEventArgs e)
    {
        ColorPickerPopup.PlacementTarget = TextColorBtn;
        ColorPickerPopup.IsOpen = !ColorPickerPopup.IsOpen;
    }

    private void ApplyColorSwatch_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string hex)
        {
            ApplyColor(hex);
        }
    }

    private void ApplyHexColor_Click(object sender, RoutedEventArgs e)
    {
        string hex = HexColorInput.Text?.Trim() ?? "#2563EB";
        if (!hex.StartsWith("#")) hex = "#" + hex;
        ApplyColor(hex);
    }

    private void ApplyColor(string hex)
    {
        try
        {
            var brush = (SolidColorBrush)new BrushConverter().ConvertFromString(hex)!;
            ColorBar.Background = brush;

            if (!EditorBox.Selection.IsEmpty)
            {
                EditorBox.Selection.ApplyPropertyValue(TextElement.ForegroundProperty, brush);
            }
            ColorPickerPopup.IsOpen = false;
            EditorBox.Focus();
        }
        catch
        {
            // Mã màu không hợp lệ
        }
    }

    private void InsertTableBtn_Click(object sender, RoutedEventArgs e)
    {
        var table = new Table
        {
            CellSpacing = 0,
            BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CBD5E1")),
            BorderThickness = new Thickness(1),
            Margin = new Thickness(0, 8, 0, 8)
        };

        table.Columns.Add(new TableColumn { Width = new GridLength(1.2, GridUnitType.Star) });
        table.Columns.Add(new TableColumn { Width = new GridLength(2.0, GridUnitType.Star) });

        var rowGroup = new TableRowGroup();

        // 1. Dòng tiêu đề bảng (Header)
        var headerRow = new TableRow { Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F1F5F9")) };
        headerRow.Cells.Add(CreateTableCell("Hạng mục / Nội dung", isHeader: true, hasRightBorder: true, hasBottomBorder: true));
        headerRow.Cells.Add(CreateTableCell("Chi tiết / Thời gian áp dụng", isHeader: true, hasRightBorder: false, hasBottomBorder: true));
        rowGroup.Rows.Add(headerRow);

        // 2. Các dòng dữ liệu mẫu (Data rows)
        var row1 = new TableRow();
        row1.Cells.Add(CreateTableCell("Thời gian thực hiện", isHeader: false, hasRightBorder: true, hasBottomBorder: true));
        row1.Cells.Add(CreateTableCell("08:30 - 11:30 ngày 15/11/2026", isHeader: false, hasRightBorder: false, hasBottomBorder: true));
        rowGroup.Rows.Add(row1);

        var row2 = new TableRow();
        row2.Cells.Add(CreateTableCell("Khu vực áp dụng", isHeader: false, hasRightBorder: true, hasBottomBorder: false));
        row2.Cells.Add(CreateTableCell("Thang máy Tháp A và Tháp B", isHeader: false, hasRightBorder: false, hasBottomBorder: false));
        rowGroup.Rows.Add(row2);

        table.RowGroups.Add(rowGroup);

        InsertBlockAtCaret(table);
    }

    private static TableCell CreateTableCell(string text, bool isHeader, bool hasRightBorder, bool hasBottomBorder)
    {
        var para = new Paragraph(isHeader ? new Bold(new Run(text)) : new Run(text))
        {
            Margin = new Thickness(0)
        };

        return new TableCell(para)
        {
            Padding = new Thickness(10, 6, 10, 6),
            BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CBD5E1")),
            BorderThickness = new Thickness(0, 0, hasRightBorder ? 1 : 0, hasBottomBorder ? 1 : 0)
        };
    }

    private void InsertImageBtn_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Chọn hình ảnh chèn vào thông báo",
            Filter = "Tệp hình ảnh (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp|Tất cả tệp (*.*)|*.*"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(dialog.FileName, UriKind.Absolute);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();

                var image = new Image
                {
                    Source = bitmap,
                    MaxWidth = 480,
                    MaxHeight = 280,
                    Stretch = Stretch.Uniform,
                    Margin = new Thickness(0, 6, 0, 6),
                    HorizontalAlignment = HorizontalAlignment.Left
                };

                var container = new BlockUIContainer(image);
                InsertBlockAtCaret(container);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không thể chèn hình ảnh: {ex.Message}", "Lỗi chèn ảnh", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }

    private void InsertBlockAtCaret(Block newBlock)
    {
        var caret = EditorBox.CaretPosition;
        var currentBlock = caret.Paragraph as Block ?? caret.GetInsertionPosition(LogicalDirection.Forward).Paragraph;

        if (currentBlock != null && EditorBox.Document.Blocks.Contains(currentBlock))
        {
            EditorBox.Document.Blocks.InsertAfter(currentBlock, newBlock);
            var nextPara = new Paragraph(new Run(""));
            EditorBox.Document.Blocks.InsertAfter(newBlock, nextPara);
            EditorBox.CaretPosition = nextPara.ContentStart;
        }
        else
        {
            EditorBox.Document.Blocks.Add(newBlock);
            var nextPara = new Paragraph(new Run(""));
            EditorBox.Document.Blocks.Add(nextPara);
            EditorBox.CaretPosition = nextPara.ContentStart;
        }

        EditorBox.Focus();
    }
}
