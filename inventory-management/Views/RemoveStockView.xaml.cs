using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace inventory_management.Views
{
    public partial class RemoveStockView : UserControl
    {
        public RemoveStockView()
        {
            InitializeComponent();
            Loaded += (s, e) => BarcodeInputTextBox.Focus();
            DataContextChanged += (s, e) =>
            {
                if (e.OldValue is ViewModels.RemoveStockViewModel oldVm)
                {
                    oldVm.RequestFocus -= OnRequestFocus;
                    oldVm.ItemLoaded -= OnItemLoaded;
                }
                if (e.NewValue is ViewModels.RemoveStockViewModel newVm)
                {
                    newVm.RequestFocus += OnRequestFocus;
                    newVm.ItemLoaded += OnItemLoaded;
                }
            };
        }

        private void OnItemLoaded()
        {
            QuantityTextBox.Focus();
            QuantityTextBox.SelectAll();
        }

        private void OnRequestFocus()
        {
            BarcodeInputTextBox.Focus();
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

