using System;
using System.Security.Cryptography;

namespace inventory_management.Services
{
    public static class PasswordHasher
    {
        public static PasswordHashResult HashPassword(string password)
        {
            using var rng = RandomNumberGenerator.Create();
            var salt = new byte[16];
            rng.GetBytes(salt);

            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100_000, HashAlgorithmName.SHA256);
            var hash = pbkdf2.GetBytes(32);

            return new PasswordHashResult(Convert.ToHexString(hash), Convert.ToHexString(salt));
        }

        public static bool VerifyPassword(string password, string storedHash, string storedSalt)
        {
            var salt = Convert.FromHexString(storedSalt);
            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100_000, HashAlgorithmName.SHA256);
            var hash = pbkdf2.GetBytes(32);
            return Convert.ToHexString(hash) == storedHash;
        }
    }

    public record PasswordHashResult(string Hash, string Salt);
}
