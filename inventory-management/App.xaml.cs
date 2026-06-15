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
                        ?? "Host=localhost;Database=inventory_ac_db;Username=postgres;Password=root";

                    services.AddDbContext<InventoryDbContext>(options =>
                        options.UseNpgsql(connectionString), ServiceLifetime.Transient);

                    // SERVICES
                    services.AddSingleton<Services.IBarcodeService, Services.BarcodeService>();
                    services.AddSingleton<Services.IPrintService, Services.PrintService>();
                    services.AddSingleton<Services.IMobileCameraService, Services.MobileCameraService>();
                    services.AddTransient<Services.IStockService, Services.StockService>();
                    services.AddTransient<IDatabaseAvailabilityService, DatabaseAvailabilityService>();
                    services.AddTransient<IBackupService, BackupService>();
                    services.AddTransient<IIntegrityCheckService, IntegrityCheckService>();
                    services.AddTransient<Services.IAuthenticationService, AuthenticationService>();
                    services.AddTransient<Services.IPdfService, Services.PdfService>();
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
                    
                    await dbContext.Database.MigrateAsync();
                    await EnsureIdentitySequencesAsync(dbContext);
                    await FixLegacyRegisteredDatesAsync(dbContext);

                    var integrityCheck = scope.ServiceProvider.GetRequiredService<IIntegrityCheckService>();
                    await integrityCheck.RunAsync();

                    var stockService = scope.ServiceProvider.GetRequiredService<IStockService>();
                    await stockService.CleanupOldZeroStockItemsAsync();

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

        private static async Task FixLegacyRegisteredDatesAsync(InventoryDbContext context)
        {
            // Set legacy dates to the stock's last updated time, or current time if there is no stock
            var sql = @"
                UPDATE items 
                SET registered_date = COALESCE((SELECT last_updated FROM stock WHERE stock.item_id = items.id LIMIT 1), CURRENT_TIMESTAMP)
                WHERE registered_date <= '0001-01-01 23:59:59';
            ";
            await context.Database.ExecuteSqlRawAsync(sql);
        }
    }
}
