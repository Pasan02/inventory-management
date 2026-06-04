using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using inventory_management.Data;
using inventory_management.Services;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace inventory_management.ViewModels.Search
{
    public partial class SearchPartsViewModel : ViewModelBase
    {
        private readonly InventoryDbContext _context;
        private readonly IDatabaseAvailabilityService _availabilityService;
        private readonly System.Action<PartTypeSearchRow> _selectPart;

        public ObservableCollection<PartTypeSearchRow> Parts { get; } = new();

        private string _statusMessage = "Ready";
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public SearchPartsViewModel(InventoryDbContext context, IDatabaseAvailabilityService availabilityService, System.Action<PartTypeSearchRow> selectPart)
        {
            _context = context;
            _availabilityService = availabilityService;
            _selectPart = selectPart;
            _ = LoadPartsAsync();
        }

        [RelayCommand]
        private async Task LoadPartsAsync()
        {
            try
            {
                var availability = await _availabilityService.GetStatusAsync();
                if (!availability.IsAvailable)
                {
                    StatusMessage = availability.Message;
                    return;
                }

                StatusMessage = "Loading parts...";
                Parts.Clear();

                // Load all registered part types
                var partTypes = await _context.PartTypes
                    .AsNoTracking()
                    .ToListAsync();

                foreach (var partType in partTypes)
                {
                    // Count items and calculate quantity
                    var items = await _context.Items
                        .Where(i => i.PartTypeId == partType.Id)
                        .Include(i => i.Stock)
                        .AsNoTracking()
                        .ToListAsync();

                    var row = new PartTypeSearchRow
                    {
                        PartTypeId = partType.Id,
                        Name = partType.Name,
                        ItemCount = items.Count,
                        Quantity = items.Sum(i => i.Stock?.Quantity ?? 0),
                        ImagePath = partType.ImagePath,
                        Image = partType.Image
                    };

                    Parts.Add(row);
                }

                // Sort by name
                var sorted = Parts.OrderBy(p => p.Name).ToList();
                Parts.Clear();
                foreach (var part in sorted)
                {
                    Parts.Add(part);
                }

                StatusMessage = Parts.Count == 0 ? "No parts found." : $"{Parts.Count} parts available.";
            }
            catch (System.Exception ex)
            {
                StatusMessage = $"Error loading parts: {ex.Message}";
            }
        }

        [RelayCommand]
        private void SelectPart(PartTypeSearchRow part)
        {
            _selectPart(part);
        }
    }
}
