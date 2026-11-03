using System;
using System.Windows.Controls;
using System.Windows.Input;
using ApartmentResidenceManagement.Wpf.ViewModels;

namespace ApartmentResidenceManagement.Wpf.Views;

public partial class AccountManagementView : UserControl
{
    private DateTime _lastWheelTime = DateTime.MinValue;

    public AccountManagementView()
    {
        InitializeComponent();
    }

    private void OnAccountsDataGridPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if ((DateTime.Now - _lastWheelTime).TotalMilliseconds < 80)
        {
            e.Handled = true;
            return;
        }
        _lastWheelTime = DateTime.Now;

        if (DataContext is AccountManagementViewModel vm)
        {
            if (e.Delta < 0)
            {
                // Lăn chuột xuống -> chuyển dòng bên dưới, hết trang tự động sang trang tiếp theo
                vm.SelectNextItemOrNextPage();
                if (vm.SelectedAccount != null)
                {
                    AccountsDataGrid.ScrollIntoView(vm.SelectedAccount);
                }
                e.Handled = true;
            }
            else if (e.Delta > 0)
            {
                // Lăn chuột lên -> chuyển dòng bên trên, hết trang tự động về trang trước
                vm.SelectPreviousItemOrPreviousPage();
                if (vm.SelectedAccount != null)
                {
                    AccountsDataGrid.ScrollIntoView(vm.SelectedAccount);
                }
                e.Handled = true;
            }
        }
    }

    private void OnAccountRowPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        // Bỏ qua nếu người dùng bấm trực tiếp vào Button thao tác (Xem, Đặt lại mật khẩu, Khóa/Mở khóa)
        if (e.OriginalSource is System.Windows.DependencyObject dep && FindVisualParent<Button>(dep) != null)
        {
            return;
        }

        if (sender is DataGridRow row && row.Item is AccountDisplayItemViewModel item)
        {
            if (DataContext is AccountManagementViewModel vm)
            {
                vm.SelectedAccount = item;
                vm.IsDetailPanelOpen = true;
            }
        }
    }

    private static T? FindVisualParent<T>(System.Windows.DependencyObject child) where T : System.Windows.DependencyObject
    {
        System.Windows.DependencyObject parentObject = System.Windows.Media.VisualTreeHelper.GetParent(child);
        if (parentObject == null) return null;
        if (parentObject is T parent) return parent;
        return FindVisualParent<T>(parentObject);
    }
}

