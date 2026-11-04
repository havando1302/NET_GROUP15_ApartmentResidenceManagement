using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace ApartmentResidenceManagement.Wpf.Controls;

public enum NotificationKind
{
    Error,
    Success,
    Warning,
    Information
}

public partial class NotificationBanner : UserControl
{
    public static readonly DependencyProperty MessageProperty = DependencyProperty.Register(
        nameof(Message), typeof(string), typeof(NotificationBanner), new PropertyMetadata(string.Empty, OnMessageChanged));

    public static readonly DependencyProperty KindProperty = DependencyProperty.Register(
        nameof(Kind), typeof(NotificationKind), typeof(NotificationBanner), new PropertyMetadata(NotificationKind.Error));

    public static readonly DependencyProperty DurationSecondsProperty = DependencyProperty.Register(
        nameof(DurationSeconds), typeof(double), typeof(NotificationBanner), new PropertyMetadata(3.0));

    public static readonly DependencyProperty IsAutoDismissEnabledProperty = DependencyProperty.Register(
        nameof(IsAutoDismissEnabled), typeof(bool), typeof(NotificationBanner), new PropertyMetadata(true));

    private readonly DispatcherTimer _autoDismissTimer;
    private bool _isDismissed;

    static NotificationBanner()
    {
        // Giữ cơ chế Coercion để khi Message rỗng hoặc đã hết thời gian 3s thì tự động ẩn Collapsed
        VisibilityProperty.OverrideMetadata(typeof(NotificationBanner), new FrameworkPropertyMetadata(
            Visibility.Visible, null, (element, value) =>
            {
                var banner = (NotificationBanner)element;
                if (banner._isDismissed || string.IsNullOrWhiteSpace(banner.Message))
                {
                    return Visibility.Collapsed;
                }
                return value;
            }));
    }

    public NotificationBanner()
    {
        InitializeComponent();

        _autoDismissTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(3.0)
        };
        _autoDismissTimer.Tick += OnAutoDismissTick;

        MouseEnter += OnNotificationMouseEnter;
        MouseLeave += OnNotificationMouseLeave;

        CoerceValue(VisibilityProperty);
    }

    public string Message
    {
        get => (string)GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    public NotificationKind Kind
    {
        get => (NotificationKind)GetValue(KindProperty);
        set => SetValue(KindProperty, value);
    }

    public double DurationSeconds
    {
        get => (double)GetValue(DurationSecondsProperty);
        set => SetValue(DurationSecondsProperty, value);
    }

    public bool IsAutoDismissEnabled
    {
        get => (bool)GetValue(IsAutoDismissEnabledProperty);
        set => SetValue(IsAutoDismissEnabledProperty, value);
    }

    private static void OnMessageChanged(DependencyObject element, DependencyPropertyChangedEventArgs e)
    {
        var banner = (NotificationBanner)element;
        banner._isDismissed = false;
        banner.CoerceValue(VisibilityProperty);
        banner.MessageScroller?.ScrollToTop();

        banner._autoDismissTimer.Stop();

        if (!string.IsNullOrWhiteSpace(banner.Message))
        {
            if (banner.IsAutoDismissEnabled)
            {
                double duration = banner.DurationSeconds > 0 ? banner.DurationSeconds : 3.0;
                banner._autoDismissTimer.Interval = TimeSpan.FromSeconds(duration);
                banner._autoDismissTimer.Start();
            }

            if (banner.Resources["FadeInStory"] is Storyboard fadeIn)
            {
                fadeIn.Begin(banner);
            }
        }
    }

    private void OnAutoDismissTick(object? sender, EventArgs e)
    {
        _autoDismissTimer.Stop();
        DismissWithAnimation();
    }

    public void DismissWithAnimation()
    {
        _autoDismissTimer.Stop();
        if (Resources["FadeOutStory"] is Storyboard fadeOut)
        {
            EventHandler onCompleted = null!;
            onCompleted = (s, e) =>
            {
                fadeOut.Completed -= onCompleted;
                Dismiss();
            };
            fadeOut.Completed += onCompleted;
            fadeOut.Begin(this);
        }
        else
        {
            Dismiss();
        }
    }

    public void Dismiss()
    {
        _autoDismissTimer.Stop();
        _isDismissed = true;
        CoerceValue(VisibilityProperty);
    }

    private void OnCloseButtonClick(object sender, RoutedEventArgs e)
    {
        DismissWithAnimation();
    }

    private void OnNotificationMouseEnter(object sender, MouseEventArgs e)
    {
        // Tạm dừng đếm ngược khi người dùng đang di chuột vào đọc thông báo
        _autoDismissTimer.Stop();
    }

    private void OnNotificationMouseLeave(object sender, MouseEventArgs e)
    {
        // Tiếp tục đếm ngược khi rời chuột (duy trì thêm 2s để người dùng kịp quan sát)
        if (!string.IsNullOrWhiteSpace(Message) && !_isDismissed && IsAutoDismissEnabled)
        {
            _autoDismissTimer.Stop();
            _autoDismissTimer.Interval = TimeSpan.FromSeconds(2.0);
            _autoDismissTimer.Start();
        }
    }
}
