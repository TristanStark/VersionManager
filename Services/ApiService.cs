using VersionManager.Models;
using VersionManager.Services.Interfaces;

namespace VersionManager.Services;

public sealed class ApiService : IApiService
{
    public Task<ApiStatusInfo> GetApiStatusAsync()
    {
        return Task.FromResult(new ApiStatusInfo
        {
            IsConnected = true,
            ApiUrl = "https://api.exemple.com",
            LastPingTime = DateTime.Now,
            LatestVersion = "2.4.1"
        });
    }

    public Task<List<VersionInfo>> GetVersionHistoryAsync()
    {
        var data = new List<VersionInfo>
        {
            new()
            {
                VersionNumber = "2.4.1",
                CreatedAt = DateTime.Now.AddDays(-1),
                Author = "Alice",
                Description = "Correctifs mineurs",
                Status = "En ligne",
                ZipSizeBytes = 5_200_000
            },
            new()
            {
                VersionNumber = "2.4.0",
                CreatedAt = DateTime.Now.AddDays(-7),
                Author = "Bob",
                Description = "Ajout nouvelles fonctionnalités",
                Status = "En ligne",
                ZipSizeBytes = 7_800_000
            },
            new()
            {
                VersionNumber = "2.3.8",
                CreatedAt = DateTime.Now.AddDays(-18),
                Author = "Charlie",
                Description = "Bug fixes",
                Status = "Archivée",
                ZipSizeBytes = 4_500_000
            }
        };

        return Task.FromResult(data);
    }
}