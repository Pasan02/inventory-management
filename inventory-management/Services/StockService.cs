using inventory_management.Data;
using inventory_management.Data.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace inventory_management.Services
{
    public class StockService : IStockService
    {
        private readonly InventoryDbContext _context;
        private readonly IDatabaseAvailabilityService _availabilityService;
        private readonly IBarcodeService _barcodeService;

        public StockService(InventoryDbContext context, IDatabaseAvailabilityService availabilityService, IBarcodeService barcodeService)
        {
            _context = context;
            _availabilityService = availabilityService;
            _barcodeService = barcodeService;
        }

        public async Task<Item?> FindItemByBarcodeAsync(string barcode)
        {
            var availability = await _availabilityService.GetStatusAsync();
            if (!availability.IsAvailable)
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(barcode))
            {
                return null;
            }

            var normalized = barcode.Trim();

            return await _context.Items
                .Include(i => i.Stock)
                .Include(i => i.Rack)
                .Include(i => i.PartType)
                .Include(i => i.PartBrand)
                .Include(i => i.VehicleModel)
                .ThenInclude(m => m.Manufacturer)
                .Include(i => i.CompatibleModels)
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.Barcode == normalized);
        }

        public async Task<Item?> FindItemByBarcodeOrNameAsync(string searchText)
        {
            var availability = await _availabilityService.GetStatusAsync();
            if (!availability.IsAvailable)
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(searchText))
            {
                return null;
            }

            var normalized = searchText.Trim().ToLower();

            return await _context.Items
                .Include(i => i.Stock)
                .Include(i => i.Rack)
                .Include(i => i.PartType)
                .Include(i => i.PartBrand)
                .Include(i => i.VehicleModel)
                .ThenInclude(m => m.Manufacturer)
                .Include(i => i.CompatibleModels)
                .AsNoTracking()
                .FirstOrDefaultAsync(i => 
                    i.Barcode.ToLower() == normalized ||
                    i.PartType.Name.ToLower().Contains(normalized) ||
                    i.PartBrand.Name.ToLower().Contains(normalized) ||
                    i.VehicleModel.Name.ToLower().Contains(normalized) ||
                    i.VehicleModel.Manufacturer.Name.ToLower().Contains(normalized) ||
                    i.CompatibleModels.Any(cm => 
                        (cm.Model != null && cm.Model.ToLower().Contains(normalized)) ||
                        (cm.Manufacturer != null && cm.Manufacturer.ToLower().Contains(normalized)) ||
                        (cm.Brand != null && cm.Brand.ToLower().Contains(normalized)) ||
                        (cm.CountryOfOrigin != null && cm.CountryOfOrigin.ToLower().Contains(normalized)) ||
                        (cm.YearRange != null && cm.YearRange.ToLower().Contains(normalized))) ||
                    (i.PartType.Name + " " + i.PartBrand.Name).ToLower().Contains(normalized) ||
                    (i.PartType.Name + " " + i.VehicleModel.Name).ToLower().Contains(normalized) ||
                    (i.PartBrand.Name + " " + i.VehicleModel.Name).ToLower().Contains(normalized));
        }

        public async Task<IReadOnlyList<Item>> GetItemsAsync()
        {
            var availability = await _availabilityService.GetStatusAsync();
            if (!availability.IsAvailable)
            {
                return Array.Empty<Item>();
            }

            return await _context.Items
                .Include(i => i.Stock)
                .Include(i => i.Rack)
                .Include(i => i.PartType)
                .Include(i => i.PartBrand)
                .Include(i => i.VehicleModel)
                .ThenInclude(m => m.Manufacturer)
                .Include(i => i.CompatibleModels)
                .AsNoTracking()
                .OrderBy(i => i.Barcode)
                .ToListAsync();
        }

        public async Task<StockOperationResult> AddStockAsync(string barcode, int quantity)
        {
            if (quantity <= 0)
            {
                return new StockOperationResult { Success = false, Message = "Quantity must be greater than zero." };
            }

            return await ChangeStockAsync(barcode, quantity, "IN");
        }

        public async Task<StockOperationResult> AddStockWithPriceAsync(string barcode, int quantity, string secretPriceCode)
        {
            if (quantity <= 0)
            {
                return new StockOperationResult { Success = false, Message = "Quantity must be greater than zero." };
            }

            var availability = await _availabilityService.GetStatusAsync();
            if (!availability.IsAvailable)
            {
                return new StockOperationResult { Success = false, Message = availability.Message };
            }

            var normalizedBarcode = barcode.Trim();
            var normalizedPrice = secretPriceCode?.Trim() ?? string.Empty;

            var item = await _context.Items
                .Include(i => i.Stock)
                .Include(i => i.CompatibleModels)
                .FirstOrDefaultAsync(i => i.Barcode == normalizedBarcode);

            if (item == null)
            {
                return new StockOperationResult { Success = false, Message = "Item not found." };
            }

            var currentPrice = item.SecretPriceCode ?? string.Empty;
            var currentMonth = DateTime.UtcNow.ToString("yyyyMM");
            var itemMonth = item.RegisteredDate.ToString("yyyyMM");

            // Look for an existing item with the same definition and the target price code
            var existingItem = await _context.Items
                .Include(i => i.Stock)
                .FirstOrDefaultAsync(i => 
                    i.PartTypeId == item.PartTypeId &&
                    i.VehicleModelId == item.VehicleModelId &&
                    i.PartBrandId == item.PartBrandId &&
                    i.CountryOfOrigin == item.CountryOfOrigin &&
                    i.SecretPriceCode == normalizedPrice);

            if (existingItem != null)
            {
                if (existingItem.Id == item.Id)
                {
                    // Regular stock update on the current item
                    return await ChangeStockAsync(barcode, quantity, "IN");
                }

                // Update the stock of the existing item
                var result = await ChangeStockAsync(existingItem.Barcode, quantity, "IN");
                if (result.Success)
                {
                    return new StockOperationResult
                    {
                        Success = true,
                        Message = result.Message,
                        NewQuantity = result.NewQuantity,
                        NewBarcode = existingItem.Barcode
                    };
                }
                return result;
            }

            if (normalizedPrice != currentPrice || currentMonth != itemMonth)
            {
                // Price code is different OR registration month is different, create a duplicate item
                await using var dbTransaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // 1. Create duplicate item
                    var newItem = new Item
                    {
                        PartTypeId = item.PartTypeId,
                        PartBrandId = item.PartBrandId,
                        VehicleModelId = item.VehicleModelId,
                        CountryOfOrigin = item.CountryOfOrigin,
                        Description = item.Description,
                        ImagePath = item.ImagePath,
                        LowStockThreshold = item.LowStockThreshold,
                        RackId = item.RackId,
                        SecretPriceCode = normalizedPrice,
                        RegisteredDate = DateTime.UtcNow,
                        Barcode = "TEMP-" + Guid.NewGuid().ToString().Substring(0, 8)
                    };

                    _context.Items.Add(newItem);
                    await _context.SaveChangesAsync();

                    // 2. Generate real barcode
                    newItem.Barcode = _barcodeService.GenerateBarcodeString(newItem.Id);

                    // 3. Copy compatible models
                    foreach (var cm in item.CompatibleModels)
                    {
                        _context.ItemCompatibleModels.Add(new ItemCompatibleModel
                        {
                            ItemId = newItem.Id,
                            Manufacturer = cm.Manufacturer,
                            Model = cm.Model,
                            YearRange = cm.YearRange,
                            Brand = cm.Brand,
                            CountryOfOrigin = cm.CountryOfOrigin
                        });
                    }

                    // 4. Create Stock record
                    var initialStock = new Stock
                    {
                        ItemId = newItem.Id,
                        Quantity = quantity,
                        LastUpdated = DateTime.UtcNow
                    };
                    _context.Stocks.Add(initialStock);

                    // 5. Create transaction log
                    var transaction = new StockTransaction
                    {
                        ItemId = newItem.Id,
                        ActionType = "IN",
                        QuantityChange = quantity,
                        Timestamp = DateTime.UtcNow,
                        MachineName = Environment.MachineName
                    };
                    transaction.ChecksumHash = StockTransactionHasher.ComputeChecksum(transaction);
                    _context.Transactions.Add(transaction);

                    await _context.SaveChangesAsync();
                    await WriteMirrorLogAsync(transaction, newItem.Barcode, quantity);
                    await dbTransaction.CommitAsync();

                    return new StockOperationResult
                    {
                        Success = true,
                        Message = $"New batch created due to price difference. New barcode generated: {newItem.Barcode}",
                        NewQuantity = quantity,
                        NewBarcode = newItem.Barcode
                    };
                }
                catch (Exception ex)
                {
                    await dbTransaction.RollbackAsync();
                    return new StockOperationResult { Success = false, Message = $"Error creating new batch: {ex.Message}" };
                }
            }
            else
            {
                // Price code is identical, just run regular stock update
                return await ChangeStockAsync(barcode, quantity, "IN");
            }
        }

        public async Task<StockOperationResult> RemoveStockAsync(string barcode, int quantity)
        {
            if (quantity <= 0)
            {
                return new StockOperationResult { Success = false, Message = "Quantity must be greater than zero." };
            }

            return await ChangeStockAsync(barcode, -quantity, "OUT");
        }

        private async Task<StockOperationResult> ChangeStockAsync(string barcode, int quantityChange, string actionType)
        {
            var availability = await _availabilityService.GetStatusAsync();
            if (!availability.IsAvailable)
            {
                return new StockOperationResult { Success = false, Message = availability.Message };
            }

            if (string.IsNullOrWhiteSpace(barcode))
            {
                return new StockOperationResult { Success = false, Message = "Barcode is required." };
            }

            var normalized = barcode.Trim();

            await using var dbTransaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var item = await _context.Items
                    .Include(i => i.Stock)
                    .FirstOrDefaultAsync(i => i.Barcode == normalized);

                if (item == null)
                {
                    return new StockOperationResult { Success = false, Message = "Item not found." };
                }

                if (item.Stock == null)
                {
                    item.Stock = new Stock { ItemId = item.Id, Quantity = 0, LastUpdated = DateTime.UtcNow };
                    _context.Stocks.Add(item.Stock);
                }

                var newQuantity = item.Stock.Quantity + quantityChange;
                if (newQuantity < 0)
                {
                    return new StockOperationResult { Success = false, Message = "Insufficient stock. Operation cancelled." };
                }

                var timestamp = DateTime.UtcNow;
                item.Stock.Quantity = newQuantity;
                item.Stock.LastUpdated = timestamp;

                var transaction = new StockTransaction
                {
                    ItemId = item.Id,
                    ActionType = actionType,
                    QuantityChange = quantityChange,
                    Timestamp = timestamp,
                    MachineName = Environment.MachineName
                };

                transaction.ChecksumHash = StockTransactionHasher.ComputeChecksum(transaction);

                if (newQuantity == 0)
                {
                    // MUST write physical mirror log before db records are deleted
                    await WriteMirrorLogAsync(transaction, item.Barcode, newQuantity);

                    // Delete transactions
                    var txs = await _context.Transactions.Where(t => t.ItemId == item.Id).ToListAsync();
                    _context.Transactions.RemoveRange(txs);

                    // Delete compatibility models
                    var comps = await _context.ItemCompatibleModels.Where(c => c.ItemId == item.Id).ToListAsync();
                    _context.ItemCompatibleModels.RemoveRange(comps);

                    // Delete stock
                    _context.Stocks.Remove(item.Stock);

                    // Delete item
                    _context.Items.Remove(item);
                }
                else
                {
                    _context.Transactions.Add(transaction);
                    await WriteMirrorLogAsync(transaction, item.Barcode, newQuantity);
                }

                await _context.SaveChangesAsync();

                await dbTransaction.CommitAsync();

                return new StockOperationResult
                {
                    Success = true,
                    Message = newQuantity == 0 
                        ? "Stock reached 0. Item and all related records have been deleted from the database." 
                        : "Stock updated successfully.",
                    NewQuantity = newQuantity
                };
            }
            catch (Exception ex)
            {
                await dbTransaction.RollbackAsync();
                return new StockOperationResult { Success = false, Message = $"Error updating stock: {ex.Message}" };
            }
        }

        private static async Task WriteMirrorLogAsync(StockTransaction transaction, string barcode, int newQuantity)
        {
            var basePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var logFolder = Path.Combine(basePath, "InventoryManagement", "logs");
            Directory.CreateDirectory(logFolder);

            var logPath = Path.Combine(logFolder, "stock-transactions.log");

            var line = string.Join("|",
                transaction.Timestamp.ToString("O"),
                transaction.ItemId,
                barcode,
                transaction.ActionType,
                transaction.QuantityChange,
                newQuantity,
                transaction.MachineName,
                transaction.ChecksumHash);

            await File.AppendAllTextAsync(logPath, line + Environment.NewLine);
        }
    }
}
