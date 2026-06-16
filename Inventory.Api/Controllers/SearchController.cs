using inventory_management.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SearchController : ControllerBase
    {
        private readonly InventoryDbContext _context;

        public SearchController(InventoryDbContext context)
        {
            _context = context;
        }

        [HttpGet("parts")]
        public async Task<IActionResult> GetParts()
        {
            var parts = await _context.Items
                .Include(i => i.PartType)
                .Include(i => i.Stock)
                .AsNoTracking()
                .GroupBy(i => new { i.PartTypeId, i.PartType.Name, i.PartType.ImagePath, i.PartType.Image })
                .Select(g => new
                {
                    PartTypeId = g.Key.PartTypeId,
                    Name = g.Key.Name,
                    ItemCount = g.Count(),
                    Quantity = g.Sum(i => i.Stock != null ? i.Stock.Quantity : 0),
                    ImagePath = g.Key.ImagePath
                })
                .OrderBy(r => r.Name)
                .ToListAsync();

            return Ok(parts);
        }

        [HttpGet("manufacturers/{partTypeId}")]
        public async Task<IActionResult> GetManufacturers(int partTypeId)
        {
            var manufacturers = await _context.Items
                .Where(i => i.PartTypeId == partTypeId)
                .Include(i => i.VehicleModel)
                .ThenInclude(m => m.Manufacturer)
                .AsNoTracking()
                .GroupBy(i => new { i.VehicleModel.VehicleManufacturerId, i.VehicleModel.Manufacturer.Name, i.VehicleModel.Manufacturer.LogoPath })
                .Select(g => new
                {
                    ManufacturerId = g.Key.VehicleManufacturerId,
                    Name = g.Key.Name,
                    ItemCount = g.Count(),
                    LogoPath = g.Key.LogoPath
                })
                .OrderBy(r => r.Name)
                .ToListAsync();

            return Ok(manufacturers);
        }

        [HttpGet("models/{partTypeId}/{manufacturerId}")]
        public async Task<IActionResult> GetModels(int partTypeId, int manufacturerId)
        {
            var models = await _context.Items
                .Where(i => i.PartTypeId == partTypeId && i.VehicleModel.VehicleManufacturerId == manufacturerId)
                .Include(i => i.VehicleModel)
                .AsNoTracking()
                .GroupBy(i => new { i.VehicleModelId, i.VehicleModel.Name, i.VehicleModel.YearRange })
                .Select(g => new
                {
                    ModelId = g.Key.VehicleModelId,
                    Name = g.Key.Name,
                    YearRange = g.Key.YearRange,
                    ItemCount = g.Count()
                })
                .OrderBy(r => r.Name)
                .ToListAsync();

            return Ok(models);
        }

        [HttpGet("items/{modelId}")]
        public async Task<IActionResult> GetItemsByModel(int modelId, [FromQuery] int partTypeId)
        {
            var items = await _context.Items
                .Where(i => i.VehicleModelId == modelId && i.PartTypeId == partTypeId)
                .Include(i => i.PartBrand)
                .Include(i => i.Stock)
                .Include(i => i.Rack)
                .AsNoTracking()
                .Select(i => new
                {
                    Id = i.Id,
                    Description = i.Description,
                    Barcode = i.Barcode,
                    BrandName = i.PartBrand.Name,
                    Quantity = i.Stock != null ? i.Stock.Quantity : 0,
                    RackLocation = i.Rack != null ? i.Rack.LocationCode : "Unassigned"
                })
                .OrderBy(i => i.Description)
                .ToListAsync();

            return Ok(items);
        }

        [HttpGet("autocomplete/{query}")]
        public async Task<IActionResult> Autocomplete(string query)
        {
            query = query.ToLower();
            var items = await _context.Items
                .Where(i => i.Barcode.ToLower().Contains(query) || i.Description.ToLower().Contains(query))
                .Take(20)
                .Select(i => new { barcode = i.Barcode, description = i.Description })
                .ToListAsync();
            return Ok(items);
        }
    }
}
