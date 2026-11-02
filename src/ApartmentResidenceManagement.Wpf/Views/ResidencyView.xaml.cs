using System;
using System.Windows.Controls;
using System.Windows.Input;
using ApartmentResidenceManagement.Wpf.ViewModels;

namespace ApartmentResidenceManagement.Wpf.Views;

public partial class ResidencyView : UserControl
{
    private DateTime _lastWheelTime = DateTime.MinValue;

    public ResidencyView()
    {
        InitializeComponent();
    }

    private void OnDataGridPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        // Điều tiết tốc độ lăn chuột để chuyển từng dòng mượt mà
        if ((DateTime.Now - _lastWheelTime).TotalMilliseconds < 80)
        {
            e.Handled = true;
            return;
        }
        _lastWheelTime = DateTime.Now;

        if (DataContext is ResidencyViewModel vm)
        {
            if (e.Delta < 0)
            {
                // Lăn chuột xuống -> Chuyển xuống dòng dưới, đến cuối trang tự sang trang tiếp theo
                vm.SelectNextItemOrNextPage();
                if (vm.SelectedResidenceItem != null)
                {
                    ResidencesDataGrid.ScrollIntoView(vm.SelectedResidenceItem);
                }
                e.Handled = true;
            }
            else if (e.Delta > 0)
            {
                // Lăn chuột lên -> Chuyển lên dòng trên, đến đầu trang tự về trang trước
                vm.SelectPreviousItemOrPreviousPage();
                if (vm.SelectedResidenceItem != null)
                {
                    ResidencesDataGrid.ScrollIntoView(vm.SelectedResidenceItem);
                }
                e.Handled = true;
            }
        }
    }

    private void OnTimelinePreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (sender is ScrollViewer sv)
        {
            // Lăn chuột cuộn Timeline mượt mà không cần hiện thanh cuộn
            sv.ScrollToVerticalOffset(sv.VerticalOffset - (e.Delta / 2.0));
            e.Handled = true;
        }
    }
}
