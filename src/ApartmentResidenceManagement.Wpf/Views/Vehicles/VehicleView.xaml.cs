using System;
using System.Windows.Controls;
using System.Windows.Input;
using ApartmentResidenceManagement.Wpf.ViewModels;

namespace ApartmentResidenceManagement.Wpf.Views;

public partial class VehicleView : UserControl
{
    private DateTime _lastWheelTime = DateTime.MinValue;

    public VehicleView()
    {
        InitializeComponent();
    }

    private void OnVehiclesDataGridPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if ((DateTime.Now - _lastWheelTime).TotalMilliseconds < 80)
        {
            e.Handled = true;
            return;
        }
        _lastWheelTime = DateTime.Now;

        if (DataContext is VehicleViewModel vm)
        {
            if (e.Delta < 0)
            {
                // Lăn chuột xuống -> chuyển dòng bên dưới, hết trang tự động sang trang tiếp theo
                vm.SelectNextItemOrNextPage();
                if (vm.SelectedVehicleItem != null && VehiclesDataGrid != null)
                {
                    VehiclesDataGrid.ScrollIntoView(vm.SelectedVehicleItem);
                }
                e.Handled = true;
            }
            else if (e.Delta > 0)
            {
                // Lăn chuột lên -> chuyển dòng bên trên, hết trang tự động về trang trước
                vm.SelectPreviousItemOrPreviousPage();
                if (vm.SelectedVehicleItem != null && VehiclesDataGrid != null)
                {
                    VehiclesDataGrid.ScrollIntoView(vm.SelectedVehicleItem);
                }
                e.Handled = true;
            }
        }
    }

    private void OnDetailPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (sender is ScrollViewer scv)
        {
            scv.ScrollToVerticalOffset(scv.VerticalOffset - e.Delta);
            e.Handled = true;
        }
    }
}
