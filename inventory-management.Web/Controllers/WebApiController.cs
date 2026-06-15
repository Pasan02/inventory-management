using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using inventory_management.Data;
using inventory_management.Data.Entities;
using inventory_management.Services;
using System.Collections.Generic;

namespace inventory_management.Web.Controllers
{
    [ApiController]
    [Route("api")]
    public class WebApiController : ControllerBase
    {
        private readonly InventoryDbContext _context;
        private readonly IStockService _stockService;
        private readonly IAuthenticationService _authService;
        private readonly IBarcodeService _barcodeService;
        private readonly IPrintService _printService;
        private readonly IDatabaseAvailabilityService _availabilityService;

        public WebApiController(
            InventoryDbContext context,
            IStockService stockService,
            IAuthenticationService authService,
            IBarcodeService barcodeService,
            IPrintService printService,
            IDatabaseAvailabilityService availabilityService)
        {
            _context = context;
            _stockService = stockService;
            _authService = authService;
            _barcodeService = barcodeService;
            _printService = printService;
            _availabilityService = availabilityService;
        }

        // --- AUTHENTICATION ---
        [HttpPost("auth/login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new { message = "Username and password are required." });
            }

            var result = await _authService.LoginAsync(request.Username, request.Password);
            if (result.Success)
            {
                return Ok(new { success = true, username = result.User?.Username });
            }
            return Unauthorized(new { success = false, message = result.Message });
        }

        // --- LOOKUP & ITEMS ---
        [HttpGet("inventory/items")]
        public async Task<IActionResult> GetItems()
        {
            var items = await _stockService.GetItemsAsync();
            return Ok(items);
        }

        [HttpGet("inventory/search")]
        public async Task<IActionResult> Search([FromQuery] string q)
        {
            if (string.IsNullOrWhiteSpace(q))
            {
                return BadRequest(new { message = "Search query is required." });
            }
            var item = await _stockService.FindItemByBarcodeOrNameAsync(q);
            if (item == null)
            {
                return NotFound(new { message = "Item not found." });
            }
            return Ok(item);
        }

        [HttpGet("barcode/image")]
        public IActionResult GetBarcodeImage([FromQuery] string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return BadRequest(new { message = "Text is required." });
            try
            {
                var bytes = _barcodeService.GenerateBarcodeImage(text);
                if (bytes == null || bytes.Length == 0) return NotFound();
                return File(bytes, "image/png");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpGet("inventory/metadata")]
        public async Task<IActionResult> GetMetadata()
        {
            var partTypes = await _context.PartTypes.OrderBy(x => x.Name).ToListAsync();
            var brands = await _context.PartBrands.OrderBy(x => x.Name).ToListAsync();
            var manufacturers = await _context.Manufacturers.OrderBy(x => x.Name).ToListAsync();
            var models = await _context.Models.Include(m => m.Manufacturer).OrderBy(x => x.Name).ToListAsync();
            var racks = await _context.Racks.OrderBy(x => x.LocationCode).ToListAsync();

            return Ok(new
            {
                partTypes,
                brands,
                manufacturers,
                models,
                racks
            });
        }

        // --- NEW METADATA REFERENCE ENTRIES ---
        [HttpPost("metadata/part-type")]
        public async Task<IActionResult> AddPartType([FromBody] PartTypeRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name)) return BadRequest(new { message = "Name is required." });
            var name = request.Name.Trim();
            if (await _context.PartTypes.AnyAsync(x => x.Name == name)) return BadRequest(new { message = "Part type already exists." });

            var partType = new PartType { Name = name, ImagePath = request.ImagePath };
            _context.PartTypes.Add(partType);
            await _context.SaveChangesAsync();
            return Ok(partType);
        }

