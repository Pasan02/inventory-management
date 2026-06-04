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

                // Load all part types that have items
                var partTypeIds = await _context.Items
                    .Select(i => i.PartTypeId)
                    .Distinct()
                    .ToListAsync();

                foreach (var partTypeId in partTypeIds)
                {
                    // Get part type info
                    var partType = await _context.PartTypes
                        .AsNoTracking()
                        .FirstOrDefaultAsync(pt => pt.Id == partTypeId);

                    if (partType == null) continue;

                    // Count items and calculate quantity
                    var items = await _context.Items
                        .Include(i => i.Stock)
                        .AsNoTracking()
                .GroupBy(i => new { i.PartTypeId, i.PartType.Name })
                .Select(g => new PartTypeSearchRow
                {
                    PartTypeId = g.Key.PartTypeId,
                    Name = g.Key.Name,
                    ItemCount = g.Count(),
                    Quantity = g.Sum(i => i.Stock != null ? i.Stock.Quantity : 0),
                    ImagePath = g.Select(i => i.PartType.ImagePath).FirstOrDefault()
                })
                .OrderBy(r => r.Name)
                        .ToListAsync();

                    var row = new PartTypeSearchRow
                    {
                        PartTypeId = partTypeId,
                        Name = partType.Name,
                        ItemCount = items.Count,
                        Quantity = items.Sum(i => i.Quantity),
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
