using System.Windows;
using System.Windows.Controls;

namespace inventory_management.Views
{
    public partial class BarcodeManagementView : UserControl
    {
        public BarcodeManagementView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is ViewModels.IScannerAwareViewModel scannerAware)
            {
                scannerAware.ActivateScanner();
            }
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
