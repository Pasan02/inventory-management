using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ZXing;
using ZXing.Common;

namespace inventory_management.Services
{
    public class BarcodeService : IBarcodeService
    {
        public string GenerateBarcodeString(int itemId)
        {
            // Format ITM-00001234
            return $"ITM-{itemId:D8}";
        }

        public byte[] GenerateBarcodeImage(string barcodeText)
        {
            // Use BarcodeWriterPixelData to avoid System.Drawing dependencies
            // This returns raw pixel data which we can convert to a WPF BitmapSource
            var writer = new BarcodeWriterPixelData
            {
                Format = BarcodeFormat.CODE_128,
                Options = new EncodingOptions
                {
                    Height = 150,
                    Width = 400,
                    Margin = 10,
                    PureBarcode = false // distinct bars
                }
            };

            var pixelData = writer.Write(barcodeText);

            if (pixelData == null)
            {
                return new byte[] { };
            }

            // Create a WPF BitmapSource from the pixel data
            // ZXing PixelData is typically BGRA32 format
            var width = pixelData.Width;
            var height = pixelData.Height;
            var dpi = 96d;
            var stride = width * 4; // 4 bytes per pixel
            
            var bitmapSource = BitmapSource.Create(
                width,
                height,
                dpi,
                dpi,
                PixelFormats.Bgra32,
                null,
                pixelData.Pixels,
                stride
            );

            // Encode as PNG to memory stream
            using var stream = new MemoryStream();
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
            encoder.Save(stream);
            
            return stream.ToArray();
        }
    }
}
