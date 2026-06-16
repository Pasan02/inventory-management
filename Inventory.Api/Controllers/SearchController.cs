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

        [HttpGet("images")]
        [AllowAnonymous]
        public IActionResult GetImage([FromQuery] string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return BadRequest();
            try
            {
                var fullPath = inventory_management.Services.AssetPathService.GetAbsolutePath(path);
                if (System.IO.File.Exists(fullPath))
                {
                    var provider = new Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider();
                    if (!provider.TryGetContentType(fullPath, out var contentType))
                    {
                        contentType = "application/octet-stream";
                    }
                    return PhysicalFile(fullPath, contentType);
                }
            }
            catch { /* ignore */ }
            return NotFound();
        }

        [HttpGet("parts")]
        public async Task<IActionResult> GetParts()
        {
            var parts = await _context.Items
                .Include(i => i.PartType)
                .AsNoTracking()
                .GroupBy(i => new { i.PartTypeId, i.PartType.Name, i.PartType.ImagePath, i.PartType.Image })
                .Select(g => new
                {
                    PartTypeId = g.Key.PartTypeId,
                    Name = g.Key.Name,
                    ItemCount = g.Count(),
                    ImagePath = g.Key.ImagePath,
                    ImageUrl = g.Key.ImagePath != null ? $"/api/search/images?path={Uri.EscapeDataString(g.Key.ImagePath)}" : null
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
                .GroupBy(i => new { i.VehicleModel.VehicleManufacturerId, i.VehicleModel.Manufacturer.Name, i.VehicleModel.Manufacturer.LogoPath, i.VehicleModel.Manufacturer.Logo })
                .Select(g => new
                {
                    ManufacturerId = g.Key.VehicleManufacturerId,
                    Name = g.Key.Name,
                    ItemCount = g.Count(),
                    LogoPath = g.Key.LogoPath,
                    LogoUrl = g.Key.LogoPath != null ? $"/api/search/images?path={Uri.EscapeDataString(g.Key.LogoPath)}" : null
                })
                .OrderBy(r => r.Name)
                .ToListAsync();

            return Ok(manufacturers);
        }

        [HttpGet("models/{partTypeId}/{manufacturerId}")]
        public async Task<IActionResult> GetModels(int partTypeId, int manufacturerId)
        {
            var dbModels = await _context.Items
                .Where(i => i.PartTypeId == partTypeId && i.VehicleModel.VehicleManufacturerId == manufacturerId && i.VehicleModelId != 0)
                .Include(i => i.VehicleModel)
                .Include(i => i.Stock)
                .AsNoTracking()
                .GroupBy(i => new { i.VehicleModelId, Name = i.VehicleModel != null ? i.VehicleModel.Name : null, YearRange = i.VehicleModel != null ? i.VehicleModel.YearRange : null })
                .Select(g => new
                {
                    ModelId = g.Key.VehicleModelId,
                    Name = g.Key.Name ?? "Universal / Generic",
                    YearRange = g.Key.YearRange ?? "-",
                    ItemCount = g.Count(),
                    Quantity = g.Sum(i => i.Stock != null ? i.Stock.Quantity : 0),
                    ImagePath = g.Max(i => i.ImagePath) // Max is safe for strings in GroupBy
                })
                .OrderBy(r => r.Name)
                .ToListAsync();

            var models = dbModels.Select(m => new
            {
                m.ModelId,
                m.Name,
                m.YearRange,
                m.ItemCount,
                m.Quantity,
                ImageUrl = m.ImagePath != null ? $"/api/search/images?path={Uri.EscapeDataString(m.ImagePath)}" : null
            }).ToList();

            return Ok(models);
        }

        [HttpGet("items/{modelId}")]
        public async Task<IActionResult> GetItemsByModel(int modelId, [FromQuery] int partTypeId)
        {
            var dbItems = await _context.Items
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
                    RackLocation = i.Rack != null ? i.Rack.LocationCode : "Unassigned",
                    CountryOfOrigin = i.CountryOfOrigin,
                    SecretPriceCode = i.SecretPriceCode,
                    RegisteredDate = i.RegisteredDate,
                    ImagePath = i.ImagePath
                })
                .OrderBy(i => i.Description)
                .ToListAsync();

            var items = dbItems.Select(i => new
            {
                i.Id,
                i.Description,
                i.Barcode,
                i.BrandName,
                i.Quantity,
                i.RackLocation,
                i.CountryOfOrigin,
                i.SecretPriceCode,
                i.RegisteredDate,
                ImageUrl = i.ImagePath != null ? $"/api/search/images?path={Uri.EscapeDataString(i.ImagePath)}" : null
            }).ToList();

            return Ok(items);
        }

        [HttpGet("items/part/{partTypeId}")]
        public async Task<IActionResult> GetItemsByPartType(int partTypeId)
        {
            var dbItems = await _context.Items
                .Where(i => i.PartTypeId == partTypeId)
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
                    RackLocation = i.Rack != null ? i.Rack.LocationCode : "Unassigned",
                    CountryOfOrigin = i.CountryOfOrigin,
                    SecretPriceCode = i.SecretPriceCode,
                    RegisteredDate = i.RegisteredDate,
                    ImagePath = i.ImagePath
                })
                .OrderBy(i => i.Description)
                .ToListAsync();

            var items = dbItems.Select(i => new
            {
                i.Id,
                i.Description,
                i.Barcode,
                i.BrandName,
                i.Quantity,
                i.RackLocation,
                i.CountryOfOrigin,
                i.SecretPriceCode,
                i.RegisteredDate,
                ImageUrl = i.ImagePath != null ? $"/api/search/images?path={Uri.EscapeDataString(i.ImagePath)}" : null
            }).ToList();

            return Ok(items);
        }

        [HttpGet("items/manufacturer/{manufacturerId}")]
        public async Task<IActionResult> GetItemsByManufacturer(int manufacturerId, [FromQuery] int partTypeId)
        {
            var dbItems = await _context.Items
                .Where(i => i.VehicleModel.VehicleManufacturerId == manufacturerId && i.PartTypeId == partTypeId)
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
                    RackLocation = i.Rack != null ? i.Rack.LocationCode : "Unassigned",
                    CountryOfOrigin = i.CountryOfOrigin,
                    SecretPriceCode = i.SecretPriceCode,
                    RegisteredDate = i.RegisteredDate,
                    ImagePath = i.ImagePath
                })
                .OrderBy(i => i.Description)
                .ToListAsync();

            var items = dbItems.Select(i => new
            {
                i.Id,
                i.Description,
                i.Barcode,
                i.BrandName,
                i.Quantity,
                i.RackLocation,
                i.CountryOfOrigin,
                i.SecretPriceCode,
                i.RegisteredDate,
                ImageUrl = i.ImagePath != null ? $"/api/search/images?path={Uri.EscapeDataString(i.ImagePath)}" : null
            }).ToList();

            return Ok(items);
        }

        [HttpGet("autocomplete/{query}")]
        public async Task<IActionResult> Autocomplete(string query)
        {
            query = query.ToLower();
            var items = await _context.Items
                .Where(i => EF.Functions.ILike(i.Barcode, $"%{query}%") || EF.Functions.ILike(i.Description, $"%{query}%"))
                .Take(20)
                .Select(i => new { barcode = i.Barcode, description = i.Description })
                .ToListAsync();
            return Ok(items);
        }
    }
}
