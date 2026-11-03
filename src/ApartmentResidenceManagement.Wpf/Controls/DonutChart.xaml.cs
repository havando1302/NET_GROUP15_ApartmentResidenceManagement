using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace ApartmentResidenceManagement.Wpf.Controls;

public class DonutSliceItem
{
    public string Title { get; set; } = string.Empty;
    public double Value { get; set; }
    public Brush Brush { get; set; } = Brushes.DodgerBlue;
}

public partial class DonutChart : UserControl
{
    public static readonly DependencyProperty SlicesProperty =
        DependencyProperty.Register(nameof(Slices), typeof(IEnumerable<DonutSliceItem>), typeof(DonutChart),
            new PropertyMetadata(null, OnDataChanged));

    public static readonly DependencyProperty CenterTextProperty =
        DependencyProperty.Register(nameof(CenterText), typeof(string), typeof(DonutChart),
            new PropertyMetadata(string.Empty, OnCenterTextChanged));

    public static readonly DependencyProperty CenterSubTextProperty =
        DependencyProperty.Register(nameof(CenterSubText), typeof(string), typeof(DonutChart),
            new PropertyMetadata(string.Empty, OnCenterSubTextChanged));

    public static readonly DependencyProperty HoleRatioProperty =
        DependencyProperty.Register(nameof(HoleRatio), typeof(double), typeof(DonutChart),
            new PropertyMetadata(0.70, OnDataChanged));

    public IEnumerable<DonutSliceItem>? Slices
    {
        get => (IEnumerable<DonutSliceItem>?)GetValue(SlicesProperty);
        set => SetValue(SlicesProperty, value);
    }

    public string CenterText
    {
        get => (string)GetValue(CenterTextProperty);
        set => SetValue(CenterTextProperty, value);
    }

    public string CenterSubText
    {
        get => (string)GetValue(CenterSubTextProperty);
        set => SetValue(CenterSubTextProperty, value);
    }

    public double HoleRatio
    {
        get => (double)GetValue(HoleRatioProperty);
        set => SetValue(HoleRatioProperty, value);
    }

    public DonutChart()
    {
        InitializeComponent();
    }

