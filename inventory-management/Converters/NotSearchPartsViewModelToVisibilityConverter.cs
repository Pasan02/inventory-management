using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using inventory_management.ViewModels;
using inventory_management.ViewModels.Search;

namespace inventory_management.Converters
{
    public class NotSearchPartsViewModelToVisibilityConverter : IValueConverter
    {
        public static NotSearchPartsViewModelToVisibilityConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is SearchItemsViewModel searchVm)
            {
                // If current step is SearchPartsViewModel (the root), hide the button
                if (searchVm.CurrentStep is SearchPartsViewModel)
                {
                    return Visibility.Collapsed;
                }
                // Otherwise (Manufacturers, Models, Items), show it
                return Visibility.Visible;
            }
            // If main VM is not SearchItemsViewModel, hide it (handled by outer converter usually, but safe here)
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
