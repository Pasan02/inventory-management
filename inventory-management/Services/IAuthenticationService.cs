using System.Threading.Tasks;
using inventory_management.Data.Entities;

namespace inventory_management.Services
{
    public interface IAuthenticationService
    {
        Task<LoginResult> LoginAsync(string username, string password);
        Task<DefaultAdminResult> EnsureDefaultAdminAsync();
        Task<AdminResetResult> TryResetAdminAsync();
        Task ForceSetPasswordAsync(string username, string password);
    }

    public class LoginResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public UserAccount? User { get; set; }
    }

    public class DefaultAdminResult
    {
        public bool Created { get; set; }
        public string Username { get; set; } = string.Empty;
        public string TemporaryPassword { get; set; } = string.Empty;
    }

    public class AdminResetResult
    {
        public bool Changed { get; set; }
        public bool Created { get; set; }
        public string Username { get; set; } = string.Empty;
        public string TemporaryPassword { get; set; } = string.Empty;
    }
}
