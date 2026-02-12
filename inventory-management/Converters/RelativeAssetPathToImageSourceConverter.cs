using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using inventory_management.Services;

namespace inventory_management.Converters
{
    public class RelativeAssetPathToImageSourceConverter : IValueConverter
    {
        public static readonly RelativeAssetPathToImageSourceConverter Instance = new();

        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var relativePath = value as string;
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                return null;
            }

            var absolutePath = AssetPathService.GetAbsolutePath(relativePath);
            if (!File.Exists(absolutePath))
            {
                return null;
            }

            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.UriSource = new Uri(absolutePath, UriKind.Absolute);
            image.EndInit();
            image.Freeze();
            return image;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
