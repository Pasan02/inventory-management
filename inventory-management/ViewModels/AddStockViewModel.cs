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
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;
using System;
using System.Threading;

namespace inventory_management.ViewModels
{
    public partial class AddStockViewModel : ViewModelBase
    {
        private readonly IStockService _stockService;
        private readonly IDatabaseAvailabilityService _availabilityService;
        private readonly IBarcodeService _barcodeService;
        private readonly IPrintService _printService;
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
                    SecretPriceCode = value.SecretPriceCode;
                    StatusMessage = "Ready";
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
            set
            {
                if (SetProperty(ref _currentItem, value))
                {
                    UpdateBarcodeImage();
                }
            }
        }

        private ImageSource? _barcodeImage;
        public ImageSource? BarcodeImage
        {
            get => _barcodeImage;
            set => SetProperty(ref _barcodeImage, value);
        }

        private int _currentQuantity;
        public int CurrentQuantity
        {
            get => _currentQuantity;
            set => SetProperty(ref _currentQuantity, value);
        }

        private string _secretPriceCode = string.Empty;
        public string SecretPriceCode
        {
            get => _secretPriceCode;
            set => SetProperty(ref _secretPriceCode, value);
        }

        private string _statusMessage = "Ready";
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public AddStockViewModel(
            IStockService stockService, 
            IDatabaseAvailabilityService availabilityService, 
            IBarcodeService barcodeService, 
            IPrintService printService)
        {
            _stockService = stockService;
            _availabilityService = availabilityService;
            _barcodeService = barcodeService;
            _printService = printService;
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
            SecretPriceCode = string.Empty;
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
                        SecretPriceCode = string.Empty;
                        StatusMessage = "Item not found.";
                        return;
                    }

                    CurrentQuantity = CurrentItem.Stock?.Quantity ?? 0;
                    SecretPriceCode = CurrentItem.SecretPriceCode;
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
        private async Task AddStock()
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
                var result = await _stockService.AddStockWithPriceAsync(item.Barcode, Quantity, SecretPriceCode);

                if (result.Success)
                {
                    CurrentQuantity = result.NewQuantity;
                    
                    if (!string.IsNullOrEmpty(result.NewBarcode))
                    {
                        // Automatically print the newly generated barcode with quantity printed = quantity added
                        var printSuccess = await _printService.PrintBarcodeLabelAsync(result.NewBarcode, Quantity);
                        
                        string printMessage = printSuccess 
                            ? $"Successfully printed {Quantity} copy/copies of the new barcode label." 
                            : "Failed to automatically print barcode labels. Please check your Zebra printer connection.";
                        
                        ModernMessageDialog.ShowSuccess($"{result.Message}\n\n{printMessage}", "Success");

                        _barcodeInput = result.NewBarcode;
                        OnPropertyChanged(nameof(BarcodeInput));
                        
                        var newItem = await _stockService.FindItemByBarcodeAsync(result.NewBarcode);
                        if (newItem != null)
                        {
                            CurrentItem = newItem;
                            CurrentQuantity = newItem.Stock?.Quantity ?? 0;
                            SecretPriceCode = newItem.SecretPriceCode;
                        }
                    }
                    else
                    {
                        ModernMessageDialog.ShowSuccess(result.Message, "Success");
                        if (CurrentItem?.Stock != null)
                        {
                            CurrentItem.Stock.Quantity = result.NewQuantity;
                        }
                    }
                    Quantity = 1;
                    
                    if (string.IsNullOrEmpty(result.NewBarcode))
                    {
                        _barcodeInput = string.Empty; 
                        OnPropertyChanged(nameof(BarcodeInput)); // Update UI without triggering search reload
                    }
                    
                    SearchText = string.Empty;
                    
                    // Reload the background items list to reflect changes without hiding the current item
                    var items = await _stockService.GetItemsAsync();
                    _allItems.Clear();
                    foreach (var itm in items) _allItems.Add(itm);
                    FilterItems();
                    
                    RequestFocus?.Invoke();
                }
                else
                {
                    StatusMessage = result.Message;
                    ModernMessageDialog.ShowError($"Failed to add stock: {result.Message}", "Error");
                }
            }
            finally
            {
                _dbSemaphore.Release();
            }
        }

        [RelayCommand]
        private async Task PrintBarcode()
        {
            if (CurrentItem == null || string.IsNullOrWhiteSpace(CurrentItem.Barcode))
            {
                StatusMessage = "No item loaded.";
                ModernMessageDialog.ShowWarning("No item is currently loaded.", "Print Warning");
                return;
            }

            var dialog = new SimpleInputDialog("Print Barcode", "Enter number of barcode copies to print:");
            dialog.Owner = Application.Current.MainWindow;
            
            if (dialog.ShowDialog() == true)
            {
                if (!int.TryParse(dialog.InputValue, out int copies) || copies <= 0)
                {
                    MessageBox.Show(Application.Current.MainWindow, "Please enter a valid positive number for copies.", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                try
                {
                    StatusMessage = $"Printing {copies} barcode label(s)...";
                    var success = await _printService.PrintBarcodeLabelAsync(CurrentItem.Barcode, copies);
                    
                    if (success)
                    {
                        StatusMessage = "Barcode label printed successfully.";
                    }
                    else
                    {
                        StatusMessage = "Failed to print barcode label.";
                        MessageBox.Show(Application.Current.MainWindow, "Printing failed. Please ensure the Zebra printer is installed and connected.", "Print Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Print error: {ex.Message}";
                    MessageBox.Show(Application.Current.MainWindow, $"An error occurred while printing: {ex.Message}", "Print Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void UpdateBarcodeImage()
        {
            if (CurrentItem != null && !string.IsNullOrEmpty(CurrentItem.Barcode))
            {
                try
                {
                    var bytes = _barcodeService.GenerateBarcodeImage(CurrentItem.Barcode);
                    BarcodeImage = BytesToImage(bytes);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error generating barcode image: {ex.Message}");
                    BarcodeImage = null;
                }
            }
            else
            {
                BarcodeImage = null;
            }
        }

        private ImageSource? BytesToImage(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0) return null;
            var image = new BitmapImage();
            using (var mem = new MemoryStream(bytes))
            {
                mem.Position = 0;
                image.BeginInit();
                image.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.UriSource = null;
                image.StreamSource = mem;
                image.EndInit();
            }
            image.Freeze();
            return image;
        }

        [RelayCommand]
        private async Task LookupItem()
        {
            await LoadItemByBarcodeInput();
        }
    }
}
