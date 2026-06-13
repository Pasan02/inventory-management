using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using inventory_management.Data;
using inventory_management.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;

namespace inventory_management.ViewModels.Search
{
    public partial class SearchModelsViewModel : ViewModelBase
    {
        private readonly InventoryDbContext _context;
        private readonly IDatabaseAvailabilityService _availabilityService;
        private readonly IPrintService _printService;
        private readonly System.Action _goBack;
        private readonly System.Action _viewAllItems;

        public PartTypeSearchRow Part { get; }
        public ManufacturerSearchRow Manufacturer { get; }
        public ObservableCollection<ModelSearchRow> Models { get; } = new();
        public ICollectionView ModelsView { get; }

        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    ModelsView.Refresh();
                }
            }
        }

        private string _statusMessage = "Ready";
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public SearchModelsViewModel(
            InventoryDbContext context,
            IDatabaseAvailabilityService availabilityService,
            IPrintService printService,
            PartTypeSearchRow part,
            ManufacturerSearchRow manufacturer,
            System.Action goBack,
            System.Action viewAllItems)
        {
            _context = context;
            _availabilityService = availabilityService;
            _printService = printService;
            Part = part;
            Manufacturer = manufacturer;
            _goBack = goBack;
            _viewAllItems = viewAllItems;
            ModelsView = CollectionViewSource.GetDefaultView(Models);
            ModelsView.Filter = FilterModels;
            _ = LoadModelsAsync();
        }

        [RelayCommand]
        private async Task LoadModelsAsync()
        {
            var availability = await _availabilityService.GetStatusAsync();
            if (!availability.IsAvailable)
            {
                StatusMessage = availability.Message;
                return;
            }

            StatusMessage = "Loading models...";
            Models.Clear();

            var partTypeImage = await _context.PartTypes
                .Where(p => p.Id == Part.PartTypeId)
                .Select(p => p.ImagePath)
                .FirstOrDefaultAsync();

            var manufacturerLogo = await _context.Manufacturers
                .Where(m => m.Id == Manufacturer.ManufacturerId)
                .Select(m => m.LogoPath)
                .FirstOrDefaultAsync();

            var models = await _context.Models
                .AsNoTracking()
                .Where(m => m.VehicleManufacturerId == Manufacturer.ManufacturerId)
                .OrderBy(m => m.Name)
                .ToListAsync();

            var items = await _context.Items
                .Include(i => i.PartBrand)
                .Include(i => i.Rack)
                .Include(i => i.Stock)
                .Include(i => i.VehicleModel)
                .Include(i => i.CompatibleModels)
                .AsNoTracking()
                .Where(i => i.PartTypeId == Part.PartTypeId && 
                            (i.VehicleModel.VehicleManufacturerId == Manufacturer.ManufacturerId || 
                             i.CompatibleModels.Any(cm => 
                                 cm.Manufacturer != null && 
                                 cm.Manufacturer.ToLower() == Manufacturer.Name.ToLower())))
                .ToListAsync();

            var groupedItems = new Dictionary<int, List<Data.Entities.Item>>();
            foreach (var item in items)
            {
                if (item.VehicleModel.VehicleManufacturerId == Manufacturer.ManufacturerId)
                {
                    if (!groupedItems.ContainsKey(item.VehicleModelId))
                    {
                        groupedItems[item.VehicleModelId] = new List<Data.Entities.Item>();
                    }
                    groupedItems[item.VehicleModelId].Add(item);
                }

                foreach (var cm in item.CompatibleModels)
                {
                    if (cm.Manufacturer != null && cm.Manufacturer.Equals(Manufacturer.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        var matchingModel = models.FirstOrDefault(m => 
                            cm.Model != null && m.Name.Equals(cm.Model, StringComparison.OrdinalIgnoreCase));
                        
                        if (matchingModel != null)
                        {
                            if (!groupedItems.ContainsKey(matchingModel.Id))
                            {
                                groupedItems[matchingModel.Id] = new List<Data.Entities.Item>();
                            }
                            if (!groupedItems[matchingModel.Id].Any(x => x.Id == item.Id))
                            {
                                groupedItems[matchingModel.Id].Add(item);
                            }
                        }
                    }
                }
            }

            var rows = models
                .Select(model =>
                {
                    groupedItems.TryGetValue(model.Id, out var modelItems);
                    modelItems ??= new List<Data.Entities.Item>();

                    return new ModelSearchRow
                    {
                        ModelId = model.Id,
                        Name = model.Name,
                        YearRange = model.YearRange ?? string.Empty,
                        ItemCount = modelItems.Count,
                        Quantity = modelItems.Sum(i => i.Stock != null ? i.Stock.Quantity : 0),
                        Brands = string.Join(", ", modelItems.Select(i => i.PartBrand.Name).Distinct().OrderBy(n => n)),
                        Racks = string.Join(", ", modelItems.Select(i => i.Rack != null ? i.Rack.LocationCode : string.Empty).Where(r => !string.IsNullOrWhiteSpace(r)).Distinct().OrderBy(r => r)),
                        CountriesOfOrigin = string.Join(", ", modelItems.Select(i => i.CountryOfOrigin).Where(c => !string.IsNullOrWhiteSpace(c)).Distinct().OrderBy(c => c)),
                        ItemImagePath = modelItems.Select(i => i.ImagePath).FirstOrDefault(p => !string.IsNullOrWhiteSpace(p))
                    };
                })
                .ToList();

            foreach (var row in rows)
            {
                Models.Add(row);
            }

            StatusMessage = Models.Count == 0 ? "No models found." : $"{Models.Count} models available.";
            ModelsView.Refresh();
        }

        private bool FilterModels(object item)
        {
            if (item is not ModelSearchRow row)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(SearchText))
            {
                return true;
            }

            var term = SearchText.Trim();
            return row.Name.Contains(term, StringComparison.OrdinalIgnoreCase)
                   || row.Brands.Contains(term, StringComparison.OrdinalIgnoreCase);
        }

        [RelayCommand]
        private void Back()
        {
            _goBack();
        }

        [RelayCommand]
        private void ViewAllItems()
        {
            _viewAllItems();
        }

        [RelayCommand]
        private async Task PrintModelBarcodeAsync(ModelSearchRow modelRow)
        {
            if (modelRow == null) return;

            try
            {
                var dialog = new inventory_management.Views.SimpleInputDialog("Print Barcode", "Enter number of barcode copies to print:");
                dialog.Owner = System.Windows.Application.Current.MainWindow;
                
                if (dialog.ShowDialog() != true) return;
                
                if (!int.TryParse(dialog.InputValue, out int copies) || copies <= 0)
                {
                    System.Windows.MessageBox.Show(System.Windows.Application.Current.MainWindow, "Please enter a valid positive number for copies.", "Invalid Input", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                    return;
                }

                StatusMessage = $"Printing {copies} barcode label(s) for {modelRow.Name}...";
                // Get one item from this model to get its barcode to print (since a model might have multiple items,
                // we'll try to get the first barcode for this specific part/manufacturer/model combination).
                var item = await _context.Items
                    .Include(i => i.PartBrand)
                    .Include(i => i.PartType)
                    .Include(i => i.VehicleModel)
                        .ThenInclude(vm => vm.Manufacturer)
                    .FirstOrDefaultAsync(i => i.PartTypeId == Part.PartTypeId && i.VehicleModelId == modelRow.ModelId);

                if (item != null && !string.IsNullOrWhiteSpace(item.Barcode))
                {
                    var success = await _printService.PrintBarcodeLabelAsync(item.Barcode, copies);
                    if (success)
                    {
                        StatusMessage = $"Barcode label printed successfully for {modelRow.Name}.";
                    }
                    else
                    {
                        StatusMessage = $"Failed to print barcode label for {modelRow.Name}.";
                        System.Windows.MessageBox.Show(System.Windows.Application.Current.MainWindow, "Printing failed. Please ensure the Zebra printer is installed and connected.", "Print Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    }
                }
                else
                {
                    StatusMessage = $"No item with barcode found for {modelRow.Name}.";
                    System.Windows.MessageBox.Show(System.Windows.Application.Current.MainWindow, "No barcode has been generated yet for this model.", "Print Warning", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Print error: {ex.Message}";
                System.Windows.MessageBox.Show(System.Windows.Application.Current.MainWindow, $"An error occurred while printing: {ex.Message}", "Print Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
    }
}
