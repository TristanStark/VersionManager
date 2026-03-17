using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using VersionManager.Models;
using VersionManager.Services.Interfaces;
using VersionManager.ViewModels;

namespace VersionManager.Services;

public sealed class ZipService : IZipService
{
    public List<ZipEntryInfo> ReadEntries(string zipPath)
    {
        var result = new List<ZipEntryInfo>();

        using var archive = ZipFile.OpenRead(zipPath);

        foreach (var entry in archive.Entries)
        {
            bool isDirectory = string.IsNullOrEmpty(entry.Name) && entry.FullName.EndsWith("/");

            result.Add(new ZipEntryInfo
            {
                FullName = entry.FullName,
                Name = isDirectory
                    ? entry.FullName.TrimEnd('/').Split('/').LastOrDefault() ?? entry.FullName
                    : entry.Name,
                SizeBytes = entry.Length,
                LastWriteTime = entry.LastWriteTime.DateTime,
                IsDirectory = isDirectory
            });
        }

        return result;
    }

    public List<ZipTreeNodeViewModel> BuildTree(IEnumerable<ZipEntryInfo> entries)
    {
        var rootMap = new Dictionary<string, ZipTreeNodeViewModel>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in entries.OrderBy(e => e.FullName, StringComparer.OrdinalIgnoreCase))
        {
            string normalized = entry.FullName.Replace('\\', '/').Trim('/');
            if (string.IsNullOrWhiteSpace(normalized))
                continue;

            var parts = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries);
            Dictionary<string, ZipTreeNodeViewModel> currentLevel = rootMap;
            ZipTreeNodeViewModel? parent = null;

            for (int i = 0; i < parts.Length; i++)
            {
                string part = parts[i];
                bool isLast = i == parts.Length - 1;

                string fullPath = string.Join('/', parts.Take(i + 1));
                bool nodeIsDirectory = !isLast || entry.IsDirectory;

                if (!currentLevel.TryGetValue(fullPath, out var node))
                {
                    node = new ZipTreeNodeViewModel
                    {
                        Name = part,
                        FullPath = fullPath,
                        IsDirectory = nodeIsDirectory,
                        SizeBytes = nodeIsDirectory ? 0 : entry.SizeBytes,
                        LastModified = nodeIsDirectory ? null : entry.LastWriteTime
                    };

                    currentLevel[fullPath] = node;

                    if (parent != null)
                        parent.Children.Add(node);
                }

                if (!nodeIsDirectory && isLast)
                {
                    node.SizeBytes = entry.SizeBytes;
                    node.LastModified = entry.LastWriteTime;
                }

                parent = node;
                currentLevel = node.ChildMap;
            }
        }

        var roots = rootMap.Values
            .Where(n => !n.FullPath.Contains('/'))
            .OrderByDescending(n => n.IsDirectory)
            .ThenBy(n => n.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        SortRecursively(roots);
        return roots;
    }

    private static void SortRecursively(IEnumerable<ZipTreeNodeViewModel> nodes)
    {
        foreach (var node in nodes)
        {
            var ordered = node.Children
                .OrderByDescending(c => c.IsDirectory)
                .ThenBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            node.ReplaceChildren(ordered);
            SortRecursively(node.Children);
        }
    }

    public FileDetailInfo BuildFileDetails(string zipPath, string entryFullName)
    {
        using var archive = ZipFile.OpenRead(zipPath);
        var entry = archive.GetEntry(entryFullName.Replace('\\', '/'));

        if (entry == null)
        {
            return new FileDetailInfo
            {
                Name = "",
                FullPath = entryFullName,
                FileType = "Introuvable"
            };
        }

        if (string.IsNullOrEmpty(entry.Name))
        {
            return new FileDetailInfo
            {
                Name = entry.FullName.TrimEnd('/').Split('/').LastOrDefault() ?? entry.FullName,
                FullPath = entry.FullName,
                SizeBytes = 0,
                LastModified = entry.LastWriteTime.DateTime,
                FileType = "Dossier",
                Sha256 = "-"
            };
        }

        using var stream = entry.Open();
        using var sha = SHA256.Create();
        string hash = Convert.ToHexString(sha.ComputeHash(stream));

        return new FileDetailInfo
        {
            Name = entry.Name,
            FullPath = entry.FullName,
            SizeBytes = entry.Length,
            LastModified = entry.LastWriteTime.DateTime,
            Sha256 = hash,
            FileType = Path.GetExtension(entry.Name)
        };
    }

    public Task ExtractAsync(string zipPath, string destinationFolder)
    {
        if (Directory.Exists(destinationFolder))
            Directory.Delete(destinationFolder, true);

        Directory.CreateDirectory(destinationFolder);
        System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, destinationFolder);
        return Task.CompletedTask;
    }

    public Task CreateZipAsync(string sourceFolder, string outputZipPath)
    {
        if (File.Exists(outputZipPath))
            File.Delete(outputZipPath);

        System.IO.Compression.ZipFile.CreateFromDirectory(sourceFolder, outputZipPath);
        return Task.CompletedTask;
    }

}