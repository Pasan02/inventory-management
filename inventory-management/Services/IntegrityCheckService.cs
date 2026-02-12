using inventory_management.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace inventory_management.Services
{
    public class IntegrityCheckService : IIntegrityCheckService
    {
        private readonly InventoryDbContext _context;
        private readonly IDatabaseAvailabilityService _availabilityService;

        public IntegrityCheckService(InventoryDbContext context, IDatabaseAvailabilityService availabilityService)
        {
            _context = context;
            _availabilityService = availabilityService;
        }

        public async Task<IntegrityCheckResult> RunAsync()
        {
            var result = new IntegrityCheckResult();
            var availability = await _availabilityService.GetStatusAsync();
            if (!availability.IsAvailable)
            {
                result.IsHealthy = false;
                result.Issues.Add(availability.Message);
                return result;
            }

            var itemsWithoutStock = await _context.Items
                .AsNoTracking()
                .Where(i => !_context.Stocks.Any(s => s.ItemId == i.Id))
                .Select(i => i.Barcode)
                .ToListAsync();

            if (itemsWithoutStock.Count > 0)
            {
                result.Issues.Add($"Items missing stock records: {string.Join(", ", itemsWithoutStock.Take(10))}");
            }

            var negativeStocks = await _context.Stocks
                .AsNoTracking()
                .Where(s => s.Quantity < 0)
                .Select(s => s.ItemId)
                .ToListAsync();

            if (negativeStocks.Count > 0)
            {
                result.Issues.Add($"Negative stock quantities detected for item IDs: {string.Join(", ", negativeStocks.Take(10))}");
            }

            var orphanStocks = await _context.Stocks
                .AsNoTracking()
                .Where(s => !_context.Items.Any(i => i.Id == s.ItemId))
                .Select(s => s.ItemId)
                .ToListAsync();

            if (orphanStocks.Count > 0)
            {
                result.Issues.Add($"Orphan stock records detected for item IDs: {string.Join(", ", orphanStocks.Take(10))}");
            }

            var transactions = await _context.Transactions
                .AsNoTracking()
                .OrderByDescending(t => t.Timestamp)
                .Take(500)
                .ToListAsync();

            var invalidTransactions = new List<long>();
            foreach (var transaction in transactions)
            {
                if (StockTransactionHasher.ComputeChecksum(transaction) != transaction.ChecksumHash)
                {
                    invalidTransactions.Add(transaction.Id);
                }
            }

            if (invalidTransactions.Count > 0)
            {
                result.Issues.Add($"Checksum mismatches detected for transaction IDs: {string.Join(", ", invalidTransactions.Take(10))}");
            }

            result.IsHealthy = result.Issues.Count == 0;
            return result;
        }
    }
}
