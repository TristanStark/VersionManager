using System.Collections.ObjectModel;
using VersionManager.Models;

namespace VersionManager.ViewModels;

/// <summary>
/// UI node representing one major Artifactory folder (for example v20).
/// The Artifactory connector can populate this collection directly later.
/// </summary>
public sealed class MajorVersionNodeViewModel
{
    public string Name { get; init; } = "";
    public ObservableCollection<PatchVersionNodeViewModel> Patches { get; } = new();

    public int PatchCount => Patches.Count;
    public int BuildCount => Patches.Sum(patch => patch.Builds.Count);

    public DateTime? LatestBuildDate => Patches
        .SelectMany(patch => patch.Builds)
        .OrderByDescending(build => build.CreatedAt)
        .FirstOrDefault()
        ?.CreatedAt;
}

/// <summary>
/// UI node representing one patch folder (for example p37).
/// Builds reuse the existing VersionInfo rows and DataGrid columns.
/// </summary>
public sealed class PatchVersionNodeViewModel
{
    public string Name { get; init; } = "";
    public ObservableCollection<VersionInfo> Builds { get; } = new();

    public int BuildCount => Builds.Count;

    public DateTime? LatestBuildDate => Builds
        .OrderByDescending(build => build.CreatedAt)
        .FirstOrDefault()
        ?.CreatedAt;
}
