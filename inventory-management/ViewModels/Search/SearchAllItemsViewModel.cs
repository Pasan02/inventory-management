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
        public ManufacturerSearchRow Manufacturer { get; }
        
        public ObservableCollection<ItemSearchRow> Items { get; } = new();
        public ICollectionView ItemsView { get; }

        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    ItemsView.Refresh();
                }
            }
        }

        private string _statusMessage = "Ready";
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public SearchAllItemsViewModel(InventoryDbContext context, IDatabaseAvailabilityService availabilityService, IPrintService printService, PartTypeSearchRow part, ManufacturerSearchRow manufacturer, System.Action goBack)
        {
            _context = context;
            _availabilityService = availabilityService;
            _printService = printService;
            Part = part;
            Manufacturer = manufacturer;
            _goBack = goBack;

            // Initialize collection view
            ItemsView = CollectionViewSource.GetDefaultView(Items);
            ItemsView.Filter = FilterItems;
            
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

                StatusMessage = $"Loading items for {Manufacturer.Name} {Part.Name}...";
                
                // Clear existing items on UI thread
                if (Application.Current?.Dispatcher != null)
                {
                    await Application.Current.Dispatcher.InvokeAsync(() => Items.Clear());
                }
                else
                {
                    Items.Clear();
                }

                var items = await _context.Items
                    .Include(i => i.PartType)
                    .Include(i => i.PartBrand)
                    .Include(i => i.VehicleModel)
                        .ThenInclude(vm => vm.Manufacturer)
                    .Include(i => i.Rack)
                    .Include(i => i.Stock)
                    .AsNoTracking()
                    .Where(i => i.PartTypeId == Part.PartTypeId && 
                                i.VehicleModel.VehicleManufacturerId == Manufacturer.ManufacturerId)
                    .OrderBy(i => i.VehicleModel.Name)
                    .ThenBy(i => i.PartBrand.Name)
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

                StatusMessage = Items.Count == 0 
                    ? "No items found." 
                    : $"{Items.Count} items found.";
                
                ItemsView.Refresh();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading items: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"Error in LoadItemsAsync: {ex}");
            }
        }

        private ItemSearchRow MapToSearchRow(Data.Entities.Item item)
        {
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
                ImagePath = item.ImagePath
            };
        }

        private bool FilterItems(object item)
        {
            if (item is not ItemSearchRow row) return false;
            
            if (string.IsNullOrWhiteSpace(SearchText)) return true;

            var term = SearchText.Trim();
            return row.Model.Contains(term, StringComparison.OrdinalIgnoreCase)
                   || row.Brand.Contains(term, StringComparison.OrdinalIgnoreCase)
                   || row.Manufacturer.Contains(term, StringComparison.OrdinalIgnoreCase)
                   || row.Description.Contains(term, StringComparison.OrdinalIgnoreCase)
                   || row.Barcode.Contains(term, StringComparison.OrdinalIgnoreCase);
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
                StatusMessage = $"Printing barcode label for {item.Barcode}...";
                var title = $"{item.Brand} {item.PartType}".Trim();
                var details = $"{item.Manufacturer} {item.Model}".Trim();
                
                var success = await _printService.PrintBarcodeLabelAsync(item.Barcode, title, details);
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
