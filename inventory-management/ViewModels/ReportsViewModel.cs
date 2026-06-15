using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using inventory_management.Data;
using inventory_management.Services;
using inventory_management.Views;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace inventory_management.ViewModels
{
    public partial class ReportsViewModel : ViewModelBase
    {
        private readonly InventoryDbContext _context;
        private readonly IDatabaseAvailabilityService _availabilityService;
        private readonly IPdfService _pdfService;

        public ObservableCollection<ReportItemRow> StockSnapshot { get; } = new();
        public ObservableCollection<ReportItemRow> LowStockItems { get; } = new();
        public ObservableCollection<ReportTransactionRow> RecentTransactions { get; } = new();
        public ObservableCollection<ReportOrderRow> RecentRemovedItems { get; } = new();
        public ObservableCollection<ReportOrderRow> OrderedItems { get; } = new();

        private string _statusMessage = "Ready";
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        private bool _selectAllRecentRemoved;
        public bool SelectAllRecentRemoved
        {
            get => _selectAllRecentRemoved;
            set
            {
                if (SetProperty(ref _selectAllRecentRemoved, value))
                {
                    foreach (var item in RecentRemovedItems)
                    {
                        item.IsSelected = value;
                    }
                }
            }
        }

        private bool _selectAllOrdered;
        public bool SelectAllOrdered
        {
            get => _selectAllOrdered;
            set
            {
                if (SetProperty(ref _selectAllOrdered, value))
                {
                    foreach (var item in OrderedItems)
                    {
                        item.IsSelected = value;
                    }
                }
            }
        }

        public ReportsViewModel(InventoryDbContext context, IDatabaseAvailabilityService availabilityService, IPdfService pdfService)
        {
            _context = context;
            _availabilityService = availabilityService;
            _pdfService = pdfService;
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
                RecentRemovedItems.Clear();
                OrderedItems.Clear();

                var items = await _context.Items
                    .Include(i => i.Stock)
                    .Include(i => i.Rack)
                    .Include(i => i.PartType)
                    .Include(i => i.PartBrand)
                    .Include(i => i.VehicleModel)
                        .ThenInclude(m => m.Manufacturer)
                    .Include(i => i.CompatibleModels)
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
                            .Select(cm => cm.ToString())
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

                // Load Order Tracking items
                var orderTrackings = await _context.OrderTrackings
                    .Include(o => o.Item)
                        .ThenInclude(i => i.PartType)
                    .Include(o => o.Item)
                        .ThenInclude(i => i.PartBrand)
                    .Include(o => o.Item)
                        .ThenInclude(i => i.VehicleModel)
                            .ThenInclude(m => m.Manufacturer)
                    .Include(o => o.Item)
                        .ThenInclude(i => i.Rack)
                    .OrderByDescending(o => o.CreatedAt)
                    .ToListAsync();

                var pendingGrouped = orderTrackings
                    .Where(o => o.Status == "Pending")
                    .GroupBy(o => new
                    {
                        o.Item.PartTypeId,
                        o.Item.PartBrandId,
                        o.Item.VehicleModelId,
                        CountryOfOrigin = o.Item.CountryOfOrigin ?? string.Empty,
                        RackId = o.Item.RackId ?? 0
                    })
                    .Select(g =>
                    {
                        var first = g.First();
                        var barcodes = string.Join(", ", g.Select(o => o.Item.Barcode).Distinct().OrderBy(b => b));
                        var totalQuantity = g.Sum(o => o.Quantity);
                        var orderIds = g.Select(o => o.Id).ToList();

                        return new ReportOrderRow
                        {
                            Id = first.Id, // Representative ID
                            OrderIds = orderIds,
                            ItemId = first.ItemId,
                            Barcode = barcodes,
                            Description = first.Item.Description,
                            PartType = first.Item.PartType.Name,
                            Brand = first.Item.PartBrand.Name,
                            Manufacturer = first.Item.VehicleModel.Manufacturer.Name,
                            Model = first.Item.VehicleModel.Name,
                            CountryOfOrigin = first.Item.CountryOfOrigin,
                            Rack = first.Item.Rack?.LocationCode ?? string.Empty,
                            Quantity = totalQuantity,
                            Status = "Pending",
                            CreatedAt = g.Max(o => o.CreatedAt)
                        };
                    })
                    .OrderByDescending(r => r.CreatedAt)
                    .ToList();

                foreach (var row in pendingGrouped)
                {
                    RecentRemovedItems.Add(row);
                }

                var orderedGrouped = orderTrackings
                    .Where(o => o.Status == "Ordered")
                    .GroupBy(o => new
                    {
                        o.Item.PartTypeId,
                        o.Item.PartBrandId,
                        o.Item.VehicleModelId,
                        CountryOfOrigin = o.Item.CountryOfOrigin ?? string.Empty,
                        RackId = o.Item.RackId ?? 0
                    })
                    .Select(g =>
                    {
                        var first = g.First();
                        var barcodes = string.Join(", ", g.Select(o => o.Item.Barcode).Distinct().OrderBy(b => b));
                        var totalQuantity = g.Sum(o => o.Quantity);
                        var orderIds = g.Select(o => o.Id).ToList();

                        return new ReportOrderRow
                        {
                            Id = first.Id,
                            OrderIds = orderIds,
                            ItemId = first.ItemId,
                            Barcode = barcodes,
                            Description = first.Item.Description,
                            PartType = first.Item.PartType.Name,
                            Brand = first.Item.PartBrand.Name,
                            Manufacturer = first.Item.VehicleModel.Manufacturer.Name,
                            Model = first.Item.VehicleModel.Name,
                            CountryOfOrigin = first.Item.CountryOfOrigin,
                            Rack = first.Item.Rack?.LocationCode ?? string.Empty,
                            Quantity = totalQuantity,
                            Status = "Ordered",
                            CreatedAt = g.Max(o => o.CreatedAt),
                            OrderedAt = g.Max(o => o.OrderedAt)
                        };
                    })
                    .OrderByDescending(r => r.OrderedAt)
                    .ToList();

                foreach (var row in orderedGrouped)
                {
                    OrderedItems.Add(row);
                }

                // Reset the select all flags
                _selectAllRecentRemoved = false;
                OnPropertyChanged(nameof(SelectAllRecentRemoved));
                _selectAllOrdered = false;
                OnPropertyChanged(nameof(SelectAllOrdered));

                StatusMessage = $"Reports loaded: {StockSnapshot.Count} items.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading reports: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task PlaceOrder(ReportOrderRow row)
        {
            if (row == null) return;

            try
            {
                StatusMessage = "Placing order...";
                var dbOrders = await _context.OrderTrackings
                    .Where(o => row.OrderIds.Contains(o.Id))
                    .ToListAsync();

                foreach (var order in dbOrders)
                {
                    order.Status = "Ordered";
                    order.OrderedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
                
                // Generate PDF Order Sheet
                await GenerateOrderPdfAndOpenAsync(new List<ReportOrderRow> { row });

                await LoadReports();
                StatusMessage = "Order placed successfully.";
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(System.Windows.Application.Current.MainWindow, $"Failed to place order: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                StatusMessage = $"Error: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task PlaceSelectedOrders()
        {
            var selectedRows = RecentRemovedItems.Where(r => r.IsSelected).ToList();
            if (!selectedRows.Any())
            {
                System.Windows.MessageBox.Show(System.Windows.Application.Current.MainWindow, "Please select at least one item to order.", "No Items Selected", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            try
            {
                StatusMessage = "Placing orders...";
                var allOrderIds = selectedRows.SelectMany(r => r.OrderIds).ToList();
                var dbOrders = await _context.OrderTrackings
                    .Where(o => allOrderIds.Contains(o.Id))
                    .ToListAsync();

                foreach (var order in dbOrders)
                {
                    order.Status = "Ordered";
                    order.OrderedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                // Generate PDF Order Sheet
                await GenerateOrderPdfAndOpenAsync(selectedRows);

                await LoadReports();
                StatusMessage = "Selected orders placed successfully.";
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(System.Windows.Application.Current.MainWindow, $"Failed to place selected orders: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                StatusMessage = $"Error: {ex.Message}";
            }
        }

        private async Task GenerateOrderPdfAndOpenAsync(List<ReportOrderRow> items)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                FileName = $"Purchase_Order_{DateTime.Now.ToString("yyyyMMdd_HHmmss")}",
                DefaultExt = ".pdf",
                Filter = "PDF documents (*.pdf)|*.pdf",
                Title = "Save Purchase Order PDF"
            };

            bool? result = dialog.ShowDialog(System.Windows.Application.Current.MainWindow);
            if (result == true)
            {
                string filePath = dialog.FileName;
                StatusMessage = "Generating PDF...";
                
                bool success = await _pdfService.GenerateOrderPdfAsync(filePath, items);
                if (success)
                {
                    StatusMessage = "PDF generated successfully.";
                    try
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(filePath)
                        {
                            UseShellExecute = true
                        });
                    }
                    catch (Exception ex)
                    {
                        System.Windows.MessageBox.Show(System.Windows.Application.Current.MainWindow, 
                            $"PDF saved successfully, but failed to open it automatically: {ex.Message}", 
                            "Information", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                    }
                }
                else
                {
                    System.Windows.MessageBox.Show(System.Windows.Application.Current.MainWindow, 
                        "Failed to generate PDF document.", 
                        "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
        }

        [RelayCommand]
        private async Task ArriveSelectedOrders()
        {
            var selectedRows = OrderedItems.Where(r => r.IsSelected).ToList();
            if (!selectedRows.Any())
            {
                System.Windows.MessageBox.Show(System.Windows.Application.Current.MainWindow, "Please select at least one item to mark as arrived.", "No Items Selected", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            try
            {
                StatusMessage = "Marking items as arrived...";
                var allOrderIds = selectedRows.SelectMany(r => r.OrderIds).ToList();
                var dbOrders = await _context.OrderTrackings
                    .Where(o => allOrderIds.Contains(o.Id))
                    .ToListAsync();

                foreach (var order in dbOrders)
                {
                    order.Status = "Arrived";
                    order.ArrivedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
                await LoadReports();
                StatusMessage = "Selected items marked as arrived.";
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(System.Windows.Application.Current.MainWindow, $"Failed to mark selected items as arrived: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                StatusMessage = $"Error: {ex.Message}";
            }
        }

        [RelayCommand]
        private void ArriveOrder(ReportOrderRow row)
        {
            if (row == null) return;

            var mainVm = System.Windows.Application.Current.MainWindow.DataContext as MainViewModel;
            if (mainVm != null)
            {
                var firstBarcode = row.Barcode.Split(',')[0].Trim();
                mainVm.NavigateToAddStockWithPrepopulation(firstBarcode, row.Quantity, row.OrderIds);
            }
            else
            {
                System.Windows.MessageBox.Show(System.Windows.Application.Current.MainWindow, "Navigation failed. Main view context not found.", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
    }
}
