using VersionManager.Models;
using VersionManager.Services.Interfaces;

namespace VersionManager.Services;

/// <summary>
/// Exposes version information to the ViewModel through the Artifactory-backed API abstraction.
/// </summary>
public sealed class ApiService : IApiService
{
    private readonly IArtifactoryService _artifactoryService;
    private readonly AppSettings _settings;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApiService" /> class using settings.json.
    /// </summary>
    public ApiService()
        : this(new SettingsService().Load())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ApiService" /> class.
    /// </summary>
    public ApiService(AppSettings settings)
        : this(settings, new ArtifactoryService(settings))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ApiService" /> class.
    /// </summary>
    public ApiService(AppSettings settings, IArtifactoryService artifactoryService)
    {
        _settings = settings;
        _artifactoryService = artifactoryService;
    }

    /// <inheritdoc />
    public async Task<ApiStatusInfo> GetApiStatusAsync()
    {
        try
        {
            string latestVersion = await _artifactoryService.GetLatestVersionAsync();
            return new ApiStatusInfo
            {
                IsConnected = true,
                ApiUrl = _settings.ApiUrl,
                LastPingTime = DateTime.Now,
                LatestVersion = latestVersion
            };
        }
        catch
        {
            return new ApiStatusInfo
            {
                IsConnected = false,
                ApiUrl = _settings.ApiUrl,
                LastPingTime = DateTime.Now,
                LatestVersion = "-"
            };
        }
    }

    /// <inheritdoc />
    public async Task<List<VersionInfo>> GetVersionHistoryAsync()
    {
        return await _artifactoryService.GetVersionHistoryAsync();
    }
}
