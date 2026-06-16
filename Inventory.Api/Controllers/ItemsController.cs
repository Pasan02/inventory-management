using inventory_management.Data;
using inventory_management.Data.Entities;
using inventory_management.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ItemsController : ControllerBase
    {
        private readonly InventoryDbContext _context;
        private readonly IBarcodeService _barcodeService;

        public ItemsController(InventoryDbContext context, IBarcodeService barcodeService)
        {
            _context = context;
            _barcodeService = barcodeService;
        }

        [HttpGet("reference-data")]
        public async Task<IActionResult> GetReferenceData()
        {
            var partTypes = await _context.PartTypes.OrderBy(t => t.Name).Select(t => new { t.Id, t.Name }).ToListAsync();
            var brands = await _context.PartBrands.OrderBy(b => b.Name).Select(b => new { t = b.Id, n = b.Name }).Select(b => new { Id = b.t, Name = b.n }).ToListAsync();
            var manufacturers = await _context.Manufacturers.OrderBy(m => m.Name).Select(m => new { m.Id, m.Name }).ToListAsync();
            var racks = await _context.Racks.OrderBy(r => r.LocationCode).Select(r => new { r.Id, r.LocationCode }).ToListAsync();
            
            return Ok(new {
                partTypes,
                brands,
                manufacturers,
                racks
            });
        }

        [HttpGet("models/{manufacturerId}")]
        public async Task<IActionResult> GetModels(int manufacturerId)
        {
            var models = await _context.Models
                .Where(m => m.VehicleManufacturerId == manufacturerId)
                .OrderBy(m => m.Name)
                .Select(m => new { m.Id, m.Name, m.YearRange })
                .ToListAsync();
            return Ok(models);
        }

        public class CreateItemRequest
        {
            public int PartTypeId { get; set; }
            public int PartBrandId { get; set; }
            public int VehicleModelId { get; set; }
            public string CountryOfOrigin { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public int LowStockThreshold { get; set; }
            public int? RackId { get; set; }
            public string? CustomBarcode { get; set; }
            public string? SecretPriceCode { get; set; }
            public List<CompatibleModelDto> CompatibleModels { get; set; } = new();
        }

        public class CompatibleModelDto
        {
            public string? Manufacturer { get; set; }
            public string? Model { get; set; }
            public string? YearRange { get; set; }
            public string? Brand { get; set; }
            public string? CountryOfOrigin { get; set; }
        }

        [HttpPost]
        public async Task<IActionResult> CreateItem([FromBody] CreateItemRequest request)
        {
            try
            {
                string barcodeToUse = request.CustomBarcode?.Trim() ?? string.Empty;
                bool isCustom = !string.IsNullOrWhiteSpace(barcodeToUse);

                if (isCustom)
                {
                    var exists = await _context.Items.AnyAsync(i => i.Barcode == barcodeToUse);
                    if (exists)
                    {
                        return BadRequest(new { message = "This custom barcode is currently in use." });
                    }
                }
                else
                {
                    barcodeToUse = "TEMP-" + Guid.NewGuid().ToString().Substring(0, 8);
                }

                var newItem = new Item
                {
                    PartTypeId = request.PartTypeId,
                    PartBrandId = request.PartBrandId,
                    VehicleModelId = request.VehicleModelId,
                    CountryOfOrigin = request.CountryOfOrigin,
                    Description = request.Description,
                    LowStockThreshold = request.LowStockThreshold,
                    RackId = request.RackId,
                    Barcode = barcodeToUse,
                    SecretPriceCode = request.SecretPriceCode?.Trim() ?? string.Empty,
                    RegisteredDate = DateTime.UtcNow
                };

                _context.Items.Add(newItem);
                await _context.SaveChangesAsync();

                if (!isCustom)
                {
                    newItem.Barcode = _barcodeService.GenerateBarcodeString(newItem.Id);
                }

                foreach (var comp in request.CompatibleModels)
                {
                    _context.ItemCompatibleModels.Add(new ItemCompatibleModel
                    {
                        ItemId = newItem.Id,
                        Manufacturer = comp.Manufacturer,
                        Model = comp.Model,
                        YearRange = comp.YearRange,
                        Brand = comp.Brand,
                        CountryOfOrigin = comp.CountryOfOrigin
                    });
                }

                var initialStock = new Stock
                {
                    ItemId = newItem.Id,
                    Quantity = 0,
                    LastUpdated = DateTime.UtcNow
                };
                _context.Stocks.Add(initialStock);

                await _context.SaveChangesAsync();

                return Ok(new { message = "Item created successfully", barcode = newItem.Barcode });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while saving the item." });
            }
        }
    }
}
