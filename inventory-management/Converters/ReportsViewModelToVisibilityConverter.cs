using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using inventory_management.ViewModels;

namespace inventory_management.Converters
{
    public class ReportsViewModelToVisibilityConverter : IValueConverter
    {
        public static ReportsViewModelToVisibilityConverter Instance { get; } = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is ReportsViewModel ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
