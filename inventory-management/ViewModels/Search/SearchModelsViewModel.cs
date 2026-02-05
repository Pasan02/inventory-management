using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using inventory_management.Data;
using inventory_management.Services;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

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

            var rows = await _context.Items
                .Include(i => i.VehicleModel)
                .Include(i => i.Stock)
                .AsNoTracking()
                .Where(i => i.PartTypeId == Part.PartTypeId && i.VehicleModel.VehicleManufacturerId == Manufacturer.ManufacturerId)
                .GroupBy(i => new { i.VehicleModelId, i.VehicleModel.Name, i.VehicleModel.YearRange })
                .Select(g => new ModelSearchRow
                {
                    ModelId = g.Key.VehicleModelId,
                    Name = g.Key.Name,
                    YearRange = g.Key.YearRange ?? string.Empty,
                    ItemCount = g.Count(),
                    Quantity = g.Sum(i => i.Stock != null ? i.Stock.Quantity : 0)
                })
                .OrderBy(r => r.Name)
                .ToListAsync();

            foreach (var row in rows)
            {
                Models.Add(row);
            }

            StatusMessage = Models.Count == 0 ? "No models found." : $"{Models.Count} models available.";
        }

        [RelayCommand]
        private void Back()
        {
            _goBack();
        }
    }
}
