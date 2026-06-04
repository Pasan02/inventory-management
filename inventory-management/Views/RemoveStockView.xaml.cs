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
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
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

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is ViewModels.IScannerAwareViewModel scannerAware)
            {
                scannerAware.ActivateScanner();
            }

            BarcodeInputTextBox.Focus();
            Keyboard.Focus(BarcodeInputTextBox);
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is ViewModels.IScannerAwareViewModel scannerAware)
            {
                scannerAware.DeactivateScanner();
            }
        }
    }
}

