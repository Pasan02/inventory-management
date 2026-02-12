using System.Drawing;
using System.IO;

namespace inventory_management.Services
{
    public interface IBarcodeService
    {
        /// <summary>
        /// Generates a standardized barcode string from an Item ID.
        /// e.g. 1234 -> "ITM-00001234"
        /// </summary>
        string GenerateBarcodeString(int itemId);

        /// <summary>
        /// Generates a Code-128 Barcode Image (as byte array) for a given barcode string.
        /// </summary>
        byte[] GenerateBarcodeImage(string barcodeText);
    }
}
