using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VersionManager.Models;
using VersionManager.Services.Interfaces;

namespace VersionManager.Services
{
    public class VersionBuildService: IVersionBuildService
    {
        private readonly ITempDirectoryService _tempDirectoryService;
        private readonly IZipService _zipService;
        private readonly IPatchPreparationService _patchPreparationService;
        private readonly IArtifactoryService _artifactoryService;

        public VersionBuildService()
        {
            _tempDirectoryService = new TempDirectoryService();
            _zipService = new ZipService();
            _patchPreparationService = new PatchPreparationService();
            _artifactoryService = new ArtifactoryService();
        }


        public async Task<BuildVersionResult> BuildAndUploadAsync(
    string patchZipPath,
    string newVersionNumber,
    IProgress<string>? logProgress = null,
    IProgress<double>? valueProgress = null,
    CancellationToken cancellationToken = default)
        {
            string rootTemp = _tempDirectoryService.CreateTempDirectory("version_build");
            string rawPatchFolder = Path.Combine(rootTemp, "patch_raw");
            string preparedPatchFolder = Path.Combine(rootTemp, "patch_prepared");
            string latestZipFolder = Path.Combine(rootTemp, "latest_zip");
            string latestExtractedFolder = Path.Combine(rootTemp, "latest_extracted");
            string outputFolder = Path.Combine(rootTemp, "output");
            string downloadedLatestZipPath = "";
            string finalZipPath = Path.Combine(outputFolder, $"package_{newVersionNumber}.zip");

            try
            {
                Directory.CreateDirectory(rawPatchFolder);
                Directory.CreateDirectory(preparedPatchFolder);
                Directory.CreateDirectory(latestZipFolder);
                Directory.CreateDirectory(latestExtractedFolder);
                Directory.CreateDirectory(outputFolder);

                logProgress?.Report("Extraction du patch brut...");
                valueProgress?.Report(10);
                await _zipService.ExtractAsync(patchZipPath, rawPatchFolder);

                cancellationToken.ThrowIfCancellationRequested();

                logProgress?.Report("Préparation du patch...");
                valueProgress?.Report(25);
                await _patchPreparationService.PreparePatchAsync(rawPatchFolder, preparedPatchFolder);

                cancellationToken.ThrowIfCancellationRequested();

                logProgress?.Report("Téléchargement de la dernière version...");
                valueProgress?.Report(40);
                downloadedLatestZipPath = await _artifactoryService.DownloadLatestVersionZipAsync(latestZipFolder);

                cancellationToken.ThrowIfCancellationRequested();

                logProgress?.Report("Extraction de la dernière version...");
                valueProgress?.Report(55);
                await _zipService.ExtractAsync(downloadedLatestZipPath, latestExtractedFolder);

                cancellationToken.ThrowIfCancellationRequested();

                logProgress?.Report("Fusion des fichiers...");
                valueProgress?.Report(70);
                MergeFolders(preparedPatchFolder, latestExtractedFolder);

                cancellationToken.ThrowIfCancellationRequested();

                logProgress?.Report("Création de l'archive finale...");
                valueProgress?.Report(85);
                await _zipService.CreateZipAsync(latestExtractedFolder, finalZipPath);

                cancellationToken.ThrowIfCancellationRequested();

                logProgress?.Report("Upload vers Artifactory...");
                valueProgress?.Report(95);
                await _artifactoryService.UploadVersionZipAsync(finalZipPath, newVersionNumber);

                valueProgress?.Report(100);
                logProgress?.Report("Publication terminée.");

                return new BuildVersionResult
                {
                    Success = true,
                    FinalZipPath = finalZipPath,
                    VersionNumber = newVersionNumber,
                    Message = "Version publiée avec succès."
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
}
