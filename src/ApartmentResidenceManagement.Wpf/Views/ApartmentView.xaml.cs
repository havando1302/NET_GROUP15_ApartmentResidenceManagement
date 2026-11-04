using System.Windows.Controls;

namespace ApartmentResidenceManagement.Wpf.Views;

public partial class ApartmentView : UserControl
{
    public ApartmentView()
    {
        InitializeComponent();
    }

    private void OnDetailPreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
    {
        if (sender is ScrollViewer scv)
        {
            scv.ScrollToVerticalOffset(scv.VerticalOffset - e.Delta);
            e.Handled = true;
        }
    }
}
