using inventory_management.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ReportsController : ControllerBase
    {
        private readonly InventoryDbContext _context;

        public ReportsController(InventoryDbContext context)
        {
            _context = context;
        }

        [HttpGet("low-stock")]
        public async Task<IActionResult> GetLowStock()
        {
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

            var lowStock = items
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
                    var totalQuantity = g.Sum(i => i.Stock?.Quantity ?? 0);
                    
                    return new 
                    {
                        Barcode = string.Join(", ", g.Select(i => i.Barcode).Distinct().OrderBy(b => b)),
                        Description = first.Description,
                        PartType = first.PartType.Name,
                        Brand = first.PartBrand.Name,
                        Manufacturer = first.VehicleModel.Manufacturer.Name,
                        Model = first.VehicleModel.Name,
                        Quantity = totalQuantity,
                        LowStockThreshold = first.LowStockThreshold
                    };
                })
                .Where(r => r.Quantity <= r.LowStockThreshold)
                .OrderBy(r => r.Barcode)
                .ToList();

            return Ok(lowStock);
        }

        [HttpGet("orders/pending")]
        public async Task<IActionResult> GetPendingOrders()
        {
            var orderTrackings = await _context.OrderTrackings
                .Include(o => o.Item)
                    .ThenInclude(i => i.PartType)
                .Include(o => o.Item)
                    .ThenInclude(i => i.PartBrand)
                .Include(o => o.Item)
                    .ThenInclude(i => i.VehicleModel)
                        .ThenInclude(m => m.Manufacturer)
                .Where(o => o.Status == "Pending")
                .OrderByDescending(o => o.CreatedAt)
                .AsNoTracking()
                .ToListAsync();

            var pending = orderTrackings
                .GroupBy(o => new { o.ItemId })
                .Select(g =>
                {
                    var first = g.First();
                    return new 
                    {
                        ItemName = first.Item.Description,
                        Barcode = first.Item.Barcode,
                        TotalQuantity = g.Sum(o => o.Quantity),
                        OrderIds = g.Select(o => o.Id).ToList()
                    };
                })
                .ToList();

            return Ok(pending);
        }
    }
}
