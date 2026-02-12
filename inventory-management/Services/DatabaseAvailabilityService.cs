using inventory_management.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace inventory_management.Services
{
    public class DatabaseAvailabilityService : IDatabaseAvailabilityService
    {
        private readonly InventoryDbContext _context;

        public DatabaseAvailabilityService(InventoryDbContext context)
        {
            _context = context;
        }

        public async Task<DatabaseAvailability> GetStatusAsync()
        {
            try
            {
                var canConnect = await _context.Database.CanConnectAsync();
                if (canConnect)
                {
                    return new DatabaseAvailability(true, "Database available.");
                }

                return new DatabaseAvailability(false, "Database unavailable. Check external SSD connection.");
            }
            catch (Exception ex)
            {
                return new DatabaseAvailability(false, $"Database unavailable: {ex.Message}");
            }
        }

        public async Task<bool> IsDatabaseAvailableAsync()
        {
            var status = await GetStatusAsync();
            return status.IsAvailable;
        }
    }
}
