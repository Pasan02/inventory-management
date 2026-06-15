using System;
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using inventory_management.Data;
using inventory_management.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel for dual HTTP (5100) and HTTPS (5101) local-network access
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(5100);
    options.ListenAnyIP(5101, listenOptions =>
    {
        listenOptions.UseHttps();
    });
});

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("InventoryDb")
    ?? "Host=localhost;Database=inventory_ac_db;Username=postgres;Password=root";

builder.Services.AddDbContext<InventoryDbContext>(options =>
    options.UseNpgsql(connectionString), ServiceLifetime.Scoped);

// Register Business Services
builder.Services.AddSingleton<IBarcodeService, BarcodeService>();
builder.Services.AddSingleton<IPrintService, PrintService>();
builder.Services.AddTransient<IStockService, StockService>();
builder.Services.AddTransient<IDatabaseAvailabilityService, DatabaseAvailabilityService>();
builder.Services.AddTransient<IBackupService, BackupService>();
builder.Services.AddTransient<IIntegrityCheckService, IntegrityCheckService>();
builder.Services.AddTransient<IAuthenticationService, AuthenticationService>();

// Configure JSON serialization to ignore reference loops (cycles)
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.WriteIndented = true;
    });

// Configure CORS to allow local development access if needed
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Ensure database and assets folder are initialized
using (var scope = app.Services.CreateScope())
{
    try
    {
        AssetPathService.EnsureInitialized();
        var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        db.Database.Migrate();
        
        var integrity = scope.ServiceProvider.GetRequiredService<IIntegrityCheckService>();
        await integrity.RunAsync();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Initialization error: {ex.Message}");
    }
}

app.UseCors("AllowAll");
app.UseRouting();

// Serve frontend static files from wwwroot
app.UseStaticFiles();

// Serve uploaded assets (images, logos) from the local Application Data folder
var assetsPath = AssetPathService.BasePath;
Directory.CreateDirectory(assetsPath);

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(assetsPath),
    RequestPath = "/assets"
});

// Map API Controllers
app.MapControllers();

// Fallback to serve the SPA index.html for client-side routing
app.MapFallbackToFile("index.html");

app.Run();
