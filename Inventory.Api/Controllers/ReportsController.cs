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

        [HttpGet("snapshot")]
        public async Task<IActionResult> GetStockSnapshot()
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

            var snapshot = items
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
                    
                    var compatTextList = g.SelectMany(i => i.CompatibleModels)
                        .Select(cm => cm.ToString())
                        .Distinct()
                        .OrderBy(n => n)
                        .ToList();
                    var compatText = string.Join(", ", compatTextList);

                    return new 
                    {
                        Barcode = string.Join(", ", g.Select(i => i.Barcode).Distinct().OrderBy(b => b)),
                        Description = first.Description,
                        PartType = first.PartType?.Name ?? "",
                        Brand = first.PartBrand?.Name ?? "",
                        Manufacturer = first.VehicleModel?.Manufacturer?.Name ?? "",
                        Model = first.VehicleModel?.Name ?? "",
                        CountryOfOrigin = first.CountryOfOrigin,
                        Rack = first.Rack?.LocationCode ?? "",
                        Quantity = totalQuantity,
                        LowStockThreshold = first.LowStockThreshold,
                        CompatibleModelsText = string.IsNullOrEmpty(compatText) ? "None" : compatText
                    };
                })
                .OrderBy(r => r.Barcode)
                .ToList();

            return Ok(snapshot);
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
                        PartType = first.PartType?.Name ?? "",
                        Brand = first.PartBrand?.Name ?? "",
                        Manufacturer = first.VehicleModel?.Manufacturer?.Name ?? "",
                        Model = first.VehicleModel?.Name ?? "",
                        CountryOfOrigin = first.CountryOfOrigin,
                        Rack = first.Rack?.LocationCode ?? "",
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
                .Include(o => o.Item)
                    .ThenInclude(i => i.Rack)
                .Where(o => o.Status == "Pending")
                .OrderByDescending(o => o.CreatedAt)
                .AsNoTracking()
                .ToListAsync();

            var pending = orderTrackings
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
                    return new 
                    {
                        Id = first.Id,
                        ItemName = first.Item.Description,
                        Barcode = string.Join(", ", g.Select(o => o.Item.Barcode).Distinct().OrderBy(b => b)),
                        TotalQuantity = g.Sum(o => o.Quantity),
                        OrderIds = g.Select(o => o.Id).ToList(),
                        PartType = first.Item.PartType?.Name ?? "",
                        Brand = first.Item.PartBrand?.Name ?? "",
                        Manufacturer = first.Item.VehicleModel?.Manufacturer?.Name ?? "",
                        Model = first.Item.VehicleModel?.Name ?? "",
                        CountryOfOrigin = first.Item.CountryOfOrigin,
                        Rack = first.Item.Rack?.LocationCode ?? "",
                        CreatedAt = g.Max(o => o.CreatedAt)
                    };
                })
                .OrderByDescending(r => r.CreatedAt)
                .ToList();

            return Ok(pending);
        }

        [HttpGet("orders/arrived")]
        public async Task<IActionResult> GetArrivedOrders()
        {
            // "Arrived" implies "Ordered" and awaiting arrival in ReportsViewModel terminology
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
                .Where(o => o.Status == "Ordered")
                .OrderByDescending(o => o.CreatedAt)
                .AsNoTracking()
                .ToListAsync();

            var arrived = orderTrackings
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
                    return new 
                    {
                        Id = first.Id,
                        ItemName = first.Item.Description,
                        Barcode = string.Join(", ", g.Select(o => o.Item.Barcode).Distinct().OrderBy(b => b)),
                        TotalQuantity = g.Sum(o => o.Quantity),
                        OrderIds = g.Select(o => o.Id).ToList(),
                        PartType = first.Item.PartType?.Name ?? "",
                        Brand = first.Item.PartBrand?.Name ?? "",
                        Manufacturer = first.Item.VehicleModel?.Manufacturer?.Name ?? "",
                        Model = first.Item.VehicleModel?.Name ?? "",
                        CountryOfOrigin = first.Item.CountryOfOrigin,
                        Rack = first.Item.Rack?.LocationCode ?? "",
                        CreatedAt = g.Max(o => o.CreatedAt),
                        OrderedAt = g.Max(o => o.OrderedAt)
                    };
                })
                .OrderByDescending(r => r.OrderedAt)
                .ToList();

            return Ok(arrived);
        }

        [HttpGet("activity")]
        public async Task<IActionResult> GetActivityLog()
        {
            var transactions = await _context.Transactions
                .Include(t => t.Item)
                    .ThenInclude(i => i.PartType)
                .Include(t => t.Item)
                    .ThenInclude(i => i.VehicleModel)
                        .ThenInclude(vm => vm.Manufacturer)
                .AsNoTracking()
                .OrderByDescending(t => t.Timestamp)
                .Take(200)
                .Select(t => new
                {
                    Timestamp = t.Timestamp,
                    Barcode = t.Item.Barcode,
                    Description = t.Item.Description,
                    PartType = t.Item.PartType.Name ?? "",
                    Manufacturer = t.Item.VehicleModel.Manufacturer.Name ?? "",
                    Model = t.Item.VehicleModel.Name ?? "",
                    ActionType = t.ActionType,
                    QuantityChange = t.QuantityChange,
                    MachineName = t.MachineName
                })
                .ToListAsync();

            return Ok(transactions);
        }

        public class OrderActionRequest
        {
            public List<int> OrderIds { get; set; } = new List<int>();
        }

        [HttpPost("orders/place")]
        public async Task<IActionResult> PlaceOrders([FromBody] OrderActionRequest request)
        {
            if (request.OrderIds == null || !request.OrderIds.Any()) return BadRequest("No order IDs provided");

            var dbOrders = await _context.OrderTrackings
                .Where(o => request.OrderIds.Contains(o.Id))
                .ToListAsync();

            foreach (var order in dbOrders)
            {
                order.Status = "Ordered";
                order.OrderedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Orders placed successfully." });
        }

        [HttpPost("orders/arrive")]
        public async Task<IActionResult> ArriveOrders([FromBody] OrderActionRequest request)
        {
            if (request.OrderIds == null || !request.OrderIds.Any()) return BadRequest("No order IDs provided");

            var dbOrders = await _context.OrderTrackings
                .Where(o => request.OrderIds.Contains(o.Id))
                .ToListAsync();

            foreach (var order in dbOrders)
            {
                order.Status = "Arrived";
                order.ArrivedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Orders marked as arrived successfully." });
        }
    }
}
