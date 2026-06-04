using System.Configuration;
using System.Data;
using System.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using inventory_management.Data;
using inventory_management.Data.Entities;
using inventory_management.Services;

namespace inventory_management
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static IHost? AppHost { get; private set; }

        public App()
        {
            AppHost = Host.CreateDefaultBuilder()
                .ConfigureServices((hostContext, services) =>
                {
                    var connectionString = hostContext.Configuration.GetConnectionString("InventoryDb")
                        ?? "Host=localhost;Database=inventory_ac_db;Username=postgres;Password=pasan";

                    services.AddDbContext<InventoryDbContext>(options =>
                        options.UseNpgsql(connectionString), ServiceLifetime.Transient);

                    // SERVICES
                    services.AddSingleton<Services.IBarcodeService, Services.BarcodeService>();
                    services.AddSingleton<Services.IPrintService, Services.PrintService>();
                    services.AddTransient<Services.IStockService, Services.StockService>();
                    services.AddTransient<IDatabaseAvailabilityService, DatabaseAvailabilityService>();
                    services.AddTransient<IBackupService, BackupService>();
                    services.AddTransient<IIntegrityCheckService, IntegrityCheckService>();
                    services.AddTransient<IAuthenticationService, AuthenticationService>();
                    services.AddHostedService<BackupHostedService>();

                    // VIEW MODELS
                    services.AddTransient<ViewModels.MainViewModel>();
                    services.AddTransient<ViewModels.HomeViewModel>();
                    services.AddTransient<ViewModels.LoginViewModel>();
                    services.AddTransient<ViewModels.ItemCreationViewModel>();
                    services.AddTransient<ViewModels.AddStockViewModel>();
                    services.AddTransient<ViewModels.RemoveStockViewModel>();
                    services.AddTransient<ViewModels.SearchItemsViewModel>();
                    services.AddTransient<ViewModels.ReportsViewModel>();

                    // MAIN WINDOW
                    services.AddSingleton<MainWindow>();
                })
                .Build();
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            await AppHost!.StartAsync();

            using (var scope = AppHost.Services.CreateScope())
            {
                try
                {
                    AssetPathService.EnsureInitialized();

                    var dbContext = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
                    
                    // Check database connection
                    var canConnect = await dbContext.Database.CanConnectAsync();
                    if (!canConnect)
                    {
                        MessageBox.Show(
                            "Cannot connect to the database. Please ensure PostgreSQL is running and the connection settings in appsettings.json are correct.\\n\\n" +
                            "Expected connection: Host=localhost;Database=inventory_ac_db;Username=postgres;Password=2003",
                            "Database Connection Error",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                        Current.Shutdown();
                        return;
                    }
                    
                    await dbContext.Database.MigrateAsync();
                    await EnsurePlaceholderDataAsync(dbContext);
                    await EnsureIdentitySequencesAsync(dbContext);

                    var integrityCheck = scope.ServiceProvider.GetRequiredService<IIntegrityCheckService>();
                    await integrityCheck.RunAsync();

                    var authService = scope.ServiceProvider.GetRequiredService<IAuthenticationService>();
                    
                    // Force reset admin password to ensure access
                    await authService.ForceSetPasswordAsync("admin", "admin123");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Failed to initialize application:\\n\\n{ex.Message}\\n\\n{ex.InnerException?.Message}\\n\\nPlease check your database connection and try again.",
                        "Application Startup Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    Current.Shutdown();
                    return;
                }
            }

            var startupForm = AppHost.Services.GetRequiredService<MainWindow>();
            startupForm.Show();

            base.OnStartup(e);
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            await AppHost!.StopAsync();
            base.OnExit(e);
        }

        private static async Task EnsurePlaceholderDataAsync(InventoryDbContext context)
        {
            if (!await context.PartTypes.AnyAsync())
            {
                context.PartTypes.AddRange(
                    new PartType { Name = "Compressor" },
                    new PartType { Name = "Condenser" });
            }

            if (!await context.PartBrands.AnyAsync())
            {
                context.PartBrands.AddRange(
                    new PartBrand { Name = "Denso" },
                    new PartBrand { Name = "Bosch" });
            }

            if (!await context.Manufacturers.AnyAsync())
            {
                context.Manufacturers.AddRange(
                    new VehicleManufacturer { Name = "Toyota" },
                    new VehicleManufacturer { Name = "Ford" });
            }

            if (!await context.Racks.AnyAsync())
            {
                context.Racks.AddRange(
                    new Rack { LocationCode = "A-01" },
                    new Rack { LocationCode = "B-05" });
            }

            await context.SaveChangesAsync();

            if (!await context.Models.AnyAsync())
            {
                var toyota = await context.Manufacturers.FirstAsync(m => m.Name == "Toyota");
                var ford = await context.Manufacturers.FirstAsync(m => m.Name == "Ford");

                context.Models.AddRange(
                    new VehicleModel { VehicleManufacturerId = toyota.Id, Name = "Corolla", YearRange = "2010-2015" },
                    new VehicleModel { VehicleManufacturerId = ford.Id, Name = "Focus", YearRange = "2012-2018" });

                await context.SaveChangesAsync();
            }

            if (!await context.Items.AnyAsync())
            {
                var partType = await context.PartTypes.FirstAsync();
                var brand = await context.PartBrands.FirstAsync();
                var model = await context.Models.FirstAsync();
                var rack = await context.Racks.FirstAsync();

                var item = new Item
                {
                    Barcode = "ITEM-0001",
                    PartTypeId = partType.Id,
                    VehicleModelId = model.Id,
                    PartBrandId = brand.Id,
                    CountryOfOrigin = "Japan",
                    Description = "Placeholder compressor",
                    LowStockThreshold = 5,
                    RackId = rack.Id
                };

                context.Items.Add(item);
                await context.SaveChangesAsync();

                var timestamp = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);

                context.Stocks.Add(new Stock
                {
                    ItemId = item.Id,
                    Quantity = 10,
                    LastUpdated = timestamp
                });

                var transaction = new StockTransaction
                {
                    ItemId = item.Id,
                    ActionType = "IN",
                    QuantityChange = 10,
                    Timestamp = timestamp,
                    MachineName = Environment.MachineName
                };

                transaction.ChecksumHash = StockTransactionHasher.ComputeChecksum(transaction);
                context.Transactions.Add(transaction);

                await context.SaveChangesAsync();
            }
        }

        private static async Task EnsureIdentitySequencesAsync(InventoryDbContext context)
        {
            var statements = new[]
            {
                "SELECT setval(pg_get_serial_sequence('part_types','id'), COALESCE((SELECT MAX(id) FROM part_types), 1), true);",
                "SELECT setval(pg_get_serial_sequence('part_brands','id'), COALESCE((SELECT MAX(id) FROM part_brands), 1), true);",
                "SELECT setval(pg_get_serial_sequence('vehicle_manufacturers','id'), COALESCE((SELECT MAX(id) FROM vehicle_manufacturers), 1), true);",
                "SELECT setval(pg_get_serial_sequence('vehicle_models','id'), COALESCE((SELECT MAX(id) FROM vehicle_models), 1), true);",
                "SELECT setval(pg_get_serial_sequence('racks','id'), COALESCE((SELECT MAX(id) FROM racks), 1), true);",
                "SELECT setval(pg_get_serial_sequence('items','id'), COALESCE((SELECT MAX(id) FROM items), 1), true);",
                "SELECT setval(pg_get_serial_sequence('stock','id'), COALESCE((SELECT MAX(id) FROM stock), 1), true);",
                "SELECT setval(pg_get_serial_sequence('stock_transactions','id'), COALESCE((SELECT MAX(id) FROM stock_transactions), 1), true);",
                "SELECT setval(pg_get_serial_sequence('user_accounts','id'), COALESCE((SELECT MAX(id) FROM user_accounts), 1), true);",
                "SELECT setval(pg_get_serial_sequence('user_login_audits','id'), COALESCE((SELECT MAX(id) FROM user_login_audits), 1), true);"
            };

            foreach (var sql in statements)
            {
                await context.Database.ExecuteSqlRawAsync(sql);
            }
        }
    }
}
