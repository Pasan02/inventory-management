using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using inventory_management.Data;
using inventory_management.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace inventory_management.ViewModels.Search
{
    public partial class SearchAllItemsViewModel : ViewModelBase
    {
        private readonly InventoryDbContext _context;
        private readonly IDatabaseAvailabilityService _availabilityService;
        private readonly IPrintService _printService;
        private readonly System.Action _goBack;

        public PartTypeSearchRow Part { get; }
        public ManufacturerSearchRow? Manufacturer { get; }
        public ModelSearchRow? Model { get; }
        
        public ObservableCollection<ItemSearchRow> Items { get; } = new();

        private bool _includeOutOfStock = false;
        public bool IncludeOutOfStock
        {
            get => _includeOutOfStock;
            set
            {
                if (SetProperty(ref _includeOutOfStock, value))
                {
                    CurrentPage = 1;
                    _ = LoadItemsAsync();
                }
            }
        }
        
        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    CurrentPage = 1;
                    _ = LoadItemsAsync();
                }
            }
        }

        private string _statusMessage = "Ready";
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        // Pagination Properties
        private int _currentPage = 1;
        public int CurrentPage
        {
            get => _currentPage;
            set => SetProperty(ref _currentPage, value);
        }

        private int _totalPages = 1;
        public int TotalPages
        {
            get => _totalPages;
            set => SetProperty(ref _totalPages, value);
        }

        private int _totalItems = 0;
        public int TotalItems
        {
            get => _totalItems;
            set => SetProperty(ref _totalItems, value);
        }

        public int PageSize { get; } = 20;

        public SearchAllItemsViewModel(InventoryDbContext context, IDatabaseAvailabilityService availabilityService, IPrintService printService, PartTypeSearchRow part, ManufacturerSearchRow? manufacturer, ModelSearchRow? model, System.Action goBack)
        {
            _context = context;
            _availabilityService = availabilityService;
            _printService = printService;
            Part = part;
            Manufacturer = manufacturer;
            Model = model;
            _goBack = goBack;
            
            // Start loading items
            _ = LoadItemsAsync();
        }

        [RelayCommand]
        private async Task LoadItemsAsync()
        {
            try
            {
                var availability = await _availabilityService.GetStatusAsync();
                if (!availability.IsAvailable)
                {
                    StatusMessage = availability.Message;
                    return;
                }

                if (Model != null)
                {
                    StatusMessage = $"Loading barcode variants for {Model.Name}...";
                }
                else
                {
                    StatusMessage = Manufacturer == null 
                        ? $"Loading items for {Part.Name}..." 
                        : $"Loading items for {Manufacturer.Name} {Part.Name}...";
                }
                
                // Clear existing items on UI thread
                if (Application.Current?.Dispatcher != null)
                {
                    await Application.Current.Dispatcher.InvokeAsync(() => Items.Clear());
                }
                else
                {
                    Items.Clear();
                }

                var query = _context.Items
                    .Include(i => i.PartType)
                    .Include(i => i.PartBrand)
                    .Include(i => i.VehicleModel)
                        .ThenInclude(vm => vm.Manufacturer)
                    .Include(i => i.CompatibleModels)
                    .Include(i => i.Rack)
                    .Include(i => i.Stock)
                    .AsNoTracking()
                    .Where(i => i.PartTypeId == Part.PartTypeId && 
                                (Manufacturer == null || i.VehicleModel.VehicleManufacturerId == Manufacturer.ManufacturerId) &&
                                (Model == null || i.VehicleModelId == Model.ModelId));

                if (!IncludeOutOfStock)
                {
                    query = query.Where(i => i.Stock != null && i.Stock.Quantity > 0);
                }

                // Apply DB-side search filter
                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    var term = SearchText.Trim().ToLower();
                    query = query.Where(i => 
                        i.VehicleModel.Name.ToLower().Contains(term) ||
                        i.PartBrand.Name.ToLower().Contains(term) ||
                        i.VehicleModel.Manufacturer.Name.ToLower().Contains(term) ||
                        i.CompatibleModels.Any(cm => 
                            (cm.Model != null && cm.Model.ToLower().Contains(term)) ||
                            (cm.Manufacturer != null && cm.Manufacturer.ToLower().Contains(term)) ||
                            (cm.Brand != null && cm.Brand.ToLower().Contains(term)) ||
                            (cm.CountryOfOrigin != null && cm.CountryOfOrigin.ToLower().Contains(term)) ||
                            (cm.YearRange != null && cm.YearRange.ToLower().Contains(term))
                        ) ||
                        i.Description.ToLower().Contains(term) ||
                        i.Barcode.ToLower().Contains(term));
                }

                TotalItems = await query.CountAsync();
                TotalPages = Math.Max(1, (int)Math.Ceiling(TotalItems / (double)PageSize));

                // Ensure current page is valid
                if (CurrentPage > TotalPages)
                {
                    CurrentPage = TotalPages;
                }

                var items = await query
                    .OrderBy(i => i.VehicleModel.Name)
                    .ThenBy(i => i.PartBrand.Name)
                    .Skip((CurrentPage - 1) * PageSize)
                    .Take(PageSize)
                    .ToListAsync();

                // Add items on UI thread to ensure View updates
                if (Application.Current?.Dispatcher != null)
                {
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        foreach (var item in items)
                        {
                            Items.Add(MapToSearchRow(item));
                        }
                    });
                }
                else
                {
                    foreach (var item in items)
                    {
                        Items.Add(MapToSearchRow(item));
                    }
                }

                StatusMessage = TotalItems == 0 
                    ? "No items found." 
                    : $"Showing {Items.Count} of {TotalItems} items.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading items: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"Error in LoadItemsAsync: {ex}");
            }
        }

        [RelayCommand]
        private void NextPage()
        {
            if (CurrentPage < TotalPages)
            {
                CurrentPage++;
                _ = LoadItemsAsync();
            }
        }

        [RelayCommand]
        private void PreviousPage()
        {
            if (CurrentPage > 1)
            {
                CurrentPage--;
                _ = LoadItemsAsync();
            }
        }

        [RelayCommand]
        private void FirstPage()
        {
            if (CurrentPage > 1)
            {
                CurrentPage = 1;
                _ = LoadItemsAsync();
            }
        }

        [RelayCommand]
        private void LastPage()
        {
            if (CurrentPage < TotalPages)
            {
                CurrentPage = TotalPages;
                _ = LoadItemsAsync();
            }
        }

        private ItemSearchRow MapToSearchRow(Data.Entities.Item item)
        {
            var compatText = string.Join(", ", item.CompatibleModels
                .Select(cm => cm.ToString()));

            return new ItemSearchRow
            {
                Barcode = item.Barcode,
                Description = item.Description,
                PartType = item.PartType.Name,
                Brand = item.PartBrand.Name,
                Manufacturer = item.VehicleModel.Manufacturer.Name,
                Model = item.VehicleModel.Name,
                Rack = item.Rack?.LocationCode ?? "N/A",
                Quantity = item.Stock?.Quantity ?? 0,
                Origin = item.CountryOfOrigin,
                ImagePath = item.ImagePath,
                SecretPriceCode = item.SecretPriceCode,
                RegisteredDate = item.RegisteredDate,
                CompatibleModelsText = string.IsNullOrEmpty(compatText) ? "None" : compatText
            };
        }

        [RelayCommand]
        private void Back()
        {
            _goBack();
        }

        [RelayCommand]
        private async Task PrintItemBarcode(ItemSearchRow item)
        {
            if (item == null) return;

            try
            {
                var dialog = new inventory_management.Views.SimpleInputDialog("Print Barcode", "Enter number of barcode copies to print:");
                dialog.Owner = Application.Current.MainWindow;
                
                if (dialog.ShowDialog() != true) return;
                
                if (!int.TryParse(dialog.InputValue, out int copies) || copies <= 0)
                {
                    MessageBox.Show(Application.Current.MainWindow, "Please enter a valid positive number for copies.", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                StatusMessage = $"Printing {copies} barcode label(s) for {item.Barcode}...";
                
                var success = await _printService.PrintBarcodeLabelAsync(item.Barcode, copies);
                if (success)
                {
                    StatusMessage = $"Barcode label printed successfully for {item.Barcode}.";
                }
                else
                {
                    StatusMessage = $"Failed to print barcode label for {item.Barcode}.";
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
}
