using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using VersionManager.Models;
using VersionManager.Services.Interfaces;

namespace VersionManager.Services;

/// <summary>
/// HTTP implementation of the Artifactory API operations required by the version manager.
/// </summary>
public sealed class ArtifactoryService : IArtifactoryService, IDisposable
{
    private const string ArtifactoryUrlEnvironmentVariable = "VERSIONMANAGER_ARTIFACTORY_URL";
    private const string RepositoryEnvironmentVariable = "VERSIONMANAGER_ARTIFACTORY_REPOSITORY";
    private const string PathPrefixEnvironmentVariable = "VERSIONMANAGER_ARTIFACTORY_PATH_PREFIX";
    private const string PackageFileNameEnvironmentVariable = "VERSIONMANAGER_ARTIFACTORY_PACKAGE_FILE_NAME";
    private const string UsernameEnvironmentVariable = "VERSIONMANAGER_ARTIFACTORY_USERNAME";
    private const string TokenEnvironmentVariable = "VERSIONMANAGER_ARTIFACTORY_TOKEN";
    private const string AuthModeEnvironmentVariable = "VERSIONMANAGER_ARTIFACTORY_AUTH_MODE";

    private static readonly Regex SemVerRegex = new(
        "^v?(?<major>0|[1-9]\\d*)\\.(?<minor>0|[1-9]\\d*)\\.(?<patch>0|[1-9]\\d*)(?:-(?<pre>[0-9A-Za-z.-]+))?(?:\\+[0-9A-Za-z.-]+)?$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly AppSettings _settings;
    private readonly HttpClient _httpClient;
    private readonly bool _ownsHttpClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="ArtifactoryService" /> class using settings.json.
    /// </summary>
    public ArtifactoryService()
        : this(new SettingsService().Load())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ArtifactoryService" /> class.
    /// </summary>
    public ArtifactoryService(AppSettings settings, HttpClient? httpClient = null)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _httpClient = httpClient ?? new HttpClient();
        _ownsHttpClient = httpClient == null;
        _httpClient.Timeout = TimeSpan.FromSeconds(Math.Max(1, _settings.TimeoutSeconds));
    }

    /// <inheritdoc />
    public async Task<string> DownloadLatestVersionZipAsync(string destinationFolder, CancellationToken cancellationToken = default)
    {
        string latestVersion = await GetLatestVersionAsync(cancellationToken);
        return await DownloadVersionZipAsync(latestVersion, destinationFolder, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<string> DownloadVersionZipAsync(string versionNumber, string destinationFolder, CancellationToken cancellationToken = default)
    {
        ValidateConfiguration(requireAuthentication: true);
        ValidateSemVer(versionNumber);

        Directory.CreateDirectory(destinationFolder);

        string packageFileName = ResolvePackageFileName(versionNumber);
        string destinationPath = Path.Combine(destinationFolder, packageFileName);
        Uri artifactUri = BuildArtifactUri(versionNumber);

        using var request = CreateRequest(HttpMethod.Get, artifactUri);
        using HttpResponseMessage response = await _httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        await EnsureSuccessAsync(response, $"download {versionNumber}", cancellationToken);

        await using Stream artifactStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using FileStream fileStream = File.Create(destinationPath);
        await artifactStream.CopyToAsync(fileStream, cancellationToken);

        return destinationPath;
    }

    /// <inheritdoc />
    public async Task UploadVersionZipAsync(string zipPath, string versionNumber, CancellationToken cancellationToken = default)
    {
        ValidateConfiguration(requireAuthentication: true);
        ValidateSemVer(versionNumber);

        if (!File.Exists(zipPath))
            throw new FileNotFoundException("Le ZIP à publier est introuvable.", zipPath);

        Uri artifactUri = BuildArtifactUri(versionNumber);

        await using FileStream fileStream = File.OpenRead(zipPath);
        using var request = CreateRequest(HttpMethod.Put, artifactUri);
        request.Content = new StreamContent(fileStream);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/zip");

        using HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, $"upload {versionNumber}", cancellationToken);
    }

    /// <inheritdoc />
    public async Task<string> GetLatestVersionAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<string> versions = await GetAvailableVersionsAsync(cancellationToken);

        string? latest = versions
            .OrderBy(version => version, SemanticVersionStringComparer.Instance)
            .LastOrDefault();

        if (string.IsNullOrWhiteSpace(latest))
            throw new InvalidOperationException("Aucune version SemVer n'a été trouvée dans le dépôt Artifactory configuré.");

        return latest;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> GetAvailableVersionsAsync(CancellationToken cancellationToken = default)
    {
        ValidateConfiguration(requireAuthentication: true);

        HashSet<string> versions = new(StringComparer.OrdinalIgnoreCase);

        Uri listUri = BuildStorageListUri(useFileListApi: true);
        using (var request = CreateRequest(HttpMethod.Get, listUri))
        using (HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken))
        {
            if (response.IsSuccessStatusCode)
            {
                await using Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                var list = await JsonSerializer.DeserializeAsync<ArtifactoryFileListResponse>(stream, JsonOptions, cancellationToken);
                foreach (string version in ExtractVersionsFromFileList(list))
                    versions.Add(version);
            }
        }

        if (versions.Count == 0)
        {
            Uri childrenUri = BuildStorageListUri(useFileListApi: false);
            using var request = CreateRequest(HttpMethod.Get, childrenUri);
            using HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);
            await EnsureSuccessAsync(response, "list versions", cancellationToken);

            await using Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var storage = await JsonSerializer.DeserializeAsync<ArtifactoryStorageResponse>(stream, JsonOptions, cancellationToken);
            foreach (string version in ExtractVersionsFromChildren(storage))
                versions.Add(version);
        }

