using System.Configuration;
using System.Data;
using System.Windows;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using inventory_management.Data;
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
                    // DATABASE CONNECTION
                    // TODO: UPDATE THIS CONNECTION STRING to match your External SSD PostgreSQL setup
                    var connectionString = "Host=localhost;Database=inventory_ac_db;Username=postgres;Password=password";

                    services.AddDbContext<InventoryDbContext>(options =>
                        options.UseNpgsql(connectionString), ServiceLifetime.Transient);

                    // SERVICES
                    services.AddSingleton<Services.IBarcodeService, Services.BarcodeService>();
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
                var integrityCheck = scope.ServiceProvider.GetRequiredService<IIntegrityCheckService>();
                var integrityResult = await integrityCheck.RunAsync();
                if (!integrityResult.IsHealthy)
                {
                    MessageBox.Show(string.Join(Environment.NewLine, integrityResult.Issues), "Integrity Check Warning");
                }

                var authService = scope.ServiceProvider.GetRequiredService<IAuthenticationService>();
                var adminResult = await authService.EnsureDefaultAdminAsync();
                if (adminResult.Created)
                {
                    MessageBox.Show($"Default admin created. Username: {adminResult.Username}. Temporary password: {adminResult.TemporaryPassword}", "Admin Account Created");
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
    }
}
