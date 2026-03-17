using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VersionManager.Models;

namespace VersionManager.Services.Interfaces
{
    public interface IVersionBuildService
    {
        Task<BuildVersionResult> BuildAndUploadAsync(
            string patchZipPath,
            string newVersionNumber,
            IProgress<string>? logProgress = null,
            IProgress<double>? valueProgress = null,
            CancellationToken cancellationToken = default);
    }
}
