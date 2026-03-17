using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VersionManager.Services.Interfaces;

namespace VersionManager.Services
{
    internal class ArtifactoryService: IArtifactoryService
    {
        public Task<string> DownloadLatestVersionZipAsync(string destinationFolder)
        {
            return null;
        }

        public Task UploadVersionZipAsync(string zipPath, string versionNumber)
        {
            return Task.CompletedTask;
        }

        public Task<string> GetLatestVersionAsync()
        {
            return null;
        }

    }
}
