using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
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

    #region Quản lý Bảng tùy chỉnh (Insert & Delete Table như Word)
    private void TableBtn_Click(object sender, RoutedEventArgs e)
    {
        TablePickerPopup.PlacementTarget = TableBtn;
        TablePickerPopup.IsOpen = !TablePickerPopup.IsOpen;
    }

    private void CreateTable_Click(object sender, RoutedEventArgs e)
    {
        int rows = int.TryParse(TableRowsInput.Text?.Trim(), out int r) ? Math.Clamp(r, 1, 30) : 3;
        int cols = int.TryParse(TableColsInput.Text?.Trim(), out int c) ? Math.Clamp(c, 1, 10) : 3;
        bool hasHeader = HasHeaderRowCheck.IsChecked == true;

        var table = new Table
        {
            CellSpacing = 0,
            BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CBD5E1")),
            BorderThickness = new Thickness(1),
            Margin = new Thickness(0, 8, 0, 8)
        };

        for (int i = 0; i < cols; i++)
        {
            table.Columns.Add(new TableColumn { Width = new GridLength(1.0, GridUnitType.Star) });
        }

        var rowGroup = new TableRowGroup();

        for (int rowIndex = 0; rowIndex < rows; rowIndex++)
        {
            bool isHeader = hasHeader && rowIndex == 0;
            var row = new TableRow();
            if (isHeader)
            {
                row.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F1F5F9"));
            }

            for (int colIndex = 0; colIndex < cols; colIndex++)
            {
                bool hasRightBorder = colIndex < cols - 1;
                bool hasBottomBorder = rowIndex < rows - 1;
                row.Cells.Add(CreateTableCell(string.Empty, isHeader, hasRightBorder, hasBottomBorder));
            }
            rowGroup.Rows.Add(row);
        }

        table.RowGroups.Add(rowGroup);
        InsertBlockAtCaret(table);

        // Đưa con trỏ vào ô đầu tiên để người dùng bắt đầu gõ
        if (rowGroup.Rows.Count > 0 && rowGroup.Rows[0].Cells.Count > 0)
        {
            var firstCell = rowGroup.Rows[0].Cells[0];
            if (firstCell.Blocks.FirstBlock != null)
            {
                EditorBox.CaretPosition = firstCell.Blocks.FirstBlock.ContentStart;
            }
        }

        TablePickerPopup.IsOpen = false;
        EditorBox.Focus();
    }

    private void DeleteTable_Click(object sender, RoutedEventArgs e)
    {
        var table = GetParentTable(EditorBox.CaretPosition);
        if (table == null && !EditorBox.Selection.IsEmpty)
        {
            table = GetParentTable(EditorBox.Selection.Start) ?? GetParentTable(EditorBox.Selection.End);
        }

        if (table == null)
        {
            // Nếu không đứng trong bảng, tìm bảng gần nhất hoặc bảng cuối cùng
            table = EditorBox.Document.Blocks.OfType<Table>().LastOrDefault();
        }

        if (table != null)
        {
            var prevBlock = table.PreviousBlock;
            EditorBox.Document.Blocks.Remove(table);
            if (prevBlock != null)
            {
                EditorBox.CaretPosition = prevBlock.ContentEnd;
            }
            EditorBox_TextChanged(EditorBox, null!);
        }

        TablePickerPopup.IsOpen = false;
        EditorBox.Focus();
    }

    private static TableCell CreateTableCell(string text, bool isHeader, bool hasRightBorder, bool hasBottomBorder)
    {
        Inline inline = isHeader ? new Bold(new Run(text)) : new Run(text);
        var para = new Paragraph(inline)
        {
            Margin = new Thickness(0)
        };

        return new TableCell(para)
        {
            Padding = new Thickness(10, 8, 10, 8),
            BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CBD5E1")),
            BorderThickness = new Thickness(0, 0, hasRightBorder ? 1 : 0, hasBottomBorder ? 1 : 0)
        };
    }

    private void EditorBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Back)
        {
            // Trường hợp 1: Con trỏ ở đầu đoạn văn ngay sau một Table
            var caret = EditorBox.CaretPosition;
            var currentBlock = caret.Paragraph as Block;
            if (currentBlock != null && currentBlock.PreviousBlock is Table prevTable)
            {
                var textRange = new TextRange(currentBlock.ContentStart, currentBlock.ContentEnd);
                if (string.IsNullOrWhiteSpace(textRange.Text) || caret.CompareTo(currentBlock.ContentStart) == 0)
                {
                    EditorBox.Document.Blocks.Remove(prevTable);
                    e.Handled = true;
                    EditorBox_TextChanged(EditorBox, null!);
                    return;
                }
            }

            // Trường hợp 2: Con trỏ ở đầu ô đầu tiên của bảng
            var table = GetParentTable(caret);
            if (table != null)
            {
                var firstCell = table.RowGroups.FirstOrDefault()?.Rows.FirstOrDefault()?.Cells.FirstOrDefault();
                if (firstCell != null && caret.CompareTo(firstCell.ContentStart) <= 0)
                {
                    var prevBlock = table.PreviousBlock;
                    EditorBox.Document.Blocks.Remove(table);
                    if (prevBlock != null)
                    {
                        EditorBox.CaretPosition = prevBlock.ContentEnd;
                    }
                    e.Handled = true;
                    EditorBox_TextChanged(EditorBox, null!);
                    return;
                }
            }
        }
        else if (e.Key == Key.Delete)
        {
            // Trường hợp: Con trỏ ở cuối đoạn văn ngay trước một Table và bấm Delete
            var caret = EditorBox.CaretPosition;
            var currentBlock = caret.Paragraph as Block;
            if (currentBlock != null && currentBlock.NextBlock is Table nextTable)
            {
                if (caret.CompareTo(currentBlock.ContentEnd) >= 0)
                {
                    EditorBox.Document.Blocks.Remove(nextTable);
                    e.Handled = true;
                    EditorBox_TextChanged(EditorBox, null!);
                    return;
                }
            }
        }
    }

    private static Table? GetParentTable(TextPointer pointer)
    {
        DependencyObject? current = pointer.Parent;
        while (current != null)
        {
            if (current is Table table)
                return table;
            if (current is FrameworkContentElement fce)
                current = fce.Parent;
            else if (current is FrameworkElement fe)
                current = fe.Parent;
            else
                break;
        }
        return null;
    }
    #endregion

    #region Chèn Hình ảnh sắc nét (High Quality Image)
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
                bitmap.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
                bitmap.EndInit();
                bitmap.Freeze();

                var image = new Image
                {
                    Source = bitmap,
                    MaxWidth = 520,
                    Stretch = Stretch.Uniform,
                    Margin = new Thickness(0, 8, 0, 8),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    SnapsToDevicePixels = true,
                    UseLayoutRounding = true
                };

                // Áp dụng thuật toán scale ảnh chất lượng cao HighQuality, chống vỡ/răng cưa
                RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.HighQuality);
                RenderOptions.SetEdgeMode(image, EdgeMode.Aliased);

                var container = new BlockUIContainer(image);
                InsertBlockAtCaret(container);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không thể chèn hình ảnh: {ex.Message}", "Lỗi chèn ảnh", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
    #endregion

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
