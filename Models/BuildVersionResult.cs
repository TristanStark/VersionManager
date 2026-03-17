namespace VersionManager.Models;

public sealed class BuildVersionResult
{
    public bool Success { get; set; }
    public string FinalZipPath { get; set; } = "";
    public string VersionNumber { get; set; } = "";
    public string Message { get; set; } = "";
}