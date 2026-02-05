using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using inventory_management.Data.Entities;
using inventory_management.Services;
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
            if (CurrentItem == null)
            {
                StatusMessage = "Load an item first.";
                return;
            }

            var result = await _stockService.AddStockAsync(CurrentItem.Barcode, Quantity);
            StatusMessage = result.Message;

            if (result.Success)
            {
                CurrentQuantity = result.NewQuantity;
            }
        }
    }
}
