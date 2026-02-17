using inventory_management.Data.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace inventory_management.Services
{
    public interface IStockService
    {
        Task<IReadOnlyList<Item>> GetItemsAsync();
        Task<Item?> FindItemByBarcodeAsync(string barcode);
        Task<Item?> FindItemByBarcodeOrNameAsync(string searchText);
        Task<StockOperationResult> AddStockAsync(string barcode, int quantity);
        Task<StockOperationResult> RemoveStockAsync(string barcode, int quantity);
    }
}
