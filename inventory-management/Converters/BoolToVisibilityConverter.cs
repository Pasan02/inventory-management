using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace inventory_management.Converters
{
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isVisible)
            {
                if (parameter?.ToString() == "Inverse")
                {
                    return isVisible ? Visibility.Collapsed : Visibility.Visible;
                }
                return isVisible ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
            {
                bool isVisible = visibility == Visibility.Visible;
                if (parameter?.ToString() == "Inverse")
                {
                    return !isVisible;
                }
                return isVisible;
            }
            return false;
        }
    }
}