        return versions
            .OrderBy(version => version, SemanticVersionStringComparer.Instance)
            .ToList();
    }

    /// <inheritdoc />
    public async Task<List<VersionInfo>> GetVersionHistoryAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<string> versions = await GetAvailableVersionsAsync(cancellationToken);
        var history = new List<VersionInfo>();

        foreach (string version in versions.OrderByDescending(version => version, SemanticVersionStringComparer.Instance))
        {
            var item = new VersionInfo
            {
                VersionNumber = version,
                CreatedAt = DateTime.Now,
                Author = "Artifactory",
                Description = ResolvePackageFileName(version),
                Status = "En ligne",
                ZipSizeBytes = 0
            };

            ArtifactoryStorageItemResponse? metadata = await TryGetArtifactMetadataAsync(version, cancellationToken);
            if (metadata != null)
            {
                item.CreatedAt = ParseArtifactoryDate(metadata.Created)
                    ?? ParseArtifactoryDate(metadata.LastModified)
                    ?? DateTime.Now;

                if (long.TryParse(metadata.Size, NumberStyles.Integer, CultureInfo.InvariantCulture, out long size))
                    item.ZipSizeBytes = size;
            }

            history.Add(item);
        }

        return history;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_ownsHttpClient)
            _httpClient.Dispose();
    }

    private async Task<ArtifactoryStorageItemResponse?> TryGetArtifactMetadataAsync(string versionNumber, CancellationToken cancellationToken)
    {
        Uri metadataUri = BuildStorageArtifactMetadataUri(versionNumber);
        using var request = CreateRequest(HttpMethod.Get, metadataUri);
        using HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
            return null;

        await using Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await JsonSerializer.DeserializeAsync<ArtifactoryStorageItemResponse>(stream, JsonOptions, cancellationToken);
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, Uri uri)
    {
        var request = new HttpRequestMessage(method, uri);
        string authMode = GetSetting(AuthModeEnvironmentVariable, _settings.AuthMode).Trim();
        string token = GetSetting(TokenEnvironmentVariable, _settings.AccessToken).Trim();
        string username = GetSetting(UsernameEnvironmentVariable, _settings.Username).Trim();

        if (authMode.Equals("None", StringComparison.OrdinalIgnoreCase))
            return request;

        if (authMode.Equals("Bearer", StringComparison.OrdinalIgnoreCase))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return request;
        }

        if (authMode.Equals("ApiKey", StringComparison.OrdinalIgnoreCase))
        {
            request.Headers.TryAddWithoutValidation("X-JFrog-Art-Api", token);
            return request;
        }

        string rawCredentials = $"{username}:{token}";
        string encodedCredentials = Convert.ToBase64String(Encoding.ASCII.GetBytes(rawCredentials));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", encodedCredentials);
        return request;
    }

    private Uri BuildArtifactUri(string versionNumber)
    {
        string path = JoinUriSegments(
            GetSetting(RepositoryEnvironmentVariable, _settings.RepositoryKey),
            GetSetting(PathPrefixEnvironmentVariable, _settings.PathPrefix),
            versionNumber,
            ResolvePackageFileName(versionNumber));

        return new Uri(EnsureTrailingSlash(GetArtifactoryRootUrl()) + path);
    }

    private Uri BuildStorageArtifactMetadataUri(string versionNumber)
    {
        string path = JoinUriSegments(
            GetSetting(RepositoryEnvironmentVariable, _settings.RepositoryKey),
            GetSetting(PathPrefixEnvironmentVariable, _settings.PathPrefix),
            versionNumber,
            ResolvePackageFileName(versionNumber));

        return new Uri($"{EnsureTrailingSlash(GetArtifactoryRootUrl())}api/storage/{path}");
    }

    private Uri BuildStorageListUri(bool useFileListApi)
    {
        string path = JoinUriSegments(
            GetSetting(RepositoryEnvironmentVariable, _settings.RepositoryKey),
            GetSetting(PathPrefixEnvironmentVariable, _settings.PathPrefix));

        string suffix = useFileListApi
            ? $"api/storage/{path}?list&deep=0&listFolders=1"
            : $"api/storage/{path}";

        return new Uri(EnsureTrailingSlash(GetArtifactoryRootUrl()) + suffix);
    }

    private string ResolvePackageFileName(string versionNumber)
    {
        string fileName = GetSetting(PackageFileNameEnvironmentVariable, _settings.PackageFileName).Trim();
        if (string.IsNullOrWhiteSpace(fileName))
            throw new InvalidOperationException("Le nom du fichier ZIP Artifactory n'est pas configuré.");

        return fileName.Replace("{version}", versionNumber, StringComparison.OrdinalIgnoreCase);
    }

    private string GetArtifactoryRootUrl()
    {
        string url = GetSetting(ArtifactoryUrlEnvironmentVariable, _settings.ApiUrl).Trim();
        if (string.IsNullOrWhiteSpace(url))
            throw new InvalidOperationException("L'URL Artifactory n'est pas configurée.");

        return url;
    }

    private void ValidateConfiguration(bool requireAuthentication)
    {
        if (!Uri.TryCreate(GetArtifactoryRootUrl(), UriKind.Absolute, out _))
            throw new InvalidOperationException("L'URL Artifactory configurée n'est pas valide.");

        if (string.IsNullOrWhiteSpace(GetSetting(RepositoryEnvironmentVariable, _settings.RepositoryKey)))
            throw new InvalidOperationException("La clé du dépôt Artifactory n'est pas configurée.");

        ResolvePackageFileName("0.0.0");

        if (!requireAuthentication)
            return;

        string authMode = GetSetting(AuthModeEnvironmentVariable, _settings.AuthMode).Trim();
        if (authMode.Equals("None", StringComparison.OrdinalIgnoreCase))
            return;

        string token = GetSetting(TokenEnvironmentVariable, _settings.AccessToken).Trim();
        if (string.IsNullOrWhiteSpace(token))
            throw new InvalidOperationException("Le token Artifactory n'est pas configuré.");

        if (authMode.Equals("Basic", StringComparison.OrdinalIgnoreCase)
            && string.IsNullOrWhiteSpace(GetSetting(UsernameEnvironmentVariable, _settings.Username)))
        {
            throw new InvalidOperationException("Le nom d'utilisateur Artifactory est obligatoire en Basic Auth.");
        }
    }

    private static void ValidateSemVer(string versionNumber)
    {
        if (!SemVerRegex.IsMatch(versionNumber ?? string.Empty))
            throw new FormatException($"La version '{versionNumber}' n'est pas une version SemVer valide. Exemple attendu : 1.2.3.");
    }

    private static IEnumerable<string> ExtractVersionsFromFileList(ArtifactoryFileListResponse? response)
    {
        if (response?.Files == null)
            yield break;

        foreach (ArtifactoryFileListItem item in response.Files)
        {
            string? version = ExtractFirstPathSegment(item.Uri);
            if (!string.IsNullOrWhiteSpace(version) && SemVerRegex.IsMatch(version))
                yield return version;
        }
    }

    private static IEnumerable<string> ExtractVersionsFromChildren(ArtifactoryStorageResponse? response)
    {
        if (response?.Children == null)
            yield break;

        foreach (ArtifactoryChild item in response.Children.Where(child => child.Folder))
        {
            string? version = ExtractFirstPathSegment(item.Uri);
            if (!string.IsNullOrWhiteSpace(version) && SemVerRegex.IsMatch(version))
                yield return version;
        }
    }

    private static string? ExtractFirstPathSegment(string? path)
    {
        return path?
            .Replace('\\', '/')
            .Trim('/')
            .Split('/', StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault();
    }

    private static string JoinUriSegments(params string?[] segments)
    {
        return string.Join(
            '/',
            segments
                .Where(segment => !string.IsNullOrWhiteSpace(segment))
                .SelectMany(segment => segment!.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries))
                .Select(Uri.EscapeDataString));
    }

    private static string EnsureTrailingSlash(string value)
    {
        return value.EndsWith('/', StringComparison.Ordinal) ? value : value + "/";
    }

    private static string GetSetting(string environmentVariable, string configuredValue)
    {
        string? environmentValue = Environment.GetEnvironmentVariable(environmentVariable);
        return string.IsNullOrWhiteSpace(environmentValue) ? configuredValue : environmentValue;
    }

    private static DateTime? ParseArtifactoryDate(string? value)
    {
        if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out DateTime result))
            return result;

        return null;
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, string operation, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
            return;

        string body = await response.Content.ReadAsStringAsync(cancellationToken);
        string message = response.StatusCode switch
        {
            HttpStatusCode.Unauthorized => "authentification refusée",
            HttpStatusCode.Forbidden => "authentification acceptée mais permissions insuffisantes",
            HttpStatusCode.NotFound => "artefact ou chemin introuvable",
            _ => response.ReasonPhrase ?? "erreur HTTP"
        };

        throw new HttpRequestException(
            $"Artifactory {operation} a échoué : {(int)response.StatusCode} {message}. {body}".Trim());
    }

    private sealed class SemanticVersionStringComparer : IComparer<string>
    {
        public static readonly SemanticVersionStringComparer Instance = new();

        public int Compare(string? x, string? y)
        {
            if (ReferenceEquals(x, y))
                return 0;

            if (x == null)
                return -1;

            if (y == null)
                return 1;

            return SemanticVersion.Parse(x).CompareTo(SemanticVersion.Parse(y));
        }
    }

    private sealed record SemanticVersion(int Major, int Minor, int Patch, string PreRelease) : IComparable<SemanticVersion>
    {
        public static SemanticVersion Parse(string version)
        {
            Match match = SemVerRegex.Match(version);
            if (!match.Success)
                throw new FormatException($"Version SemVer invalide : {version}");

            return new SemanticVersion(
                int.Parse(match.Groups["major"].Value, CultureInfo.InvariantCulture),
                int.Parse(match.Groups["minor"].Value, CultureInfo.InvariantCulture),
                int.Parse(match.Groups["patch"].Value, CultureInfo.InvariantCulture),
                match.Groups["pre"].Value);
        }

        public int CompareTo(SemanticVersion? other)
        {
            if (other == null)
                return 1;

            int major = Major.CompareTo(other.Major);
            if (major != 0)
                return major;

            int minor = Minor.CompareTo(other.Minor);
            if (minor != 0)
                return minor;

            int patch = Patch.CompareTo(other.Patch);
            if (patch != 0)
                return patch;

            return ComparePreRelease(PreRelease, other.PreRelease);
        }

        private static int ComparePreRelease(string left, string right)
        {
            bool leftEmpty = string.IsNullOrWhiteSpace(left);
            bool rightEmpty = string.IsNullOrWhiteSpace(right);

            if (leftEmpty && rightEmpty)
                return 0;

            if (leftEmpty)
                return 1;

            if (rightEmpty)
                return -1;

            string[] leftParts = left.Split('.');
            string[] rightParts = right.Split('.');
            int max = Math.Max(leftParts.Length, rightParts.Length);

            for (int i = 0; i < max; i++)
            {
                if (i >= leftParts.Length)
                    return -1;

                if (i >= rightParts.Length)
                    return 1;

                bool leftNumeric = int.TryParse(leftParts[i], NumberStyles.None, CultureInfo.InvariantCulture, out int leftNumber);
                bool rightNumeric = int.TryParse(rightParts[i], NumberStyles.None, CultureInfo.InvariantCulture, out int rightNumber);

                if (leftNumeric && rightNumeric)
                {
                    int numericCompare = leftNumber.CompareTo(rightNumber);
                    if (numericCompare != 0)
                        return numericCompare;
                }
                else if (leftNumeric)
                {
                    return -1;
                }
                else if (rightNumeric)
                {
                    return 1;
                }
                else
                {
                    int textCompare = string.CompareOrdinal(leftParts[i], rightParts[i]);
                    if (textCompare != 0)
                        return textCompare;
                }
            }

            return 0;
        }
    }

    private sealed class ArtifactoryFileListResponse
    {
        [JsonPropertyName("files")]
        public List<ArtifactoryFileListItem>? Files { get; set; }
    }

    private sealed class ArtifactoryFileListItem
    {
        [JsonPropertyName("uri")]
        public string Uri { get; set; } = "";
    }

    private sealed class ArtifactoryStorageResponse
    {
        [JsonPropertyName("children")]
        public List<ArtifactoryChild>? Children { get; set; }
    }

    private sealed class ArtifactoryChild
    {
        [JsonPropertyName("uri")]
        public string Uri { get; set; } = "";

        [JsonPropertyName("folder")]
        public bool Folder { get; set; }
    }

    private sealed class ArtifactoryStorageItemResponse
    {
        [JsonPropertyName("created")]
        public string? Created { get; set; }

        [JsonPropertyName("lastModified")]
        public string? LastModified { get; set; }

        [JsonPropertyName("size")]
        public string? Size { get; set; }
    }
}
