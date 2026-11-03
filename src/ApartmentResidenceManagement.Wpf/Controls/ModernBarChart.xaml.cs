using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace ApartmentResidenceManagement.Wpf.Controls;

public class BarSeriesItem
{
    public string SeriesName { get; set; } = string.Empty;
    public double Value { get; set; }
    public Brush Brush { get; set; } = Brushes.DodgerBlue;
}

public class BarCategoryGroup
{
    public string CategoryLabel { get; set; } = string.Empty;
    public List<BarSeriesItem> Series { get; set; } = new();
}

public partial class ModernBarChart : UserControl
{
    public static readonly DependencyProperty CategoriesProperty =
        DependencyProperty.Register(nameof(Categories), typeof(IEnumerable<BarCategoryGroup>), typeof(ModernBarChart),
            new PropertyMetadata(null, OnDataChanged));

    public IEnumerable<BarCategoryGroup>? Categories
    {
        get => (IEnumerable<BarCategoryGroup>?)GetValue(CategoriesProperty);
        set => SetValue(CategoriesProperty, value);
    }

    public ModernBarChart()
    {
        InitializeComponent();
    }

    private static void OnDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ModernBarChart chart)
        {
            chart.DrawChart();
        }
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        DrawChart();
    }

    private void DrawChart()
    {
        ChartCanvas.Children.Clear();

        double width = ActualWidth > 0 ? ActualWidth : 420;
        double height = ActualHeight > 0 ? ActualHeight : 240;

        ChartCanvas.Width = width;
        ChartCanvas.Height = height;

        var catList = Categories?.ToList();
        if (catList == null || catList.Count == 0)
        {
            TxtEmpty.Visibility = Visibility.Visible;
            return;
        }

        double maxRaw = 0;
        foreach (var c in catList)
        {
            foreach (var s in c.Series)
            {
                if (s.Value > maxRaw) maxRaw = s.Value;
            }
        }

        // UI06: Nếu toàn bộ giá trị bằng 0
        if (maxRaw <= 0)
        {
            TxtEmpty.Visibility = Visibility.Visible;
            return;
        }

        TxtEmpty.Visibility = Visibility.Collapsed;

        // Xác định thang đo trục Y tròn trịa (0, 2, 4, 6... hoặc 5, 10, 15...)
        int step = maxRaw <= 6 ? 2 : (maxRaw <= 15 ? 3 : (maxRaw <= 30 ? 5 : 10));
        int maxAxis = (int)Math.Ceiling(maxRaw / step) * step;
        if (maxAxis < 4) maxAxis = 4;

        double leftPad = 28;
        double rightPad = 12;
        double topPad = 20;
        double bottomPad = 28;

        double plotWidth = width - leftPad - rightPad;
        double plotHeight = height - topPad - bottomPad;

        if (plotWidth <= 20 || plotHeight <= 20) return;

        // UI02: Vẽ các đường lưới ngang và nhãn trục Y
        int numLines = maxAxis / step;
        for (int i = 0; i <= numLines; i++)
        {
            double val = i * step;
            double y = topPad + plotHeight - (val / maxAxis) * plotHeight;

            // Đường gióng ngang
            var gridLine = new Line
            {
                X1 = leftPad,
                X2 = width - rightPad,
                Y1 = y,
                Y2 = y,
                Stroke = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
                StrokeThickness = 1
            };
            ChartCanvas.Children.Add(gridLine);

            // Số trục Y
            var txtVal = new TextBlock
            {
                Text = val.ToString("N0"),
                FontSize = 10.5,
                Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)),
                TextAlignment = TextAlignment.Right,
                Width = leftPad - 6
            };
            Canvas.SetLeft(txtVal, 0);
            Canvas.SetTop(txtVal, y - 7);
            ChartCanvas.Children.Add(txtVal);
        }

        // UI02: Vẽ các cột chữ nhật (Rectangle) cho từng category
        double catSlotWidth = plotWidth / catList.Count;

        for (int cIdx = 0; cIdx < catList.Count; cIdx++)
        {
            var cat = catList[cIdx];
            double slotX = leftPad + cIdx * catSlotWidth;

            // Nhãn Category trục X dưới chân cột
            var txtCat = new TextBlock
            {
                Text = cat.CategoryLabel,
                FontSize = 11,
                FontWeight = FontWeights.Medium,
                Foreground = new SolidColorBrush(Color.FromRgb(100, 116, 139)),
                TextAlignment = TextAlignment.Center,
                Width = catSlotWidth
            };
            Canvas.SetLeft(txtCat, slotX);
            Canvas.SetTop(txtCat, height - bottomPad + 6);
            ChartCanvas.Children.Add(txtCat);

            // Vẽ các cột thuộc category (1 cột hoặc cột đôi/cột ba)
            int seriesCount = cat.Series.Count;
            if (seriesCount == 0) continue;

            double barWidth = Math.Clamp((catSlotWidth - 14) / seriesCount, 8, 36);
            double totalBarsWidth = seriesCount * barWidth + (seriesCount - 1) * 3;
            double barsStartX = slotX + (catSlotWidth - totalBarsWidth) / 2.0;

            for (int sIdx = 0; sIdx < seriesCount; sIdx++)
            {
                var s = cat.Series[sIdx];
                double barX = barsStartX + sIdx * (barWidth + 3);

                // Chuẩn hóa chiều cao cột theo maxAxis
                double barHeight = (s.Value / maxAxis) * plotHeight;
                if (s.Value > 0 && barHeight < 4) barHeight = 4; // Tối thiểu 4px để nhìn thấy cột
                double barY = topPad + plotHeight - barHeight;

                // UI02: Vẽ Rectangle có góc bo tròn nhẹ ở đỉnh
                var rect = new Rectangle
                {
                    Width = barWidth,
                    Height = barHeight,
                    Fill = s.Brush,
                    RadiusX = 4,
                    RadiusY = 4,
                    Cursor = System.Windows.Input.Cursors.Hand
                };

                // UI07: ToolTip khi hover
                rect.ToolTip = new ToolTip
                {
                    Content = $"{cat.CategoryLabel}\n{s.SeriesName}: {s.Value:N0}",
                    FontSize = 12
                };

                Canvas.SetLeft(rect, barX);
                Canvas.SetTop(rect, barY);
                ChartCanvas.Children.Add(rect);

                // Hiển thị số lượng ngay trên đầu cột nếu Value > 0
                if (s.Value > 0)
                {
                    var txtBarVal = new TextBlock
                    {
                        Text = s.Value.ToString("N0"),
                        FontSize = 11,
                        FontWeight = FontWeights.SemiBold,
                        Foreground = new SolidColorBrush(Color.FromRgb(15, 23, 42)),
                        TextAlignment = TextAlignment.Center,
                        Width = barWidth + 8
                    };
                    Canvas.SetLeft(txtBarVal, barX - 4);
                    Canvas.SetTop(txtBarVal, Math.Max(2, barY - 16));
                    ChartCanvas.Children.Add(txtBarVal);
                }
            }
        }

        // UI04, UI05: Hoạt hình DoubleAnimation nhẹ nhàng trên Opacity khi tải (250ms)
        var anim = new DoubleAnimation(0.2, 1.0, TimeSpan.FromMilliseconds(250));
        ChartCanvas.BeginAnimation(UIElement.OpacityProperty, anim);
    }
}
