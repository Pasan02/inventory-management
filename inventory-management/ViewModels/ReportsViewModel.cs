using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using inventory_management.Data;
using inventory_management.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace inventory_management.ViewModels
{
    public partial class ReportsViewModel : ViewModelBase
    {
        private readonly InventoryDbContext _context;
        private readonly IDatabaseAvailabilityService _availabilityService;

        public ObservableCollection<ReportItemRow> StockSnapshot { get; } = new();
        public ObservableCollection<ReportItemRow> LowStockItems { get; } = new();
        public ObservableCollection<ReportTransactionRow> RecentTransactions { get; } = new();

        private string _statusMessage = "Ready";
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public ReportsViewModel(InventoryDbContext context, IDatabaseAvailabilityService availabilityService)
        {
            _context = context;
            _availabilityService = availabilityService;
            _ = LoadReports();
        }

        [RelayCommand]
        private async Task LoadReports()
        {
            var availability = await _availabilityService.GetStatusAsync();
            if (!availability.IsAvailable)
            {
                StatusMessage = availability.Message;
                return;
            }

            try
            {
                StatusMessage = "Loading reports...";
                StockSnapshot.Clear();
                LowStockItems.Clear();
                RecentTransactions.Clear();

                var items = await _context.Items
                    .Include(i => i.Stock)
                    .Include(i => i.Rack)
                    .Include(i => i.PartType)
                    .Include(i => i.PartBrand)
                    .Include(i => i.VehicleModel)
                        .ThenInclude(m => m.Manufacturer)
                    .Include(i => i.CompatibleModels)
                        .ThenInclude(cm => cm.VehicleModel)
                            .ThenInclude(vm => vm.Manufacturer)
                    .AsNoTracking()
                    .ToListAsync();

                var grouped = items
                    .GroupBy(i => new
                    {
                        i.PartTypeId,
                        i.PartBrandId,
                        i.VehicleModelId,
                        CountryOfOrigin = i.CountryOfOrigin ?? string.Empty,
                        RackId = i.RackId ?? 0
                    })
                    .Select(g =>
                    {
                        var first = g.First();
                        var barcodes = string.Join(", ", g.Select(i => i.Barcode).Distinct().OrderBy(b => b));
                        var totalQuantity = g.Sum(i => i.Stock?.Quantity ?? 0);
                        
                        var compatTextList = g.SelectMany(i => i.CompatibleModels)
                            .Select(cm => $"{cm.VehicleModel.Manufacturer.Name} {cm.VehicleModel.Name}")
                            .Distinct()
                            .OrderBy(n => n)
                            .ToList();
                        var compatText = string.Join(", ", compatTextList);

                        return new ReportItemRow
                        {
                            Barcode = barcodes,
                            Description = first.Description,
                            PartType = first.PartType.Name,
                            Brand = first.PartBrand.Name,
                            Manufacturer = first.VehicleModel.Manufacturer.Name,
                            Model = first.VehicleModel.Name,
                            CountryOfOrigin = first.CountryOfOrigin,
                            Rack = first.Rack?.LocationCode ?? string.Empty,
                            Quantity = totalQuantity,
                            LowStockThreshold = first.LowStockThreshold,
                            CompatibleModelsText = string.IsNullOrEmpty(compatText) ? "None" : compatText
                        };
                    })
                    .OrderBy(r => r.Barcode)
                    .ToList();

                foreach (var row in grouped)
                {
                    StockSnapshot.Add(row);

                    if (row.Quantity <= row.LowStockThreshold)
                    {
                        LowStockItems.Add(row);
                    }
                }

                var transactions = await _context.Transactions
                    .Include(t => t.Item)
                        .ThenInclude(i => i.PartType)
                    .Include(t => t.Item)
                        .ThenInclude(i => i.VehicleModel)
                            .ThenInclude(vm => vm.Manufacturer)
                    .AsNoTracking()
                    .OrderByDescending(t => t.Timestamp)
                    .Take(200)
                    .ToListAsync();

                foreach (var transaction in transactions)
                {
                    RecentTransactions.Add(new ReportTransactionRow
                    {
                        Timestamp = transaction.Timestamp,
                        Barcode = transaction.Item.Barcode,
                        Description = transaction.Item.Description,
                        PartType = transaction.Item.PartType?.Name ?? string.Empty,
                        Manufacturer = transaction.Item.VehicleModel?.Manufacturer?.Name ?? string.Empty,
                        Model = transaction.Item.VehicleModel?.Name ?? string.Empty,
                        ActionType = transaction.ActionType,
                        QuantityChange = transaction.QuantityChange,
                        MachineName = transaction.MachineName
                    });
                }

                StatusMessage = $"Reports loaded: {StockSnapshot.Count} items.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading reports: {ex.Message}";
            }
        }
    }
}
