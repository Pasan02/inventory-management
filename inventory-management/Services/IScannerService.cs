using System;
using System.Threading.Tasks;

namespace inventory_management.Services
{
    public enum ScannerMode
    {
        KeyboardWedge,
        Serial
    }

    public sealed class BarcodeScannedEventArgs : EventArgs
    {
        public BarcodeScannedEventArgs(string barcode, ScannerMode mode)
        {
            Barcode = barcode;
            Mode = mode;
        }

        public string Barcode { get; }

        public ScannerMode Mode { get; }
    }

    public interface IScannerService
    {
        ScannerMode CurrentMode { get; }

        string ScannerStatus { get; }

        event EventHandler<BarcodeScannedEventArgs>? BarcodeScanned;

        event EventHandler? ScannerStatusChanged;

        Task RefreshDetectionAsync();
    }
}
