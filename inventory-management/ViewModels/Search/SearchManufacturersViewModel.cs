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
            var availability = await _availabilityService.GetStatusAsync();
            if (!availability.IsAvailable)
            {
                StatusMessage = availability.Message;
                return;
            }

            StatusMessage = "Loading manufacturers...";
            Manufacturers.Clear();

            var rows = await _context.Items
                .Include(i => i.VehicleModel)
                .ThenInclude(m => m.Manufacturer)
                .Include(i => i.Stock)
                .AsNoTracking()
                .Where(i => i.PartTypeId == Part.PartTypeId)
                .GroupBy(i => new { i.VehicleModel.VehicleManufacturerId, i.VehicleModel.Manufacturer.Name })
                .Select(g => new ManufacturerSearchRow
                {
                    ManufacturerId = g.Key.VehicleManufacturerId,
                    Name = g.Key.Name,
                    ItemCount = g.Count(),
                    Quantity = g.Sum(i => i.Stock != null ? i.Stock.Quantity : 0),
                    LogoPath = g.Select(i => i.VehicleModel.Manufacturer.LogoPath).FirstOrDefault()
                })
                .OrderBy(r => r.Name)
                .ToListAsync();

            foreach (var row in rows)
            {
                Manufacturers.Add(row);
            }

            StatusMessage = Manufacturers.Count == 0 ? "No manufacturers found." : $"{Manufacturers.Count} manufacturers available.";
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
