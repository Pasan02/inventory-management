using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using inventory_management.Data;
using inventory_management.Services;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace inventory_management.ViewModels.Search
{
    public partial class SearchManufacturersViewModel : ViewModelBase
    {
        private readonly InventoryDbContext _context;
        private readonly IDatabaseAvailabilityService _availabilityService;
        private readonly System.Action<ManufacturerSearchRow> _selectManufacturer;
        private readonly System.Action _goBack;

        public PartTypeSearchRow Part { get; }
        public ObservableCollection<ManufacturerSearchRow> Manufacturers { get; } = new();

        private string _statusMessage = "Ready";
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public SearchManufacturersViewModel(InventoryDbContext context, IDatabaseAvailabilityService availabilityService, PartTypeSearchRow part, System.Action<ManufacturerSearchRow> selectManufacturer, System.Action goBack)
        {
            _context = context;
            _availabilityService = availabilityService;
            Part = part;
            _selectManufacturer = selectManufacturer;
            _goBack = goBack;
            _ = LoadManufacturersAsync();
        }

        [RelayCommand]
        private async Task LoadManufacturersAsync()
        {
            try
            {
                var availability = await _availabilityService.GetStatusAsync();
                if (!availability.IsAvailable)
                {
                    StatusMessage = availability.Message;
                    return;
                }

                StatusMessage = "Loading manufacturers...";
                Manufacturers.Clear();

                // Get manufacturer IDs that have items with this part type
                var manufacturerIds = await _context.Items
                    .Include(i => i.VehicleModel)
                .ThenInclude(m => m.Manufacturer)
                .Include(i => i.Stock)
                    .AsNoTracking()
                    .Where(i => i.PartTypeId == Part.PartTypeId)
                    .Select(i => i.VehicleModel.VehicleManufacturerId)
                    .Distinct()
                    .ToListAsync();

                foreach (var manufacturerId in manufacturerIds)
                {
                    // Get manufacturer info
                    var manufacturer = await _context.Manufacturers
                        .AsNoTracking()
                        .FirstOrDefaultAsync(m => m.Id == manufacturerId);

                    if (manufacturer == null) continue;

                    // Count items and calculate quantity
                    var items = await _context.Items
                        .Include(i => i.VehicleModel)
                        .Include(i => i.Stock)
                        .AsNoTracking()
                        .Where(i => i.PartTypeId == Part.PartTypeId && i.VehicleModel.VehicleManufacturerId == manufacturerId)
                        .ToListAsync();

                    var row = new ManufacturerSearchRow
                    {
                        ManufacturerId = manufacturerId,
                        Name = manufacturer.Name,
                        ItemCount = items.Count,
                        Quantity = items.Sum(i => i.Stock?.Quantity ?? 0),
                        LogoPath = manufacturer.LogoPath,
                        Logo = manufacturer.Logo
                    };

                    Manufacturers.Add(row);
                }

                // Sort by name
                var sorted = Manufacturers.OrderBy(m => m.Name).ToList();
                Manufacturers.Clear();
                foreach (var mfr in sorted)
                {
                    Manufacturers.Add(mfr);
                }

                StatusMessage = Manufacturers.Count == 0 ? "No manufacturers found." : $"{Manufacturers.Count} manufacturers available.";
            }
            catch (System.Exception ex)
            {
                StatusMessage = $"Error loading manufacturers: {ex.Message}";
            }
        }

        [RelayCommand]
        private void SelectManufacturer(ManufacturerSearchRow manufacturer)
        {
            _selectManufacturer(manufacturer);
        }

        [RelayCommand]
        private void Back()
        {
            _goBack();
        }
    }
}
