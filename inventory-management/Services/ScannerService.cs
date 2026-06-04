using System;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace inventory_management.Services
{
    public class ScannerService : IScannerService, IDisposable
    {
        private readonly object _sync = new();
        private SerialPort? _serialPort;
        private CancellationTokenSource? _serialLoopCts;

        public ScannerMode CurrentMode { get; private set; } = ScannerMode.KeyboardWedge;

        public string ScannerStatus { get; private set; } = "Scanner mode: Keyboard (USB-HID / wedge fallback)";

        public event EventHandler<BarcodeScannedEventArgs>? BarcodeScanned;

        public event EventHandler? ScannerStatusChanged;

        public async Task RefreshDetectionAsync()
        {
            await Task.Run(() =>
            {
                var ports = SerialPort.GetPortNames()
                    .OrderBy(static p => p, StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                if (ports.Length == 1 && TryAttachSerialScanner(ports[0]))
                {
                    UpdateStatus(ScannerMode.Serial, $"Scanner mode: Serial ({ports[0]})");
                    return;
                }

                StopSerialScanner();

                if (ports.Length > 1)
                {
                    var joined = string.Join(", ", ports);
                    UpdateStatus(
                        ScannerMode.KeyboardWedge,
                        $"Scanner mode: Keyboard (multiple serial ports found: {joined})");
                    return;
                }

                UpdateStatus(ScannerMode.KeyboardWedge, "Scanner mode: Keyboard (USB-HID / wedge fallback)");
            });
        }

        private bool TryAttachSerialScanner(string portName)
        {
            lock (_sync)
            {
                if (_serialPort?.IsOpen == true &&
                    string.Equals(_serialPort.PortName, portName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                StopSerialScanner();

                try
                {
                    var port = new SerialPort(portName, 9600)
                    {
                        Encoding = Encoding.UTF8,
                        NewLine = "\r\n",
                        ReadTimeout = 500,
                        DtrEnable = true,
                        RtsEnable = true
                    };

                    port.Open();
                    _serialPort = port;
                    _serialLoopCts = new CancellationTokenSource();
                    _ = Task.Run(() => ReadSerialLoopAsync(port, _serialLoopCts.Token));
                    return true;
                }
                catch
                {
                    StopSerialScanner();
                    return false;
                }
            }
        }

        private async Task ReadSerialLoopAsync(SerialPort port, CancellationToken cancellationToken)
        {
            var buffer = new StringBuilder();

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var chunk = await Task.Run(port.ReadExisting, cancellationToken);
                    if (string.IsNullOrEmpty(chunk))
                    {
                        await Task.Delay(75, cancellationToken);
                        continue;
                    }

                    foreach (var ch in chunk)
                    {
                        if (ch == '\r' || ch == '\n')
                        {
                            PublishBuffer(buffer);
                            continue;
                        }

                        buffer.Append(ch);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch
                {
                    StopSerialScanner();
                    UpdateStatus(ScannerMode.KeyboardWedge, "Scanner mode: Keyboard (serial scanner disconnected)");
                    break;
                }
            }
        }

        private void PublishBuffer(StringBuilder buffer)
        {
            if (buffer.Length == 0)
            {
                return;
            }

            var barcode = buffer.ToString().Trim();
            buffer.Clear();

            if (string.IsNullOrWhiteSpace(barcode))
            {
                return;
            }

            BarcodeScanned?.Invoke(this, new BarcodeScannedEventArgs(barcode, CurrentMode));
        }

        private void StopSerialScanner()
        {
            lock (_sync)
            {
                _serialLoopCts?.Cancel();
                _serialLoopCts?.Dispose();
                _serialLoopCts = null;

                if (_serialPort != null)
                {
                    try
                    {
                        if (_serialPort.IsOpen)
                        {
                            _serialPort.Close();
                        }
                    }
                    catch
                    {
                    }

                    _serialPort.Dispose();
                    _serialPort = null;
                }
            }
        }

        private void UpdateStatus(ScannerMode mode, string status)
        {
            CurrentMode = mode;
            ScannerStatus = status;
            ScannerStatusChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Dispose()
        {
            StopSerialScanner();
        }
    }
}
