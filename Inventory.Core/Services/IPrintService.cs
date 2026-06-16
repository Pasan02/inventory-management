using System.Threading.Tasks;

namespace inventory_management.Services
{
    public interface IPrintService
    {
        /// <summary>
        /// Prints a barcode label to the detected Zebra printer or the fallback default printer.
        /// </summary>
        /// <param name="barcode">The barcode string to encode in the barcode graphic.</param>
        /// <param name="title">Main title printed above the barcode (e.g. Brand + Part Type).</param>
        /// <param name="details">Secondary information (e.g. Make + Model).</param>
        /// <param name="copies">Number of barcode labels to print.</param>
        /// <returns>True if printing was successful, false otherwise.</returns>
        Task<bool> PrintBarcodeLabelAsync(string barcode, int copies = 1);
    }
}
