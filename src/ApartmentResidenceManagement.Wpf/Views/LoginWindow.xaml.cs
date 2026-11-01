using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using ApartmentResidenceManagement.Wpf.ViewModels;

namespace ApartmentResidenceManagement.Wpf.Views;

public partial class LoginWindow : Window
{
    public LoginWindow(LoginViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        // Đảm bảo nạp ảnh nền toà nhà chung cư tin cậy
        LoadHeroImage();

        // Đăng ký sự kiện đóng Window khi login thành công (được gọi từ ViewModel)
        viewModel.OnLoginSuccess += (sender, e) => {
            DialogResult = true;
            Close();
        };
    }

    private void LoadHeroImage()
    {
        try
        {
            // 1. Thử nạp từ thư mục Assets trong thư mục build (bin/Assets)
            string localAssetPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "login_background_hero.jpg");
            if (File.Exists(localAssetPath))
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(localAssetPath, UriKind.Absolute);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                imgBackground.Source = bitmap;
                return;
            }

            // 2. Thử nạp từ thư mục Assets gốc của dự án (khi đang chạy Visual Studio F5)
            string devAssetPath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Assets", "login_background_hero.jpg"));
            if (File.Exists(devAssetPath))
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(devAssetPath, UriKind.Absolute);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                imgBackground.Source = bitmap;
                return;
            }

            // 3. Fallback sang Pack URI resource đã được biên dịch
            var packUri = new Uri("pack://application:,,,/ApartmentResidenceManagement.Wpf;component/Assets/login_background_hero.jpg", UriKind.Absolute);
            var resBmp = new BitmapImage();
            resBmp.BeginInit();
            resBmp.UriSource = packUri;
            resBmp.CacheOption = BitmapCacheOption.OnLoad;
            resBmp.EndInit();
            imgBackground.Source = resBmp;
        }
        catch
        {
            // Bỏ qua lỗi fallback nhẹ nếu môi trường test không có màn hình
        }
    }

    private void Window_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
        {
            DragMove();
        }
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        System.Windows.Application.Current.Shutdown();
    }
}
