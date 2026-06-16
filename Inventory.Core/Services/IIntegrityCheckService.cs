using System.Collections.Generic;
using System.Threading.Tasks;

namespace inventory_management.Services
{
    public interface IIntegrityCheckService
    {
        Task<IntegrityCheckResult> RunAsync();
    }

    public class IntegrityCheckResult
    {
        public bool IsHealthy { get; set; }
        public List<string> Issues { get; set; } = new();
    }
}
