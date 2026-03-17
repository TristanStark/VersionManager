namespace VersionManager.Models;

public sealed class ZipEntryInfo
{
    public string FullName { get; set; } = "";
    public string Name { get; set; } = "";
    public long SizeBytes { get; set; }
    public DateTime? LastWriteTime { get; set; }
    public bool IsDirectory { get; set; }
}