using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace inventory_management.Services
{
    public class BackupHostedService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public BackupHostedService(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await RunInitialFullBackupAsync(stoppingToken);

            using var timer = new PeriodicTimer(TimeSpan.FromMinutes(5));
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await RunIncrementalBackupAsync(stoppingToken);
                await RunScheduledFullBackupAsync(stoppingToken);
            }
        }

        private async Task RunInitialFullBackupAsync(CancellationToken stoppingToken)
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var backupService = scope.ServiceProvider.GetRequiredService<IBackupService>();
            await backupService.TryRunScheduledFullBackupAsync();
            stoppingToken.ThrowIfCancellationRequested();
        }

        private async Task RunIncrementalBackupAsync(CancellationToken stoppingToken)
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var backupService = scope.ServiceProvider.GetRequiredService<IBackupService>();
            await backupService.RunIncrementalBackupAsync();
            stoppingToken.ThrowIfCancellationRequested();
        }

        private async Task RunScheduledFullBackupAsync(CancellationToken stoppingToken)
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var backupService = scope.ServiceProvider.GetRequiredService<IBackupService>();
            await backupService.TryRunScheduledFullBackupAsync();
            stoppingToken.ThrowIfCancellationRequested();
        }
    }
}
