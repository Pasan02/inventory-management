using inventory_management.Data.Entities;
using System.Threading.Tasks;

namespace inventory_management.Services
{
    public interface IStockService
    {
        Task<Item?> FindItemByBarcodeAsync(string barcode);
        Task<StockOperationResult> AddStockAsync(string barcode, int quantity);
        Task<StockOperationResult> RemoveStockAsync(string barcode, int quantity);
    }
}
