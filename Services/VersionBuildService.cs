using System.IO;
using VersionManager.Models;
using VersionManager.Services.Interfaces;

namespace VersionManager.Services;

/// <summary>
/// Orchestrates the version build workflow: download current ZIP, apply patch ZIP, and upload final ZIP.
/// </summary>
public sealed class VersionBuildService : IVersionBuildService
{
    private readonly ITempDirectoryService _tempDirectoryService;
    private readonly IZipService _zipService;
    private readonly IArtifactoryService _artifactoryService;

    /// <summary>
    /// Initializes a new instance of the <see cref="VersionBuildService" /> class using settings.json.
    /// </summary>
    public VersionBuildService()
        : this(new SettingsService().Load())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="VersionBuildService" /> class.
    /// </summary>
    public VersionBuildService(AppSettings settings)
        : this(new TempDirectoryService(), new ZipService(), new ArtifactoryService(settings))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="VersionBuildService" /> class.
    /// </summary>
    public VersionBuildService(
        ITempDirectoryService tempDirectoryService,
        IZipService zipService,
        IArtifactoryService artifactoryService)
    {
        _tempDirectoryService = tempDirectoryService;
        _zipService = zipService;
        _artifactoryService = artifactoryService;
    }

    /// <inheritdoc />
    public async Task<BuildVersionResult> BuildAndUploadAsync(
        string patchZipPath,
        string newVersionNumber,
        IProgress<string>? logProgress = null,
        IProgress<double>? valueProgress = null,
        CancellationToken cancellationToken = default)
    {
        string rootTemp = _tempDirectoryService.CreateTempDirectory("version_build");
        string latestZipFolder = Path.Combine(rootTemp, "latest_zip");
        string outputFolder = Path.Combine(rootTemp, "output");
        string finalZipPath = Path.Combine(outputFolder, $"package_{newVersionNumber}.zip");

        try
        {
            Directory.CreateDirectory(latestZipFolder);
            Directory.CreateDirectory(outputFolder);

            if (string.IsNullOrWhiteSpace(patchZipPath) || !File.Exists(patchZipPath))
                throw new FileNotFoundException("Le ZIP de patch est introuvable.", patchZipPath);

            logProgress?.Report("Téléchargement de la dernière version Artifactory...");
            valueProgress?.Report(25);
            string currentVersionZipPath = await _artifactoryService.DownloadLatestVersionZipAsync(latestZipFolder, cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            logProgress?.Report("Application du patch sur le ZIP courant...");
            valueProgress?.Report(55);
            finalZipPath = await BuildUpdatedZipAsync(
                currentVersionZipPath,
                patchZipPath,
                finalZipPath,
                logProgress,
                valueProgress,
                cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            logProgress?.Report("Upload vers Artifactory...");
            valueProgress?.Report(95);
            await _artifactoryService.UploadVersionZipAsync(finalZipPath, newVersionNumber, cancellationToken);

            valueProgress?.Report(100);
            logProgress?.Report("Publication terminée.");

            return new BuildVersionResult
            {
                Success = true,
                FinalZipPath = finalZipPath,
                VersionNumber = newVersionNumber,
                Message = $"Version {newVersionNumber} publiée avec succès."
            };
        }
        catch (Exception ex)
        {
            return new BuildVersionResult
            {
                Success = false,
                FinalZipPath = finalZipPath,
                VersionNumber = newVersionNumber,
                Message = ex.Message
            };
        }
        finally
        {
            _tempDirectoryService.DeleteDirectorySafe(rootTemp);
        }
    }

    /// <inheritdoc />
    public async Task<string> BuildUpdatedZipAsync(
        string currentVersionZipPath,
        string patchZipPath,
        string outputZipPath,
        IProgress<string>? logProgress = null,
        IProgress<double>? valueProgress = null,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(currentVersionZipPath))
            throw new FileNotFoundException("Le ZIP courant est introuvable.", currentVersionZipPath);

        if (!File.Exists(patchZipPath))
            throw new FileNotFoundException("Le ZIP de patch est introuvable.", patchZipPath);

        string mergeTemp = _tempDirectoryService.CreateTempDirectory("version_merge");
        string currentExtractedFolder = Path.Combine(mergeTemp, "current");
        string patchExtractedFolder = Path.Combine(mergeTemp, "patch");

        try
        {
            Directory.CreateDirectory(currentExtractedFolder);
            Directory.CreateDirectory(patchExtractedFolder);

            logProgress?.Report("Extraction du ZIP courant...");
            valueProgress?.Report(60);
            await _zipService.ExtractAsync(currentVersionZipPath, currentExtractedFolder);

            cancellationToken.ThrowIfCancellationRequested();

            logProgress?.Report("Extraction du ZIP de patch...");
            valueProgress?.Report(70);
            await _zipService.ExtractAsync(patchZipPath, patchExtractedFolder);

            cancellationToken.ThrowIfCancellationRequested();

            // Hook volontairement simple : remplace ce bloc par ta logique métier de merge.
            // Par défaut, le patch écrase les fichiers existants et ajoute les nouveaux fichiers.
            logProgress?.Report("Fusion par défaut du patch dans le ZIP courant...");
            MergeFolders(patchExtractedFolder, currentExtractedFolder);

            cancellationToken.ThrowIfCancellationRequested();

            logProgress?.Report("Création de l'archive finale...");
            valueProgress?.Report(85);
            string? outputFolder = Path.GetDirectoryName(outputZipPath);
            if (!string.IsNullOrWhiteSpace(outputFolder))
                Directory.CreateDirectory(outputFolder);

            await _zipService.CreateZipAsync(currentExtractedFolder, outputZipPath);
            return outputZipPath;
        }
        finally
        {
            _tempDirectoryService.DeleteDirectorySafe(mergeTemp);
        }
    }

    private static void MergeFolders(string sourceFolder, string targetFolder)
    {
        foreach (string directory in Directory.GetDirectories(sourceFolder, "*", SearchOption.AllDirectories))
        {
            string relative = Path.GetRelativePath(sourceFolder, directory);
            string targetDir = Path.Combine(targetFolder, relative);
            Directory.CreateDirectory(targetDir);
        }

        foreach (string file in Directory.GetFiles(sourceFolder, "*", SearchOption.AllDirectories))
        {
            string relative = Path.GetRelativePath(sourceFolder, file);
            string targetFile = Path.Combine(targetFolder, relative);

            string? targetDir = Path.GetDirectoryName(targetFile);
            if (!string.IsNullOrWhiteSpace(targetDir))
                Directory.CreateDirectory(targetDir);

            File.Copy(file, targetFile, overwrite: true);
        }
    }
}
