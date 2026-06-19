using VersionManager.Models;

namespace VersionManager.Services.Interfaces;

/// <summary>
/// Provides Artifactory operations used by the version publication workflow.
/// </summary>
public interface IArtifactoryService
{
    /// <summary>
    /// Downloads the ZIP artifact associated with the highest SemVer folder found in the configured repository.
    /// </summary>
    Task<string> DownloadLatestVersionZipAsync(string destinationFolder, CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads the ZIP artifact associated with the supplied SemVer folder.
    /// </summary>
    Task<string> DownloadVersionZipAsync(string versionNumber, string destinationFolder, CancellationToken cancellationToken = default);

    /// <summary>
    /// Uploads a ZIP artifact into the configured repository under the supplied SemVer folder.
    /// </summary>
    Task UploadVersionZipAsync(string zipPath, string versionNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the highest SemVer folder currently available in the configured repository path.
    /// </summary>
    Task<string> GetLatestVersionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the SemVer folders available in the configured repository path.
    /// </summary>
    Task<IReadOnlyList<string>> GetAvailableVersionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets version history information from Artifactory metadata.
    /// </summary>
    Task<List<VersionInfo>> GetVersionHistoryAsync(CancellationToken cancellationToken = default);
}
