using inventory_management.Data;
using inventory_management.Data.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Diagnostics;
using Npgsql;

namespace inventory_management.Services
{
    public class BackupService : IBackupService
    {
        private readonly InventoryDbContext _context;
        private readonly IDatabaseAvailabilityService _availabilityService;
        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions { WriteIndented = true };
        private readonly string _backupFolder;
        private readonly string _statePath;

        public BackupService(InventoryDbContext context, IDatabaseAvailabilityService availabilityService)
        {
            _context = context;
            _availabilityService = availabilityService;
            var basePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            _backupFolder = Path.Combine(basePath, "InventoryManagement", "backups");
            _statePath = Path.Combine(_backupFolder, "backup-state.json");
            Directory.CreateDirectory(_backupFolder);
        }

        public async Task RunIncrementalBackupAsync()
        {
            var availability = await _availabilityService.GetStatusAsync();
            if (!availability.IsAvailable)
            {
                return;
            }

            var state = await LoadStateAsync();
            var since = state.LastIncrementalUtc ?? DateTime.UtcNow.AddMinutes(-10);

            var transactions = await _context.Transactions
                .AsNoTracking()
                .Where(t => t.Timestamp > since)
                .OrderBy(t => t.Timestamp)
                .ToListAsync();

            if (transactions.Count == 0)
            {
                state.LastIncrementalUtc = DateTime.UtcNow;
                await SaveStateAsync(state);
                return;
            }

            var payload = new IncrementalBackup
            {
                GeneratedUtc = DateTime.UtcNow,
                Transactions = transactions
            };

            var fileName = $"incremental-{DateTime.UtcNow:yyyyMMdd-HHmmss}.json";
            var path = Path.Combine(_backupFolder, fileName);
            await File.WriteAllTextAsync(path, JsonSerializer.Serialize(payload, _jsonOptions));

            state.LastIncrementalUtc = payload.GeneratedUtc;
            await SaveStateAsync(state);
        }

        public async Task RunFullBackupAsync()
        {
            var availability = await _availabilityService.GetStatusAsync();
            if (!availability.IsAvailable)
            {
                return;
            }

            var fileName = $"pg_backup-{DateTime.Now:yyyyMMdd-HHmmss}.backup";
            var path = Path.Combine(_backupFolder, fileName);
            
            try 
            {
                var connStr = _context.Database.GetDbConnection().ConnectionString;
                var builder = new NpgsqlConnectionStringBuilder(connStr);
                
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = "pg_dump",
                    Arguments = $"-h {builder.Host} -U {builder.Username} -d {builder.Database} -F c -f \"{path}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                
                processStartInfo.EnvironmentVariables["PGPASSWORD"] = builder.Password;

                using var process = Process.Start(processStartInfo);
                if (process != null)
                {
                    await process.WaitForExitAsync();
                    if (process.ExitCode != 0)
                    {
                        var error = await process.StandardError.ReadToEndAsync();
                        System.Diagnostics.Debug.WriteLine($"pg_dump failed: {error}");
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error running pg_dump: {ex.Message}");
                return;
            }

            var state = await LoadStateAsync();
            state.LastFullBackupLocalDate = DateTime.Now.Date;
            await SaveStateAsync(state);
        }

        public async Task<bool> TryRunScheduledFullBackupAsync()
        {
            var state = await LoadStateAsync();
            var now = DateTime.Now;
            
            if (state.LastFullBackupLocalDate.HasValue)
            {
                var daysSinceLastBackup = (now.Date - state.LastFullBackupLocalDate.Value.Date).TotalDays;
                if (daysSinceLastBackup < 2)
                {
                    return false;
                }
            }

            if (now.Hour < 12)
            {
                return false;
            }

            await RunFullBackupAsync();
            return true;
        }

        private async Task<BackupState> LoadStateAsync()
        {
            if (!File.Exists(_statePath))
            {
                return new BackupState();
            }

            var json = await File.ReadAllTextAsync(_statePath);
            return JsonSerializer.Deserialize<BackupState>(json) ?? new BackupState();
        }

        private Task SaveStateAsync(BackupState state)
        {
            var json = JsonSerializer.Serialize(state, _jsonOptions);
            return File.WriteAllTextAsync(_statePath, json);
        }

        private class BackupState
        {
            public DateTime? LastIncrementalUtc { get; set; }
            public DateTime? LastFullBackupLocalDate { get; set; }
        }

        private class IncrementalBackup
        {
            public DateTime GeneratedUtc { get; set; }
            public List<StockTransaction> Transactions { get; set; } = new();
        }

        private class FullBackup
        {
            public DateTime GeneratedUtc { get; set; }
            public List<PartType> PartTypes { get; set; } = new();
            public List<PartBrand> PartBrands { get; set; } = new();
            public List<VehicleManufacturer> Manufacturers { get; set; } = new();
            public List<VehicleModel> Models { get; set; } = new();
            public List<Rack> Racks { get; set; } = new();
            public List<Item> Items { get; set; } = new();
            public List<Stock> Stocks { get; set; } = new();
            public List<StockTransaction> Transactions { get; set; } = new();
            public List<UserAccount> UserAccounts { get; set; } = new();
        }
    }
}
