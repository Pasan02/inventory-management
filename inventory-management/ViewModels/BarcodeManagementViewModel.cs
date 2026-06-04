using CommunityToolkit.Mvvm.Input;
using inventory_management.Data.Entities;
using inventory_management.Services;
using inventory_management.Views;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace inventory_management.ViewModels
{
    public partial class BarcodeManagementViewModel : ViewModelBase, IScannerAwareViewModel
    {
        private readonly IStockService _stockService;
        private readonly IBarcodeService _barcodeService;
        private readonly IBarcodePrintService _barcodePrintService;
        private readonly IScannerService _scannerService;
        private bool _scannerActive;

        public ObservableCollection<Item> Items { get; } = new();

        public ObservableCollection<string> Printers { get; } = new();

        public ObservableCollection<BarcodeLabelProfile> LabelProfiles { get; } = new();

        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    _ = LoadItemsAsync();
                }
            }
        }

        private Item? _selectedItem;
        public Item? SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (SetProperty(ref _selectedItem, value))
                {
                    UpdatePreview();
                }
            }
        }

        private string _scannerStatus = "Scanner mode: Keyboard (USB-HID / wedge fallback)";
        public string ScannerStatus
        {
            get => _scannerStatus;
            set => SetProperty(ref _scannerStatus, value);
        }

        private string _barcodeText = string.Empty;
        public string BarcodeText
        {
            get => _barcodeText;
            set => SetProperty(ref _barcodeText, value);
        }

        private ImageSource? _barcodePreview;
        public ImageSource? BarcodePreview
        {
            get => _barcodePreview;
            set => SetProperty(ref _barcodePreview, value);
        }

        private string _selectedPrinter = string.Empty;
        public string SelectedPrinter
        {
            get => _selectedPrinter;
            set => SetProperty(ref _selectedPrinter, value);
        }

        private BarcodeLabelProfile? _selectedLabelProfile;
        public BarcodeLabelProfile? SelectedLabelProfile
        {
            get => _selectedLabelProfile;
            set => SetProperty(ref _selectedLabelProfile, value);
        }

        private int _printQuantity = 1;
        public int PrintQuantity
        {
            get => _printQuantity;
            set => SetProperty(ref _printQuantity, value);
        }

        private string _statusMessage = "Ready";
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public BarcodeManagementViewModel(
            IStockService stockService,
            IBarcodeService barcodeService,
            IBarcodePrintService barcodePrintService,
            IScannerService scannerService)
        {
            _stockService = stockService;
            _barcodeService = barcodeService;
            _barcodePrintService = barcodePrintService;
            _scannerService = scannerService;
            ScannerStatus = scannerService.ScannerStatus;

            foreach (var profile in _barcodePrintService.GetLabelProfiles())
            {
                LabelProfiles.Add(profile);
            }

            SelectedLabelProfile = LabelProfiles.FirstOrDefault();
            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            await LoadItemsAsync();
            await LoadPrintersAsync();
        }

        [RelayCommand]
        private Task LoadItemsAsync() => LoadItemsCoreAsync();

        private async Task LoadItemsCoreAsync()
        {
            var items = await _stockService.GetItemsAsync();
            Items.Clear();

            var query = SearchText?.Trim();
            var filtered = string.IsNullOrWhiteSpace(query)
                ? items
                : items.Where(i =>
                    i.Barcode.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    i.PartType.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    i.PartBrand.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    i.VehicleModel.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    i.VehicleModel.Manufacturer.Name.Contains(query, StringComparison.OrdinalIgnoreCase));

            foreach (var item in filtered)
            {
                Items.Add(item);
            }

            if (SelectedItem != null)
            {
                SelectedItem = Items.FirstOrDefault(i => i.Id == SelectedItem.Id);
            }

            StatusMessage = $"{Items.Count} item(s) loaded.";
        }

        [RelayCommand]
        private async Task LoadPrintersAsync()
        {
            Printers.Clear();
            var printers = await _barcodePrintService.GetInstalledPrintersAsync();
            foreach (var printer in printers)
            {
                Printers.Add(printer);
            }

            if (Printers.Count > 0 && string.IsNullOrWhiteSpace(SelectedPrinter))
            {
                SelectedPrinter = Printers[0];
            }
        }

        [RelayCommand]
        private void PrintPreviewBarcode()
        {
            if (SelectedItem == null)
            {
                StatusMessage = "Select an item first.";
                ModernMessageDialog.ShowWarning("Select an item first.", "Barcode Preview");
                return;
            }

            UpdatePreview();
            StatusMessage = "Barcode preview refreshed.";
        }

        [RelayCommand]
        private async Task PrintBarcodeAsync()
        {
            if (SelectedItem == null)
            {
                ModernMessageDialog.ShowWarning("Select an item first.", "Print Barcode");
                return;
            }

            if (string.IsNullOrWhiteSpace(SelectedPrinter))
            {
                ModernMessageDialog.ShowWarning("Select a printer first.", "Print Barcode");
                return;
            }

            if (SelectedLabelProfile == null)
            {
                ModernMessageDialog.ShowWarning("Select a label profile first.", "Print Barcode");
                return;
            }

            if (PrintQuantity <= 0)
            {
                ModernMessageDialog.ShowWarning("Print quantity must be greater than zero.", "Print Barcode");
                return;
            }

            try
            {
                var barcode = EnsureBarcode(SelectedItem);
                var imageBytes = _barcodeService.GenerateBarcodeImage(barcode);

                await _barcodePrintService.PrintBarcodeAsync(
                    SelectedPrinter,
                    SelectedLabelProfile,
                    imageBytes,
                    barcode,
                    $"{SelectedItem.PartType.Name} / {SelectedItem.PartBrand.Name}",
                    $"{SelectedItem.VehicleModel.Manufacturer.Name} {SelectedItem.VehicleModel.Name}",
                    PrintQuantity);

                StatusMessage = "Barcode print job sent successfully.";
                ModernMessageDialog.ShowSuccess("Barcode print job sent successfully.", "Print Barcode");
            }
            catch (Exception ex)
            {
                StatusMessage = $"Print failed: {ex.Message}";
                ModernMessageDialog.ShowError($"Print failed: {ex.Message}", "Print Barcode");
            }
        }

        private void UpdatePreview()
        {
            if (SelectedItem == null)
            {
                BarcodeText = string.Empty;
                BarcodePreview = null;
                return;
            }

            var barcode = EnsureBarcode(SelectedItem);
            BarcodeText = barcode;
            BarcodePreview = BytesToImage(_barcodeService.GenerateBarcodeImage(barcode));
        }

        private string EnsureBarcode(Item item)
        {
            if (!string.IsNullOrWhiteSpace(item.Barcode))
            {
                return item.Barcode;
            }

            return _barcodeService.GenerateBarcodeString(item.Id);
        }

        private static ImageSource? BytesToImage(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
            {
                return null;
            }

            var image = new BitmapImage();
            using var mem = new MemoryStream(bytes);
            image.BeginInit();
            image.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = mem;
            image.EndInit();
            image.Freeze();
            return image;
        }

        public void ActivateScanner()
        {
            if (_scannerActive)
            {
                return;
            }

            _scannerActive = true;
            _scannerService.BarcodeScanned += OnBarcodeScanned;
            _scannerService.ScannerStatusChanged += OnScannerStatusChanged;
            ScannerStatus = _scannerService.ScannerStatus;
            _ = _scannerService.RefreshDetectionAsync();
        }

        public void DeactivateScanner()
        {
            if (!_scannerActive)
            {
                return;
            }

            _scannerActive = false;
            _scannerService.BarcodeScanned -= OnBarcodeScanned;
            _scannerService.ScannerStatusChanged -= OnScannerStatusChanged;
        }

        private void OnScannerStatusChanged(object? sender, EventArgs e)
        {
            App.Current.Dispatcher.Invoke(() => ScannerStatus = _scannerService.ScannerStatus);
        }

        private void OnBarcodeScanned(object? sender, BarcodeScannedEventArgs e)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                SearchText = e.Barcode;
                SelectedItem = Items.FirstOrDefault(i => string.Equals(i.Barcode, e.Barcode, StringComparison.OrdinalIgnoreCase));
                StatusMessage = SelectedItem == null
                    ? $"Scanned barcode not found: {e.Barcode}"
                    : $"Scanned and selected item: {e.Barcode}";
            });
        }
    }
}
