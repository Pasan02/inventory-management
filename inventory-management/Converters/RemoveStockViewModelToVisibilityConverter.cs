using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using inventory_management.ViewModels;

namespace inventory_management.Converters
{
    public class RemoveStockViewModelToVisibilityConverter : IValueConverter
    {
        public static RemoveStockViewModelToVisibilityConverter Instance { get; } = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is RemoveStockViewModel ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
