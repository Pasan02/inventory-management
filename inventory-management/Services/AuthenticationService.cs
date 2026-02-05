using inventory_management.Data;
using inventory_management.Data.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace inventory_management.Services
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly InventoryDbContext _context;
        private readonly IDatabaseAvailabilityService _availabilityService;

        public AuthenticationService(InventoryDbContext context, IDatabaseAvailabilityService availabilityService)
        {
            _context = context;
            _availabilityService = availabilityService;
        }

        public async Task<LoginResult> LoginAsync(string username, string password)
        {
            var availability = await _availabilityService.GetStatusAsync();
            if (!availability.IsAvailable)
            {
                return new LoginResult { Success = false, Message = availability.Message };
            }

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                return new LoginResult { Success = false, Message = "Username and password required." };
            }

            var normalized = username.Trim();
            var account = await _context.UserAccounts.FirstOrDefaultAsync(u => u.Username == normalized);

            if (account == null || !account.IsActive)
            {
                await WriteAuditAsync(account?.Id, false, "Invalid credentials.");
                return new LoginResult { Success = false, Message = "Invalid credentials." };
            }

            if (!PasswordHasher.VerifyPassword(password, account.PasswordHash, account.PasswordSalt))
            {
                await WriteAuditAsync(account.Id, false, "Invalid credentials.");
                return new LoginResult { Success = false, Message = "Invalid credentials." };
            }

            await WriteAuditAsync(account.Id, true, null);

            return new LoginResult { Success = true, Message = "Login successful.", User = account };
        }

        public async Task<DefaultAdminResult> EnsureDefaultAdminAsync()
        {
            var availability = await _availabilityService.GetStatusAsync();
            if (!availability.IsAvailable)
            {
                return new DefaultAdminResult { Created = false };
            }

            var hasUser = await _context.UserAccounts.AnyAsync();
            if (hasUser)
            {
                return new DefaultAdminResult { Created = false };
            }

            var password = Environment.GetEnvironmentVariable("INVENTORY_ADMIN_PASSWORD");
            if (string.IsNullOrWhiteSpace(password))
            {
                password = "ChangeMe123!";
            }

            var hashResult = PasswordHasher.HashPassword(password);
            var account = new UserAccount
            {
                Username = "admin",
                PasswordHash = hashResult.Hash,
                PasswordSalt = hashResult.Salt,
                IsActive = true,
                CreatedUtc = DateTime.UtcNow
            };

            _context.UserAccounts.Add(account);
            await _context.SaveChangesAsync();

            await WriteAuditAsync(account.Id, true, "Default admin created.");

            return new DefaultAdminResult
            {
                Created = true,
                Username = account.Username,
                TemporaryPassword = password
            };
        }

        public async Task<AdminResetResult> TryResetAdminAsync()
        {
            var availability = await _availabilityService.GetStatusAsync();
            if (!availability.IsAvailable)
            {
                return new AdminResetResult();
            }

            var password = Environment.GetEnvironmentVariable("INVENTORY_ADMIN_PASSWORD");
            if (string.IsNullOrWhiteSpace(password))
            {
                return new AdminResetResult();
            }

            var username = Environment.GetEnvironmentVariable("INVENTORY_ADMIN_USERNAME");
            if (string.IsNullOrWhiteSpace(username))
            {
                username = "admin";
            }

            var normalized = username.Trim();
            var account = await _context.UserAccounts.FirstOrDefaultAsync(u => u.Username == normalized);

            var hashResult = PasswordHasher.HashPassword(password);
            var created = false;

            if (account == null)
            {
                account = new UserAccount
                {
                    Username = normalized,
                    PasswordHash = hashResult.Hash,
                    PasswordSalt = hashResult.Salt,
                    IsActive = true,
                    CreatedUtc = DateTime.UtcNow
                };

                _context.UserAccounts.Add(account);
                created = true;
            }
            else
            {
                account.PasswordHash = hashResult.Hash;
                account.PasswordSalt = hashResult.Salt;
                account.IsActive = true;
                _context.UserAccounts.Update(account);
            }

            await _context.SaveChangesAsync();
            await WriteAuditAsync(account.Id, true, created ? "Admin account created." : "Admin password reset.");

            return new AdminResetResult
            {
                Changed = true,
                Created = created,
                Username = account.Username,
                TemporaryPassword = password
            };
        }

        private async Task WriteAuditAsync(int? userId, bool success, string? reason)
        {
            var audit = new UserLoginAudit
            {
                UserAccountId = userId,
                Success = success,
                FailureReason = reason,
                MachineName = Environment.MachineName,
                Timestamp = DateTime.UtcNow
            };

            _context.UserLoginAudits.Add(audit);
            await _context.SaveChangesAsync();
        }
    }
}
