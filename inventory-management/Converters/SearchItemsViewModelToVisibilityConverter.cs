using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using inventory_management.ViewModels;

namespace inventory_management.Converters
{
    public class SearchItemsViewModelToVisibilityConverter : IValueConverter
    {
        public static readonly SearchItemsViewModelToVisibilityConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is SearchItemsViewModel ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
