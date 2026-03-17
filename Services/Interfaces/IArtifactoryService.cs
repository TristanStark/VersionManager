using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VersionManager.Services.Interfaces
{
    public interface IArtifactoryService
    {
        Task<string> DownloadLatestVersionZipAsync(string destinationFolder);
        Task UploadVersionZipAsync(string zipPath, string versionNumber);
        Task<string> GetLatestVersionAsync();
    }
}
