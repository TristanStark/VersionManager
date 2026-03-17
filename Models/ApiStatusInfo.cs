namespace VersionManager.Models;

public sealed class ApiStatusInfo
{
    public bool IsConnected { get; set; }
    public string ApiUrl { get; set; } = "";
    public DateTime? LastPingTime { get; set; }
    public string LatestVersion { get; set; } = "";
}