using VersionManager.Models;

namespace VersionManager.Services.Interfaces;

/// <summary>
/// Builds and publishes a new version ZIP.
/// </summary>
public interface IVersionBuildService
{
    /// <summary>
    /// Downloads the current version, builds the updated ZIP from the selected patch ZIP, and uploads the result.
    /// </summary>
    Task<BuildVersionResult> BuildAndUploadAsync(
        string patchZipPath,
        string newVersionNumber,
        IProgress<string>? logProgress = null,
        IProgress<double>? valueProgress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Builds a new ZIP from the current version ZIP and a patch ZIP.
    /// Replace this method body with the project-specific merge logic when needed.
    /// </summary>
    Task<string> BuildUpdatedZipAsync(
        string currentVersionZipPath,
        string patchZipPath,
        string outputZipPath,
        IProgress<string>? logProgress = null,
        IProgress<double>? valueProgress = null,
        CancellationToken cancellationToken = default);
}
