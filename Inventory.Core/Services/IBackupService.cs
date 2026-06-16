using System.Threading.Tasks;

namespace inventory_management.Services
{
    public interface IBackupService
    {
        Task RunIncrementalBackupAsync();
        Task RunFullBackupAsync();
        Task<bool> TryRunScheduledFullBackupAsync();
    }
}
