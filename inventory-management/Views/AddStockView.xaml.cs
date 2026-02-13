using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace inventory_management.Views
{
    public partial class AddStockView : UserControl
    {
        public AddStockView()
        {
            InitializeComponent();
        }

        // Prevent ListBox from changing selection when scrolling with mouse wheel
        private void ListBox_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (sender is ListBox)
            {
                e.Handled = true;
                
                // Pass the scroll event to the parent to allow page scrolling
                var parent = System.Windows.Media.VisualTreeHelper.GetParent(sender as System.Windows.DependencyObject);
                if (parent != null)
                {
                    var mouseWheelEventArgs = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
                    {
                        RoutedEvent = UIElement.MouseWheelEvent
                    };
                    (parent as System.Windows.UIElement)?.RaiseEvent(mouseWheelEventArgs);
                }
            }
        }
    }
}

