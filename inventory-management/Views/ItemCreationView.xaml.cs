using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace inventory_management.Views
{
    public partial class ItemCreationView : UserControl
    {
        public ItemCreationView()
        {
            InitializeComponent();
        }

        // Prevent ComboBox from changing selection when scrolling with mouse wheel while closed
        private void ComboBox_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (sender is ComboBox comboBox && !comboBox.IsDropDownOpen)
            {
                e.Handled = true;
                
                // Pass the scroll event to the parent ScrollViewer
                var scrollViewer = FindParentScrollViewer(comboBox);
                if (scrollViewer != null)
                {
                    scrollViewer.RaiseEvent(new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
                    {
                        RoutedEvent = UIElement.MouseWheelEvent
                    });
                }
            }
        }

        private ScrollViewer? FindParentScrollViewer(System.Windows.DependencyObject child)
        {
            var parent = System.Windows.Media.VisualTreeHelper.GetParent(child);
            if (parent == null) return null;
            if (parent is ScrollViewer scrollViewer) return scrollViewer;
            return FindParentScrollViewer(parent);
        }
    }
}

