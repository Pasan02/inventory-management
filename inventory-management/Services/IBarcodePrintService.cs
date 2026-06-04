using System.Collections.Generic;
using System.Threading.Tasks;

namespace inventory_management.Services
{
    public sealed class BarcodeLabelProfile
    {
        public string Name { get; set; } = string.Empty;

        public double LabelWidthMm { get; set; }

        public double LabelHeightMm { get; set; }

        public double LeftMarginMm { get; set; }

        public double TopMarginMm { get; set; }

        public double BarcodeWidthMm { get; set; }

        public double BarcodeHeightMm { get; set; }

        public double TitleFontSize { get; set; }

        public double SubtitleFontSize { get; set; }

        public override string ToString() => Name;
    }

    public interface IBarcodePrintService
    {
        Task<IReadOnlyList<string>> GetInstalledPrintersAsync();

        IReadOnlyList<BarcodeLabelProfile> GetLabelProfiles();

        Task PrintBarcodeAsync(
            string printerName,
            BarcodeLabelProfile profile,
            byte[] barcodeImage,
            string barcodeText,
            string title,
            string subtitle,
            int quantity);
    }
}
