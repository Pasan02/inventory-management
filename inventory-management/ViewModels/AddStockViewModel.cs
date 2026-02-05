using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using inventory_management.Data.Entities;
using inventory_management.Services;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace inventory_management.ViewModels
{
    public partial class AddStockViewModel : ViewModelBase
    {
        private readonly IStockService _stockService;
        private readonly IDatabaseAvailabilityService _availabilityService;

        private string _barcodeInput = string.Empty;
        public string BarcodeInput
        {
            get => _barcodeInput;
            set => SetProperty(ref _barcodeInput, value);
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
                    StatusMessage = "Item selected.";
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

        public AddStockViewModel(IStockService stockService, IDatabaseAvailabilityService availabilityService)
        {
            _stockService = stockService;
            _availabilityService = availabilityService;
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

            Items.Clear();
            var items = await _stockService.GetItemsAsync();
            foreach (var item in items)
            {
                Items.Add(item);
            }

            if (Items.Count == 0)
            {
                StatusMessage = "No items found.";
            }
        }

        [RelayCommand]
        private async Task LookupItem()
        {
            var availability = await _availabilityService.GetStatusAsync();
            if (!availability.IsAvailable)
            {
                StatusMessage = availability.Message;
                CurrentItem = null;
                CurrentQuantity = 0;
                return;
            }

            StatusMessage = "Searching...";
            CurrentItem = await _stockService.FindItemByBarcodeAsync(BarcodeInput);

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
        private async Task AddStock()
        {
            if (Quantity <= 0)
            {
                StatusMessage = "Quantity must be greater than zero.";
                return;
            }

            var item = CurrentItem ?? await _stockService.FindItemByBarcodeAsync(BarcodeInput);
            if (item == null)
            {
                StatusMessage = "Item not found.";
                CurrentItem = null;
                CurrentQuantity = 0;
                return;
            }

            CurrentItem = item;
            var result = await _stockService.AddStockAsync(item.Barcode, Quantity);
            StatusMessage = result.Message;

            if (result.Success)
            {
                CurrentQuantity = result.NewQuantity;
            }
        }

        [RelayCommand]
        private async Task ScanAdd()
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
                CurrentItem = null;
                CurrentQuantity = 0;
                return;
            }

            CurrentItem = item;
            var result = await _stockService.AddStockAsync(item.Barcode, 1);
            StatusMessage = result.Message;

            if (result.Success)
            {
                CurrentQuantity = result.NewQuantity;
            }
        }
    }
}
