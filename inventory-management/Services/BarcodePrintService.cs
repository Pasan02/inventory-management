using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Printing;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace inventory_management.Services
{
    public class BarcodePrintService : IBarcodePrintService
    {
        private readonly IReadOnlyList<BarcodeLabelProfile> _profiles = new[]
        {
            new BarcodeLabelProfile
            {
                Name = "Sticker 50 x 30 mm",
                LabelWidthMm = 50,
                LabelHeightMm = 30,
                LeftMarginMm = 2,
                TopMarginMm = 2,
                BarcodeWidthMm = 44,
                BarcodeHeightMm = 14,
                TitleFontSize = 12,
                SubtitleFontSize = 9
            },
            new BarcodeLabelProfile
            {
                Name = "Sticker 58 x 40 mm",
                LabelWidthMm = 58,
                LabelHeightMm = 40,
                LeftMarginMm = 3,
                TopMarginMm = 3,
                BarcodeWidthMm = 50,
                BarcodeHeightMm = 18,
                TitleFontSize = 13,
                SubtitleFontSize = 10
            },
            new BarcodeLabelProfile
            {
                Name = "A4 Preview Label",
                LabelWidthMm = 90,
                LabelHeightMm = 45,
                LeftMarginMm = 5,
                TopMarginMm = 5,
                BarcodeWidthMm = 76,
                BarcodeHeightMm = 22,
                TitleFontSize = 14,
                SubtitleFontSize = 10
            }
        };

        public Task<IReadOnlyList<string>> GetInstalledPrintersAsync()
        {
            return Task.Run<IReadOnlyList<string>>(() =>
            {
                using var server = new LocalPrintServer();
                return server.GetPrintQueues()
                    .Select(static queue => queue.Name)
                    .OrderBy(static name => name, StringComparer.OrdinalIgnoreCase)
                    .ToList();
            });
        }

        public IReadOnlyList<BarcodeLabelProfile> GetLabelProfiles() => _profiles;

        public async Task PrintBarcodeAsync(
            string printerName,
            BarcodeLabelProfile profile,
            byte[] barcodeImage,
            string barcodeText,
            string title,
            string subtitle,
            int quantity)
        {
            if (string.IsNullOrWhiteSpace(printerName))
            {
                throw new InvalidOperationException("Printer selection is required.");
            }

            if (profile == null)
            {
                throw new InvalidOperationException("Label profile selection is required.");
            }

            if (barcodeImage == null || barcodeImage.Length == 0)
            {
                throw new InvalidOperationException("Barcode image is required.");
            }

            if (quantity <= 0)
            {
                throw new InvalidOperationException("Print quantity must be greater than zero.");
            }

            await Task.Run(() =>
            {
                using var server = new LocalPrintServer();
                var queue = server.GetPrintQueues()
                    .FirstOrDefault(q => string.Equals(q.Name, printerName, StringComparison.OrdinalIgnoreCase));

                if (queue == null)
                {
                    throw new InvalidOperationException($"Printer '{printerName}' was not found.");
                }

                var printDialog = new PrintDialog
                {
                    PrintQueue = queue
                };

                var document = new FixedDocument();
                var pageSize = new Size(MmToDip(profile.LabelWidthMm), MmToDip(profile.LabelHeightMm));
                document.DocumentPaginator.PageSize = pageSize;

                for (var index = 0; index < quantity; index++)
                {
                    document.Pages.Add(CreatePage(profile, pageSize, barcodeImage, barcodeText, title, subtitle));
                }

                printDialog.PrintDocument(document.DocumentPaginator, $"Barcode Labels - {barcodeText}");
            });
        }

        private static PageContent CreatePage(
            BarcodeLabelProfile profile,
            Size pageSize,
            byte[] barcodeImage,
            string barcodeText,
            string title,
            string subtitle)
        {
            var page = new FixedPage
            {
                Width = pageSize.Width,
                Height = pageSize.Height,
                Background = Brushes.White
            };

            var root = new Grid
            {
                Width = pageSize.Width,
                Height = pageSize.Height,
                Margin = new Thickness(MmToDip(profile.LeftMarginMm), MmToDip(profile.TopMarginMm), 0, 0)
            };

            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var titleBlock = new TextBlock
            {
                Text = title,
                FontSize = profile.TitleFontSize,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.Black,
                TextWrapping = TextWrapping.Wrap,
                Width = MmToDip(profile.BarcodeWidthMm)
            };

            var image = new Image
            {
                Width = MmToDip(profile.BarcodeWidthMm),
                Height = MmToDip(profile.BarcodeHeightMm),
                Stretch = Stretch.Fill,
                Margin = new Thickness(0, 3, 0, 3),
                Source = LoadBitmap(barcodeImage)
            };

            var barcodeBlock = new TextBlock
            {
                Text = barcodeText,
                FontSize = profile.SubtitleFontSize,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.Black,
                Width = MmToDip(profile.BarcodeWidthMm)
            };

            var subtitleBlock = new TextBlock
            {
                Text = subtitle,
                FontSize = Math.Max(8, profile.SubtitleFontSize - 1),
                Foreground = Brushes.Black,
                TextWrapping = TextWrapping.Wrap,
                Width = MmToDip(profile.BarcodeWidthMm)
            };

            Grid.SetRow(titleBlock, 0);
            Grid.SetRow(image, 1);
            Grid.SetRow(barcodeBlock, 2);
            Grid.SetRow(subtitleBlock, 3);

            root.Children.Add(titleBlock);
            root.Children.Add(image);
            root.Children.Add(barcodeBlock);
            root.Children.Add(subtitleBlock);

            page.Children.Add(root);

            var content = new PageContent();
            ((IAddChild)content).AddChild(page);
            return content;
        }

        private static BitmapImage LoadBitmap(byte[] bytes)
        {
            var image = new BitmapImage();
            using var stream = new MemoryStream(bytes);
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = stream;
            image.EndInit();
            image.Freeze();
            return image;
        }

        private static double MmToDip(double mm) => (mm / 25.4) * 96.0;
    }
}