        [HttpPost("metadata/brand")]
        public async Task<IActionResult> AddBrand([FromBody] BrandRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name)) return BadRequest(new { message = "Name is required." });
            var name = request.Name.Trim();
            if (await _context.PartBrands.AnyAsync(x => x.Name == name)) return BadRequest(new { message = "Brand already exists." });

            var brand = new PartBrand { Name = name };
            _context.PartBrands.Add(brand);
            await _context.SaveChangesAsync();
            return Ok(brand);
        }

        [HttpPost("metadata/manufacturer")]
        public async Task<IActionResult> AddManufacturer([FromBody] ManufacturerRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name)) return BadRequest(new { message = "Name is required." });
            var name = request.Name.Trim();
            if (await _context.Manufacturers.AnyAsync(x => x.Name == name)) return BadRequest(new { message = "Manufacturer already exists." });

            var m = new VehicleManufacturer { Name = name, LogoPath = request.LogoPath };
            _context.Manufacturers.Add(m);
            await _context.SaveChangesAsync();
            return Ok(m);
        }

        [HttpPost("metadata/model")]
        public async Task<IActionResult> AddModel([FromBody] ModelRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name)) return BadRequest(new { message = "Name is required." });
            if (request.ManufacturerId <= 0) return BadRequest(new { message = "Valid Manufacturer ID is required." });

            var name = request.Name.Trim();
            if (await _context.Models.AnyAsync(x => x.Name == name && x.VehicleManufacturerId == request.ManufacturerId))
            {
                return BadRequest(new { message = "Model already exists for this manufacturer." });
            }

            var model = new VehicleModel
            {
                Name = name,
                VehicleManufacturerId = request.ManufacturerId,
                YearRange = string.IsNullOrWhiteSpace(request.YearRange) ? null : request.YearRange.Trim()
            };
            _context.Models.Add(model);
            await _context.SaveChangesAsync();
            return Ok(model);
        }

        [HttpPost("metadata/rack")]
        public async Task<IActionResult> AddRack([FromBody] RackRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.LocationCode)) return BadRequest(new { message = "Location code is required." });
            var code = request.LocationCode.Trim();
            if (await _context.Racks.AnyAsync(x => x.LocationCode == code)) return BadRequest(new { message = "Rack already exists." });

            var rack = new Rack { LocationCode = code };
            _context.Racks.Add(rack);
            await _context.SaveChangesAsync();
            return Ok(rack);
        }

        // --- CREATE ITEM ---
        [HttpPost("inventory/items/create")]
        public async Task<IActionResult> CreateItem([FromBody] CreateItemRequest request)
        {
            if (request.PartTypeId <= 0 || request.BrandId <= 0 || request.ModelId <= 0 || string.IsNullOrWhiteSpace(request.CountryOfOrigin))
            {
                return BadRequest(new { message = "PartType, Brand, Model, and Country of Origin are required fields." });
            }

            try
            {
                string barcodeToUse = request.CustomBarcode?.Trim() ?? string.Empty;
                bool isCustom = !string.IsNullOrWhiteSpace(barcodeToUse);

                if (isCustom)
                {
                    var exists = await _context.Items.AnyAsync(i => i.Barcode == barcodeToUse);
                    if (exists)
                    {
                        return BadRequest(new { message = "This custom barcode is currently in use by another active item." });
                    }
                }
                else
                {
                    barcodeToUse = "TEMP-" + Guid.NewGuid().ToString().Substring(0, 8);
                }

                var newItem = new Item
                {
                    PartTypeId = request.PartTypeId,
                    PartBrandId = request.BrandId,
                    VehicleModelId = request.ModelId,
                    CountryOfOrigin = request.CountryOfOrigin.Trim(),
                    Description = request.Description?.Trim() ?? string.Empty,
                    LowStockThreshold = request.LowStockThreshold <= 0 ? 5 : request.LowStockThreshold,
                    RackId = request.RackId > 0 ? request.RackId : null,
                    Barcode = barcodeToUse,
                    ImagePath = string.IsNullOrWhiteSpace(request.ImagePath) ? null : request.ImagePath.Trim(),
                    SecretPriceCode = request.SecretPriceCode?.Trim() ?? string.Empty,
                    RegisteredDate = DateTime.UtcNow
                };

                _context.Items.Add(newItem);
                await _context.SaveChangesAsync();

                if (!isCustom)
                {
                    newItem.Barcode = _barcodeService.GenerateBarcodeString(newItem.Id);
                }

                // Add compatibility models
                if (request.CompatibleModels != null)
                {
                    foreach (var comp in request.CompatibleModels)
                    {
                        var cm = new ItemCompatibleModel
                        {
                            ItemId = newItem.Id,
                            Manufacturer = string.IsNullOrWhiteSpace(comp.Manufacturer) ? null : comp.Manufacturer.Trim(),
                            Model = string.IsNullOrWhiteSpace(comp.Model) ? null : comp.Model.Trim(),
                            YearRange = string.IsNullOrWhiteSpace(comp.YearRange) ? null : comp.YearRange.Trim(),
                            Brand = string.IsNullOrWhiteSpace(comp.Brand) ? null : comp.Brand.Trim(),
                            CountryOfOrigin = string.IsNullOrWhiteSpace(comp.CountryOfOrigin) ? null : comp.CountryOfOrigin.Trim()
                        };
                        _context.ItemCompatibleModels.Add(cm);
                    }
                }

                var initialStock = new Stock
                {
                    ItemId = newItem.Id,
                    Quantity = 0,
                    LastUpdated = DateTime.UtcNow
                };
                _context.Stocks.Add(initialStock);

                await _context.SaveChangesAsync();

                return Ok(new { success = true, barcode = newItem.Barcode, itemId = newItem.Id });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Error creating item: {ex.Message}" });
            }
        }

        // --- STOCK OPERATIONS ---
        [HttpPost("inventory/stock/add")]
        public async Task<IActionResult> AddStock([FromBody] StockAdjustRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Barcode) || request.Quantity <= 0)
            {
                return BadRequest(new { message = "Barcode and positive quantity are required." });
            }

            StockOperationResult result;
            if (!string.IsNullOrWhiteSpace(request.SecretPriceCode))
            {
                result = await _stockService.AddStockWithPriceAsync(request.Barcode, request.Quantity, request.SecretPriceCode);
            }
            else
            {
                result = await _stockService.AddStockAsync(request.Barcode, request.Quantity);
            }

            if (result.Success)
            {
                if (request.OrderIds != null && request.OrderIds.Any())
                {
                    foreach (var id in request.OrderIds)
                    {
                        await _stockService.MarkOrderAsArrivedAsync(id);
                    }
                }

                return Ok(new
                {
                    success = true,
                    message = result.Message,
                    newQuantity = result.NewQuantity,
                    newBarcode = result.NewBarcode ?? request.Barcode
                });
            }
            return BadRequest(new { success = false, message = result.Message });
        }

        [HttpPost("inventory/stock/remove")]
        public async Task<IActionResult> RemoveStock([FromBody] StockAdjustRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Barcode) || request.Quantity <= 0)
            {
                return BadRequest(new { message = "Barcode and positive quantity are required." });
            }

            var result = await _stockService.RemoveStockAsync(request.Barcode, request.Quantity, request.IsReplacement);
            if (result.Success)
            {
                return Ok(new
                {
                    success = true,
                    message = result.Message,
                    newQuantity = result.NewQuantity
                });
            }
            return BadRequest(new { success = false, message = result.Message });
        }

        // --- REPORTS ---
        [HttpGet("reports")]
        public async Task<IActionResult> GetReports()
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

            var stockSnapshot = items
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

                    return new ReportItemRowDto
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

            var lowStock = stockSnapshot.Where(x => x.Quantity <= x.LowStockThreshold).ToList();

            var transactions = await _context.Transactions
                .Include(t => t.Item)
                    .ThenInclude(i => i.PartType)
                .Include(t => t.Item)
                    .ThenInclude(i => i.VehicleModel)
                        .ThenInclude(vm => vm.Manufacturer)
                .AsNoTracking()
                .OrderByDescending(t => t.Timestamp)
                .Take(200)
                .Select(t => new ReportTransactionRowDto
                {
                    Timestamp = t.Timestamp,
                    Barcode = t.Item.Barcode,
                    Description = t.Item.Description,
                    PartType = t.Item.PartType != null ? t.Item.PartType.Name : string.Empty,
                    Manufacturer = t.Item.VehicleModel != null && t.Item.VehicleModel.Manufacturer != null ? t.Item.VehicleModel.Manufacturer.Name : string.Empty,
                    Model = t.Item.VehicleModel != null ? t.Item.VehicleModel.Name : string.Empty,
                    ActionType = t.ActionType,
                    QuantityChange = t.QuantityChange,
                    MachineName = t.MachineName
                })
                .ToListAsync();

            // Load Order Tracking
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

            var pendingOrders = orderTrackings
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

                    return new ReportOrderRowDto
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
                        Status = "Pending",
                        CreatedAt = g.Max(o => o.CreatedAt)
                    };
                })
                .OrderByDescending(r => r.CreatedAt)
                .ToList();

            var orderedItems = orderTrackings
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

                    return new ReportOrderRowDto
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

            return Ok(new
            {
                stockSnapshot,
                lowStock,
                transactions,
                pendingOrders,
                orderedItems
            });
        }

        [HttpPost("reports/orders/place")]
        public async Task<IActionResult> PlaceOrders([FromBody] OrderIdsRequest request)
        {
            if (request?.OrderIds == null || !request.OrderIds.Any())
            {
                return BadRequest(new { message = "Order IDs are required." });
            }

            var dbOrders = await _context.OrderTrackings
                .Where(o => request.OrderIds.Contains(o.Id))
                .ToListAsync();

            foreach (var order in dbOrders)
            {
                order.Status = "Ordered";
                order.OrderedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Orders placed successfully." });
        }

        [HttpPost("reports/orders/arrive")]
        public async Task<IActionResult> ArriveOrders([FromBody] OrderIdsRequest request)
        {
            if (request?.OrderIds == null || !request.OrderIds.Any())
            {
                return BadRequest(new { message = "Order IDs are required." });
            }

            var dbOrders = await _context.OrderTrackings
                .Where(o => request.OrderIds.Contains(o.Id))
                .ToListAsync();

            foreach (var order in dbOrders)
            {
                order.Status = "Arrived";
                order.ArrivedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Orders marked as arrived." });
        }

        // --- PRINTER PROXY ---
        [HttpPost("print")]
        public async Task<IActionResult> PrintBarcode([FromBody] PrintRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Barcode) || request.Copies <= 0)
            {
                return BadRequest(new { message = "Barcode and positive copies count are required." });
            }

            var success = await _printService.PrintBarcodeLabelAsync(request.Barcode, request.Copies);
            if (success)
            {
                return Ok(new { success = true, message = "Barcode printed successfully." });
            }
            return StatusCode(500, new { success = false, message = "Printing failed. Check host printer configuration." });
        }

        // --- IMAGE UPLOAD ---
        [HttpPost("upload")]
        public async Task<IActionResult> UploadImage([FromForm] UploadRequest request)
        {
            if (request.File == null || request.File.Length == 0)
            {
                return BadRequest(new { message = "No file uploaded." });
            }

            try
            {
                var folderName = "items";
                if (!string.IsNullOrWhiteSpace(request.Category))
                {
                    var cleanCat = request.Category.Trim().ToLower();
                    if (cleanCat == "part-types" || cleanCat == "part-type") folderName = "part-types";
                    else if (cleanCat == "manufacturers" || cleanCat == "manufacturer") folderName = "manufacturers";
                }

                var assetsRoot = AssetPathService.BasePath;
                var subfolder = Path.Combine(assetsRoot, folderName);
                Directory.CreateDirectory(subfolder);

                var extension = Path.GetExtension(request.File.FileName);
                if (string.IsNullOrEmpty(extension)) extension = ".jpg"; // fallback
                var fileName = $"{folderName.TrimEnd('s')}-{Guid.NewGuid():N}{extension}";
                var relativePath = Path.Combine(folderName, fileName);
                var destinationPath = Path.Combine(assetsRoot, relativePath);

                using (var stream = new FileStream(destinationPath, FileMode.Create))
                {
                    await request.File.CopyToAsync(stream);
                }

                return Ok(new { success = true, relativePath });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Error saving image: {ex.Message}" });
            }
        }
    }

    // --- DTOs and Request Models ---
    public class LoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class PartTypeRequest { public string Name { get; set; } = string.Empty; public string? ImagePath { get; set; } }
    public class BrandRequest { public string Name { get; set; } = string.Empty; }
    public class ManufacturerRequest { public string Name { get; set; } = string.Empty; public string? LogoPath { get; set; } }
    public class ModelRequest { public string Name { get; set; } = string.Empty; public int ManufacturerId { get; set; } public string? YearRange { get; set; } }
    public class RackRequest { public string LocationCode { get; set; } = string.Empty; }

    public class CreateItemRequest
    {
        public int PartTypeId { get; set; }
        public int BrandId { get; set; }
        public int ModelId { get; set; }
        public string CountryOfOrigin { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int LowStockThreshold { get; set; }
        public int RackId { get; set; }
        public string? CustomBarcode { get; set; }
        public string? ImagePath { get; set; }
        public string? SecretPriceCode { get; set; }
        public List<CompatibilityDto>? CompatibleModels { get; set; }
    }

    public class CompatibilityDto
    {
        public string? Manufacturer { get; set; }
        public string? Model { get; set; }
        public string? YearRange { get; set; }
        public string? Brand { get; set; }
        public string? CountryOfOrigin { get; set; }
    }

    public class StockAdjustRequest
    {
        public string Barcode { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public string? SecretPriceCode { get; set; }
        public bool IsReplacement { get; set; }
        public List<int>? OrderIds { get; set; }
    }

    public class OrderIdsRequest
    {
        public List<int> OrderIds { get; set; } = new();
    }

    public class PrintRequest
    {
        public string Barcode { get; set; } = string.Empty;
        public int Copies { get; set; } = 1;
    }

    public class UploadRequest
    {
        public Microsoft.AspNetCore.Http.IFormFile? File { get; set; }
        public string? Category { get; set; }
    }

    public class ReportItemRowDto
    {
        public string Barcode { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string PartType { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public string Manufacturer { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public string CountryOfOrigin { get; set; } = string.Empty;
        public string Rack { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public int LowStockThreshold { get; set; }
        public string CompatibleModelsText { get; set; } = string.Empty;
    }

    public class ReportTransactionRowDto
    {
        public DateTime Timestamp { get; set; }
        public string Barcode { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string PartType { get; set; } = string.Empty;
        public string Manufacturer { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public string ActionType { get; set; } = string.Empty;
        public int QuantityChange { get; set; }
        public string MachineName { get; set; } = string.Empty;
    }

    public class ReportOrderRowDto
    {
        public int Id { get; set; }
        public List<int> OrderIds { get; set; } = new();
        public int ItemId { get; set; }
        public string Barcode { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string PartType { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public string Manufacturer { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public string CountryOfOrigin { get; set; } = string.Empty;
        public string Rack { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? OrderedAt { get; set; }
    }
}
