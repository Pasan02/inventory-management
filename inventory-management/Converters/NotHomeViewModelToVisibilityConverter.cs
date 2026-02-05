using System.Globalization;
using System.Windows;
using System.Windows.Data;
using inventory_management.ViewModels;

namespace inventory_management.Converters
{
    public class NotHomeViewModelToVisibilityConverter : IValueConverter
    {
        public static NotHomeViewModelToVisibilityConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is HomeViewModel or LoginViewModel || value == null ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
