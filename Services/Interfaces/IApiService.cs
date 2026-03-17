using VersionManager.Models;

namespace VersionManager.Services.Interfaces;

public interface IApiService
{
    Task<ApiStatusInfo> GetApiStatusAsync();
    Task<List<VersionInfo>> GetVersionHistoryAsync();
}