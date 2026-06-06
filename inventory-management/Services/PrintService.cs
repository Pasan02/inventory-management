using System;
using System.Drawing.Printing;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace inventory_management.Services
{
    public class PrintService : IPrintService
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        private class DOCINFOA
        {
            [MarshalAs(UnmanagedType.LPStr)] public string pDocName = string.Empty;
            [MarshalAs(UnmanagedType.LPStr)] public string pOutputFile = null!;
            [MarshalAs(UnmanagedType.LPStr)] public string pDataType = string.Empty;
        }

        [DllImport("winspool.Drv", EntryPoint = "OpenPrinterA", SetLastError = true, CharSet = CharSet.Ansi, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern bool OpenPrinter([MarshalAs(UnmanagedType.LPStr)] string szPrinter, out IntPtr hPrinter, IntPtr pd);

        [DllImport("winspool.Drv", EntryPoint = "ClosePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern bool ClosePrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", EntryPoint = "StartDocPrinterA", SetLastError = true, CharSet = CharSet.Ansi, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern bool StartDocPrinter(IntPtr hPrinter, int level, [In, MarshalAs(UnmanagedType.LPStruct)] DOCINFOA di);

        [DllImport("winspool.Drv", EntryPoint = "EndDocPrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern bool EndDocPrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", EntryPoint = "StartPagePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern bool StartPagePrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", EntryPoint = "EndPagePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern bool EndPagePrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", EntryPoint = "WritePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern bool WritePrinter(IntPtr hPrinter, IntPtr pBytes, int dwCount, out int dwWritten);

        public Task<bool> PrintBarcodeLabelAsync(string barcode, int copies = 1)
        {
            return Task.Run(() =>
            {
                try
                {
                    string? printerName = FindZebraPrinter();
                    if (string.IsNullOrEmpty(printerName))
                    {
                        System.Diagnostics.Debug.WriteLine("No printer detected.");
                        return false;
                    }

                    // Format ZPL string
                    // ZD230 standard print density is 203 DPI (8 dots/mm)
                    // ^XA - Start Format
                    // ^CF0,24 - Set default font (Font 0, Height 24, Width 24)
                    // ^FO50,30 - Field Origin (x=50, y=30)
                    // ^FD - Field Data
                    // ^FS - Field Separator
                    // ^BY2,2.0,50 - Barcode width 2, ratio 2.0, height 50
                    // ^BCN,50,Y,N,N - Code 128 (Normal, Height 50, Print interpretation line=Yes)
                    // ^XZ - End Format
                    string zpl = 
                        "^XA\n" +
                        $"^FO0,40^FB400,1,0,C^A0N,24,24^FDAlpine Auto A/C^FS\n" +
                        $"^FO10,85^BY2,2.0,50^BCN,50,Y,N,N^FD{barcode}^FS\n" +
                        $"^PQ{copies}\n" +
                        "^XZ\n";

                    return SendStringToPrinter(printerName, zpl);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error printing barcode label: {ex.Message}");
                    return false;
                }
            });
        }

        private string? FindZebraPrinter()
        {
            try
            {
                // Search installed printers for keywords
                foreach (string printer in PrinterSettings.InstalledPrinters)
                {
                    if (printer.Contains("Zebra", StringComparison.OrdinalIgnoreCase) ||
                        printer.Contains("ZDesigner", StringComparison.OrdinalIgnoreCase))
                    {
                        return printer;
                    }
                }

                // Fallback: Default system printer
                var settings = new PrinterSettings();
                return settings.PrinterName;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error finding Zebra printer: {ex.Message}");
                return null;
            }
        }

        private bool SendStringToPrinter(string printerName, string zplString)
        {
            IntPtr hPrinter = IntPtr.Zero;
            DOCINFOA di = new DOCINFOA
            {
                pDocName = "Inventory Barcode Label",
                pDataType = "RAW"
            };
            bool success = false;

            if (OpenPrinter(printerName.Normalize(), out hPrinter, IntPtr.Zero))
            {
                if (StartDocPrinter(hPrinter, 1, di))
                {
                    if (StartPagePrinter(hPrinter))
                    {
                        IntPtr pBytes = Marshal.StringToCoTaskMemAnsi(zplString);
                        int dwCount = zplString.Length;
                        
                        success = WritePrinter(hPrinter, pBytes, dwCount, out int dwWritten);
                        
                        Marshal.FreeCoTaskMem(pBytes);
                        EndPagePrinter(hPrinter);
                    }
                    EndDocPrinter(hPrinter);
                }
                ClosePrinter(hPrinter);
            }

            return success;
        }

        private string EscapeZpl(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            // Basic cleanup to prevent ZPL injection characters if any
            return input.Replace("^", " ").Replace("_", " ");
        }
    }
}
