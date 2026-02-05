using inventory_management.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<InventoryDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("InventoryDatabase")));

var app = builder.Build();

app.MapGet("/health", async (InventoryDbContext db) =>
{
    var canConnect = await db.Database.CanConnectAsync();
    return Results.Ok(new { database = canConnect ? "available" : "unavailable" });
});

app.MapGet("/stock/{barcode}", async (string barcode, InventoryDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(barcode))
    {
        return Results.BadRequest("Barcode required.");
    }

    var normalized = barcode.Trim();
    var item = await db.Items
        .Include(i => i.Stock)
        .Include(i => i.PartType)
        .Include(i => i.PartBrand)
        .Include(i => i.VehicleModel)
        .ThenInclude(m => m.Manufacturer)
        .Include(i => i.Rack)
        .AsNoTracking()
        .FirstOrDefaultAsync(i => i.Barcode == normalized);

    if (item == null)
    {
        return Results.NotFound();
    }

    return Results.Ok(new
    {
        item.Id,
        item.Barcode,
        item.Description,
        PartType = item.PartType.Name,
        Brand = item.PartBrand.Name,
        Manufacturer = item.VehicleModel.Manufacturer.Name,
        Model = item.VehicleModel.Name,
        item.CountryOfOrigin,
        Rack = item.Rack?.LocationCode,
        Quantity = item.Stock?.Quantity ?? 0,
        item.LowStockThreshold
    });
});

app.MapGet("/low-stock", async (InventoryDbContext db) =>
{
    var items = await db.Items
        .Include(i => i.Stock)
        .Include(i => i.PartType)
        .Include(i => i.PartBrand)
        .Include(i => i.VehicleModel)
        .ThenInclude(m => m.Manufacturer)
        .Include(i => i.Rack)
        .AsNoTracking()
        .ToListAsync();

    var lowStock = items
        .Where(i => (i.Stock?.Quantity ?? 0) <= i.LowStockThreshold)
        .Select(i => new
        {
            i.Id,
            i.Barcode,
            i.Description,
            PartType = i.PartType.Name,
            Brand = i.PartBrand.Name,
            Manufacturer = i.VehicleModel.Manufacturer.Name,
            Model = i.VehicleModel.Name,
            i.CountryOfOrigin,
            Rack = i.Rack?.LocationCode,
            Quantity = i.Stock?.Quantity ?? 0,
            i.LowStockThreshold
        });

    return Results.Ok(lowStock);
});

app.MapGet("/items/search", async (string? q, InventoryDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(q))
    {
        return Results.Ok(Array.Empty<object>());
    }

    var query = q.Trim();

    var items = await db.Items
        .Include(i => i.Stock)
        .Include(i => i.PartType)
        .Include(i => i.PartBrand)
        .Include(i => i.VehicleModel)
        .ThenInclude(m => m.Manufacturer)
        .Include(i => i.Rack)
        .AsNoTracking()
        .Where(i => i.Barcode.Contains(query) || i.Description.Contains(query))
        .Take(50)
        .ToListAsync();

    var results = items.Select(i => new
    {
        i.Id,
        i.Barcode,
        i.Description,
        PartType = i.PartType.Name,
        Brand = i.PartBrand.Name,
        Manufacturer = i.VehicleModel.Manufacturer.Name,
        Model = i.VehicleModel.Name,
        i.CountryOfOrigin,
        Rack = i.Rack?.LocationCode,
        Quantity = i.Stock?.Quantity ?? 0,
        i.LowStockThreshold
    });

    return Results.Ok(results);
});

app.Run();