    private static void OnDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is DonutChart chart)
        {
            chart.DrawChart();
        }
    }

    private static void OnCenterTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is DonutChart chart)
        {
            chart.TxtCenterValue.Text = e.NewValue as string ?? string.Empty;
        }
    }

    private static void OnCenterSubTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is DonutChart chart)
        {
            chart.TxtCenterLabel.Text = e.NewValue as string ?? string.Empty;
        }
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        DrawChart();
    }

    private void DrawChart()
    {
        ChartCanvas.Children.Clear();

        double size = Math.Min(ActualWidth > 0 ? ActualWidth : 180, ActualHeight > 0 ? ActualHeight : 180);
        if (size <= 20) size = 180;

        ChartCanvas.Width = size;
        ChartCanvas.Height = size;

        double center = size / 2.0;
        double outerRadius = center - 6;
        double innerRadius = outerRadius * Math.Clamp(HoleRatio, 0.0, 0.9);

        var sliceList = Slices?.Where(s => s.Value > 0).ToList();
        double total = sliceList?.Sum(s => s.Value) ?? 0;

        // UI06: Khi tổng dữ liệu bằng 0, hiển thị "Không có dữ liệu"
        if (sliceList == null || sliceList.Count == 0 || total <= 0)
        {
            TxtEmpty.Visibility = Visibility.Visible;
            TxtCenterValue.Visibility = Visibility.Collapsed;
            TxtCenterLabel.Visibility = Visibility.Collapsed;

            var emptyRing = new Path
            {
                Data = CreatePieOrDonutGeometry(center, center, innerRadius, outerRadius, 0, 359.99),
                Fill = new SolidColorBrush(Color.FromArgb(30, 148, 163, 184)),
                IsHitTestVisible = false
            };
            ChartCanvas.Children.Add(emptyRing);
            return;
        }

        TxtEmpty.Visibility = Visibility.Collapsed;

        // Nếu là biểu đồ tròn đặc (HoleRatio <= 0.05), ẩn text ở giữa tâm
        if (HoleRatio <= 0.05)
        {
            TxtCenterValue.Visibility = Visibility.Collapsed;
            TxtCenterLabel.Visibility = Visibility.Collapsed;
        }
        else
        {
            TxtCenterValue.Visibility = Visibility.Visible;
            TxtCenterLabel.Visibility = Visibility.Visible;
            TxtCenterValue.Text = CenterText;
            TxtCenterLabel.Text = CenterSubText;
        }

        // UI01: Vẽ các hình quạt bằng PathGeometry và ArcSegment
        double currentAngle = -90.0; // Bắt đầu từ 12 giờ

        foreach (var slice in sliceList)
        {
            double sweepAngle = (slice.Value / total) * 360.0;
            if (sweepAngle <= 0) continue;

            if (sweepAngle >= 360.0) sweepAngle = 359.99;

            var geom = CreatePieOrDonutGeometry(center, center, innerRadius, outerRadius, currentAngle, sweepAngle);
            var path = new Path
            {
                Data = geom,
                Fill = slice.Brush,
                Stroke = Brushes.White,
                StrokeThickness = 2,
                Cursor = System.Windows.Input.Cursors.Hand
            };

            // UI07: ToolTip hiển thị tên nhóm, số lượng và tỷ lệ %
            double percent = (slice.Value / total) * 100.0;
            path.ToolTip = new ToolTip
            {
                Content = $"{slice.Title}: {slice.Value:N0} ({percent:N1}%)",
                FontSize = 12
            };

            ChartCanvas.Children.Add(path);
            currentAngle += sweepAngle;
        }

        // UI04, UI05: DoubleAnimation nhẹ nhàng trên Opacity khi tải biểu đồ (250ms)
        var anim = new DoubleAnimation(0.2, 1.0, TimeSpan.FromMilliseconds(250));
        ChartCanvas.BeginAnimation(UIElement.OpacityProperty, anim);
    }

    private static Geometry CreatePieOrDonutGeometry(
        double centerX, double centerY, double innerRadius, double outerRadius, double startAngleDeg, double sweepAngleDeg)
    {
        double endAngleDeg = startAngleDeg + sweepAngleDeg;

        Point p1 = DegreesToPoint(centerX, centerY, outerRadius, startAngleDeg);
        Point p2 = DegreesToPoint(centerX, centerY, outerRadius, endAngleDeg);

        bool isLargeArc = sweepAngleDeg > 180.0;

        var figure = new PathFigure
        {
            IsClosed = true,
            IsFilled = true
        };

        if (innerRadius <= 0.05)
        {
            // Biểu đồ tròn đặc (Pie Chart): Tâm -> p1 -> Cung ngoài -> Khép lại tâm
            figure.StartPoint = new Point(centerX, centerY);
            figure.Segments.Add(new LineSegment(p1, true));
            figure.Segments.Add(new ArcSegment(p2, new Size(outerRadius, outerRadius), 0, isLargeArc, SweepDirection.Clockwise, true));
        }
        else
        {
            Point p3 = DegreesToPoint(centerX, centerY, innerRadius, endAngleDeg);
            Point p4 = DegreesToPoint(centerX, centerY, innerRadius, startAngleDeg);

            figure.StartPoint = p1;
            figure.Segments.Add(new ArcSegment(p2, new Size(outerRadius, outerRadius), 0, isLargeArc, SweepDirection.Clockwise, true));
            figure.Segments.Add(new LineSegment(p3, true));
            figure.Segments.Add(new ArcSegment(p4, new Size(innerRadius, innerRadius), 0, isLargeArc, SweepDirection.Counterclockwise, true));
        }

        var geometry = new PathGeometry();
        geometry.Figures.Add(figure);
        return geometry;
    }

    private static Point DegreesToPoint(double centerX, double centerY, double radius, double angleDeg)
    {
        double rad = angleDeg * Math.PI / 180.0;
        return new Point(centerX + radius * Math.Cos(rad), centerY + radius * Math.Sin(rad));
    }
}
