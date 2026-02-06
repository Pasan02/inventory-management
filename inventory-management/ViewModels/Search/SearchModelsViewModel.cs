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
        private readonly System.Action _goBack;

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

        public SearchModelsViewModel(InventoryDbContext context, IDatabaseAvailabilityService availabilityService, PartTypeSearchRow part, ManufacturerSearchRow manufacturer, System.Action goBack)
        {
            _context = context;
            _availabilityService = availabilityService;
            Part = part;
            Manufacturer = manufacturer;
            _goBack = goBack;
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
                .AsNoTracking()
                .Where(i => i.PartTypeId == Part.PartTypeId && i.VehicleModel.VehicleManufacturerId == Manufacturer.ManufacturerId)
                .ToListAsync();

            var groupedItems = items
                .GroupBy(i => i.VehicleModelId)
                .ToDictionary(g => g.Key, g => g.ToList());

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
                        PartTypeImagePath = partTypeImage,
                        ManufacturerLogoPath = manufacturerLogo
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
    }
}
