using System;
using System.IO;

namespace inventory_management.Services
{
    public static class AssetPathService
    {
        public static string BasePath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "InventoryManagement",
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
            
            // Migrate old assets if they exist in the AppContext.BaseDirectory
            var oldPath = Path.Combine(AppContext.BaseDirectory, "assets");
            if (Directory.Exists(oldPath) && oldPath != BasePath)
            {
                try
                {
                    CopyDirectory(oldPath, BasePath);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to migrate assets: {ex.Message}");
                }
            }

            // Migrate assets from old LocalApplicationData path
            var oldLocalPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "InventoryManagement",
                "assets");
                
            if (Directory.Exists(oldLocalPath) && oldLocalPath != BasePath)
            {
                try
                {
                    CopyDirectory(oldLocalPath, BasePath);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to migrate assets from LocalApplicationData: {ex.Message}");
                }
            }

            _initialized = true;
        }

        private static void CopyDirectory(string sourceDir, string destinationDir)
        {
            var dir = new DirectoryInfo(sourceDir);
            if (!dir.Exists) return;

            Directory.CreateDirectory(destinationDir);

            foreach (FileInfo file in dir.GetFiles())
            {
                string targetFilePath = Path.Combine(destinationDir, file.Name);
                if (!File.Exists(targetFilePath))
                {
                    file.CopyTo(targetFilePath);
                }
            }

            foreach (DirectoryInfo subDir in dir.GetDirectories())
            {
                string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
                CopyDirectory(subDir.FullName, newDestinationDir);
            }
        }
    }
}
