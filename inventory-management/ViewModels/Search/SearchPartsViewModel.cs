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
            var availability = await _availabilityService.GetStatusAsync();
            if (!availability.IsAvailable)
            {
                StatusMessage = availability.Message;
                return;
            }

            StatusMessage = "Loading parts...";
            Parts.Clear();

            var rows = await _context.Items
                .Include(i => i.PartType)
                .Include(i => i.Stock)
                .AsNoTracking()
                .GroupBy(i => new { i.PartTypeId, i.PartType.Name })
                .Select(g => new PartTypeSearchRow
                {
                    PartTypeId = g.Key.PartTypeId,
                    Name = g.Key.Name,
                    ItemCount = g.Count(),
                    Quantity = g.Sum(i => i.Stock != null ? i.Stock.Quantity : 0)
                })
                .OrderBy(r => r.Name)
                .ToListAsync();

            foreach (var row in rows)
            {
                Parts.Add(row);
            }

            StatusMessage = Parts.Count == 0 ? "No parts found." : $"{Parts.Count} parts available.";
        }

        [RelayCommand]
        private void SelectPart(PartTypeSearchRow part)
        {
            _selectPart(part);
        }
    }
}
