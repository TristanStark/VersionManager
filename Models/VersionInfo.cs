using VersionManager.Helpers;

namespace VersionManager.Models;

public sealed class VersionInfo
{
    public string VersionNumber { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public string Author { get; set; } = "";
    public string Description { get; set; } = "";
    public string Status { get; set; } = "";
    public long ZipSizeBytes { get; set; }

    public string ZipSizeReadable => FileSizeHelper.ToReadableSize(ZipSizeBytes);
}