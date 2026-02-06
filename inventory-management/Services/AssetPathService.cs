using System;
using System.IO;

namespace inventory_management.Services
{
    public static class AssetPathService
    {
        public static string BasePath => Path.Combine(
            AppContext.BaseDirectory,
            "assets");

        private static bool _initialized;

        public static string GetAbsolutePath(string? relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                return string.Empty;
            }

            EnsureInitialized();

            var trimmed = relativePath.Trim();
            trimmed = trimmed.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return Path.Combine(BasePath, trimmed);
        }

        public static void EnsureInitialized()
        {
            if (_initialized)
            {
                return;
            }

            Directory.CreateDirectory(BasePath);
            _initialized = true;
        }
    }
}
