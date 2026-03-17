using VersionManager.Models;
using VersionManager.ViewModels;

namespace VersionManager.Services.Interfaces;

public interface IZipService
{
    List<ZipEntryInfo> ReadEntries(string zipPath);
    List<ZipTreeNodeViewModel> BuildTree(IEnumerable<ZipEntryInfo> entries);
    FileDetailInfo BuildFileDetails(string zipPath, string entryFullName);
}