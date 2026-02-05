using System.Threading.Tasks;

namespace inventory_management.Services
{
    public interface IDatabaseAvailabilityService
    {
        Task<DatabaseAvailability> GetStatusAsync();
        Task<bool> IsDatabaseAvailableAsync();
    }

    public record DatabaseAvailability(bool IsAvailable, string Message);
}
