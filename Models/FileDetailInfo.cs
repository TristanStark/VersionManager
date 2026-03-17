namespace VersionManager.Models;

public sealed class FileDetailInfo
{
    public string Name { get; set; } = "";
    public string FullPath { get; set; } = "";
    public long SizeBytes { get; set; }
    public DateTime? LastModified { get; set; }
    public string Sha256 { get; set; } = "";
    public string FileType { get; set; } = "";
}