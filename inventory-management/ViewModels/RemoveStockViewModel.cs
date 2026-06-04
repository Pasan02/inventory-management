using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using inventory_management.Data.Entities;
using inventory_management.Services;
using inventory_management.Views;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

using System.Collections.Generic; // Added
using System; // Added
using System.Windows.Threading;

namespace inventory_management.ViewModels
{
    public partial class RemoveStockViewModel : ViewModelBase, IScannerAwareViewModel
    {
        private readonly IStockService _stockService;
        private readonly IDatabaseAvailabilityService _availabilityService;
        private readonly IScannerService _scannerService;
        private readonly List<Item> _allItems = new();
        private bool _scannerActive;

        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    FilterItems();
                }
            }
        }

        private string _barcodeInput = string.Empty;
        public string BarcodeInput
        {
            get => _barcodeInput;
            set
            {
                if (SetProperty(ref _barcodeInput, value))
                {
                    _ = LoadItemByBarcodeInput();
                }
            }
        }

        public ObservableCollection<Item> Items { get; } = new();

        private Item? _selectedItem;
        public Item? SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (SetProperty(ref _selectedItem, value) && value != null)
                {
                    BarcodeInput = value.Barcode;
                    CurrentItem = value;
                    CurrentQuantity = value.Stock?.Quantity ?? 0;
                    StatusMessage = "Ready"; // Don't show success message
                }
            }
        }

        private int _quantity = 1;
        public int Quantity
        {
            get => _quantity;
            set => SetProperty(ref _quantity, value);
        }

        private Item? _currentItem;
        public Item? CurrentItem
        {
            get => _currentItem;
            set => SetProperty(ref _currentItem, value);
        }

        private int _currentQuantity;
        public int CurrentQuantity
        {
            get => _currentQuantity;
            set => SetProperty(ref _currentQuantity, value);
        }

        private string _statusMessage = "Ready";
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        private string _scannerStatus = "Scanner mode: Keyboard (USB-HID / wedge fallback)";
        public string ScannerStatus
        {
            get => _scannerStatus;
            set => SetProperty(ref _scannerStatus, value);
        }

        public RemoveStockViewModel(IStockService stockService, IDatabaseAvailabilityService availabilityService, IScannerService scannerService)
        {
            _stockService = stockService;
            _availabilityService = availabilityService;
            _scannerService = scannerService;
            ScannerStatus = _scannerService.ScannerStatus;
            LoadItems();
        }

        private async void LoadItems()
        {
            var availability = await _availabilityService.GetStatusAsync();
            if (!availability.IsAvailable)
            {
                StatusMessage = availability.Message;
                return;
            }

            _allItems.Clear();
            var items = await _stockService.GetItemsAsync();
            foreach (var item in items)
            {
                _allItems.Add(item);
            }
            FilterItems();
        }

        private void FilterItems()
        {
            Items.Clear();
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                // If search is empty, show nothing to keep list clean, or all. 
                // "list according to user typed text" implies filtering.
                // Let's show all initally as fallback or empty? 
                // A dropdown replacement usually shows options. 
                foreach(var item in _allItems) Items.Add(item);
            }
            else
            {
                var lower = SearchText.ToLower();
                foreach(var item in _allItems.Where(i => i.Barcode.ToLower().Contains(lower)))
                {
                    Items.Add(item);
                }
            }
        }

        private void ClearInputs()
        {
            _barcodeInput = string.Empty;
            OnPropertyChanged(nameof(BarcodeInput));
            SearchText = string.Empty;
            Quantity = 1;
            StatusMessage = "Ready";
            SelectedItem = null;
            CurrentItem = null;
            CurrentQuantity = 0;
            // Reload to refresh stock counts if needed, or just reset filters
            FilterItems(); 
        }

        [RelayCommand]
        private void LoadItemsCommand()
        {
            LoadItems();
        }

        private async Task LoadItemByBarcodeInput()
        {
            if (string.IsNullOrWhiteSpace(BarcodeInput))
            {
                CurrentItem = null;
                CurrentQuantity = 0;
                StatusMessage = "Ready";
                return;
            }

            var availability = await _availabilityService.GetStatusAsync();
            if (!availability.IsAvailable)
            {
                StatusMessage = availability.Message;
                CurrentItem = null;
                CurrentQuantity = 0;
                return;
            }

            StatusMessage = "Searching...";
            CurrentItem = await _stockService.FindItemByBarcodeOrNameAsync(BarcodeInput);

            if (CurrentItem == null)
            {
                CurrentQuantity = 0;
                StatusMessage = "Item not found.";
                return;
            }

            CurrentQuantity = CurrentItem.Stock?.Quantity ?? 0;
            StatusMessage = "Item loaded.";
        }

        [RelayCommand]
        private void Reset()
        {
            ClearInputs();
        }

        [RelayCommand]
        private async Task RemoveStock()
        {
            if (Quantity <= 0)
            {
                StatusMessage = "Quantity must be greater than zero.";
                ModernMessageDialog.ShowWarning("Quantity must be greater than zero.", "Invalid Quantity");
                return;
            }

            var item = CurrentItem ?? await _stockService.FindItemByBarcodeAsync(BarcodeInput);
            if (item == null)
            {
                StatusMessage = "Item not found.";
                ModernMessageDialog.ShowError("Item not found.", "Error");
                CurrentItem = null;
                CurrentQuantity = 0;
                return;
            }

            CurrentItem = item;
            var result = await _stockService.RemoveStockAsync(item.Barcode, Quantity);

            if (result.Success)
            {
                ModernMessageDialog.ShowSuccess($"Stock removed successfully.\nNew Quantity: {result.NewQuantity}", "Success");
                
                // Refresh the current item to show updated stock
                var updatedItem = await _stockService.FindItemByBarcodeAsync(item.Barcode);
                if (updatedItem != null)
                {
                    CurrentItem = updatedItem;
                    CurrentQuantity = updatedItem.Stock?.Quantity ?? 0;
                }
                
                Quantity = 1;
            }
            else
            {
                StatusMessage = result.Message;
                ModernMessageDialog.ShowError($"Failed to remove stock: {result.Message}", "Error");
            }
        }

        [RelayCommand]
        private async Task ScanRemove()
        {
            if (string.IsNullOrWhiteSpace(BarcodeInput))
            {
                StatusMessage = "Barcode is required.";
                return;
            }

            var availability = await _availabilityService.GetStatusAsync();
            if (!availability.IsAvailable)
            {
                StatusMessage = availability.Message;
                return;
            }

            var item = await _stockService.FindItemByBarcodeAsync(BarcodeInput);
            if (item == null)
            {
                StatusMessage = "Item not found.";
                ModernMessageDialog.ShowError("Item not found.", "Error");
                CurrentItem = null;
                CurrentQuantity = 0;
                return;
            }

            CurrentItem = item;
            var result = await _stockService.RemoveStockAsync(item.Barcode, 1);

            if (result.Success)
            {
                ModernMessageDialog.ShowSuccess($"Stock removed successfully.\nNew Quantity: {result.NewQuantity}", "Success");
                
                // Refresh the current item to show updated stock
                var updatedItem = await _stockService.FindItemByBarcodeAsync(item.Barcode);
                if (updatedItem != null)
                {
                    CurrentItem = updatedItem;
                    CurrentQuantity = updatedItem.Stock?.Quantity ?? 0;
                }
                
                Quantity = 1;
            }
            else
            {
                 StatusMessage = result.Message;
                 ModernMessageDialog.ShowError($"Failed to remove stock: {result.Message}", "Error");
            }
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
            _ = App.Current.Dispatcher.InvokeAsync(() => ScannerStatus = _scannerService.ScannerStatus, DispatcherPriority.Normal);
        }

        private void OnBarcodeScanned(object? sender, BarcodeScannedEventArgs e)
        {
            _ = App.Current.Dispatcher.InvokeAsync(async () =>
            {
                BarcodeInput = e.Barcode;
                await ScanRemove();
            }, DispatcherPriority.Normal);
        }
    }
}
