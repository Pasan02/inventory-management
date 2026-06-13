using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using inventory_management.Data.Entities;
using inventory_management.Services;
using inventory_management.Views;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

using System.Collections.Generic;
using System;
using System.Threading;

namespace inventory_management.ViewModels
{
    public partial class RemoveStockViewModel : ViewModelBase
    {
        private readonly IStockService _stockService;
        private readonly IDatabaseAvailabilityService _availabilityService;
        private readonly List<Item> _allItems = new();
        private readonly SemaphoreSlim _dbSemaphore = new(1, 1);
        private CancellationTokenSource? _searchCts;
        
        public event Action? RequestFocus;
        public event Action? ItemLoaded;

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
                    _barcodeInput = value.Barcode;
                    OnPropertyChanged(nameof(BarcodeInput));
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

        private bool _isReplacement;
        public bool IsReplacement
        {
            get => _isReplacement;
            set => SetProperty(ref _isReplacement, value);
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

        public RemoveStockViewModel(IStockService stockService, IDatabaseAvailabilityService availabilityService)
        {
            _stockService = stockService;
            _availabilityService = availabilityService;
            LoadItems();
        }

        private async void LoadItems()
        {
            await _dbSemaphore.WaitAsync();
            try
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
            finally
            {
                _dbSemaphore.Release();
            }
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
            IsReplacement = false;
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
            _searchCts?.Cancel();
            _searchCts = new CancellationTokenSource();
            var token = _searchCts.Token;

            if (string.IsNullOrWhiteSpace(BarcodeInput))
            {
                CurrentItem = null;
                CurrentQuantity = 0;
                StatusMessage = "Ready";
                return;
            }

            try
            {
                await Task.Delay(250, token);

                await _dbSemaphore.WaitAsync(token);
                try
                {
                    var availability = await _availabilityService.GetStatusAsync();
                    if (token.IsCancellationRequested) return;

                    if (!availability.IsAvailable)
                    {
                        StatusMessage = availability.Message;
                        CurrentItem = null;
                        CurrentQuantity = 0;
                        return;
                    }

                    StatusMessage = "Searching...";
                    var item = await _stockService.FindItemByBarcodeOrNameAsync(BarcodeInput);
                    if (token.IsCancellationRequested) return;

                    CurrentItem = item;

                    if (CurrentItem == null)
                    {
                        CurrentQuantity = 0;
                        StatusMessage = "Item not found.";
                        return;
                    }

                    CurrentQuantity = CurrentItem.Stock?.Quantity ?? 0;
                    StatusMessage = "Item loaded.";
                    ItemLoaded?.Invoke();
                }
                finally
                {
                    _dbSemaphore.Release();
                }
            }
            catch (OperationCanceledException)
            {
                // Ignore cancellation
            }
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

            await _dbSemaphore.WaitAsync();
            try
            {
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
                var result = await _stockService.RemoveStockAsync(item.Barcode, Quantity, IsReplacement);

                if (result.Success)
                {
                    if (result.NewQuantity == 0)
                    {
                        ModernMessageDialog.ShowSuccess("Stock has been completely removed. The item is now out of stock.", "Stock Cleared");
                        ClearInputs();
                    }
                    else
                    {
                        ModernMessageDialog.ShowSuccess($"Stock removed successfully.\nNew Quantity: {result.NewQuantity}", "Success");
                        CurrentQuantity = result.NewQuantity;
                        if (CurrentItem?.Stock != null)
                        {
                            CurrentItem.Stock.Quantity = result.NewQuantity;
                        }
                        Quantity = 1;
                        
                        _barcodeInput = string.Empty; 
                        OnPropertyChanged(nameof(BarcodeInput)); // Update UI without triggering search reload
                        
                        SearchText = string.Empty;
                        
                        // Reload the background items list to reflect changes without hiding the current item
                        var items = await _stockService.GetItemsAsync();
                        _allItems.Clear();
                        foreach (var itm in items) _allItems.Add(itm);
                        FilterItems();
                    }
                    
                    RequestFocus?.Invoke();
                }
                else
                {
                    StatusMessage = result.Message;
                    ModernMessageDialog.ShowError($"Failed to remove stock: {result.Message}", "Error");
                }
            }
            finally
            {
                _dbSemaphore.Release();
            }
        }

        [RelayCommand]
        private async Task LookupItem()
        {
            await LoadItemByBarcodeInput();
        }
    }
}
