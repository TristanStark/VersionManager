using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VersionManager.Services.Interfaces;

namespace VersionManager.Services
{
    internal class TempDirectoryService: ITempDirectoryService
    {
        public string CreateTempDirectory(string prefix)
        {
            string path = Path.Combine(
                Path.GetTempPath(),
                $"{prefix}_{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}");

            Directory.CreateDirectory(path);
            return path;
        }

        public void DeleteDirectorySafe(string path)
        {
            try
            {
                if (Directory.Exists(path))
                    Directory.Delete(path, true);
            }
            catch
            {
            }
        }

    }
}
