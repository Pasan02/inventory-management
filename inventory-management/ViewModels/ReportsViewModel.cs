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
                    .AsNoTracking()
                    .ToListAsync();

                foreach (var item in items.OrderBy(i => i.Barcode))
                {
                    var row = new ReportItemRow
                    {
                        Barcode = item.Barcode,
                        Description = item.Description,
                        PartType = item.PartType.Name,
                        Brand = item.PartBrand.Name,
                        Manufacturer = item.VehicleModel.Manufacturer.Name,
                        Model = item.VehicleModel.Name,
                        CountryOfOrigin = item.CountryOfOrigin,
                        Rack = item.Rack?.LocationCode ?? string.Empty,
                        Quantity = item.Stock?.Quantity ?? 0,
                        LowStockThreshold = item.LowStockThreshold
                    };

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
