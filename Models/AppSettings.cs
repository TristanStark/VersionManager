namespace VersionManager.Models;

/// <summary>
/// Stores the local configuration used to access Artifactory.
/// </summary>
public sealed class AppSettings
{
    /// <summary>
    /// Gets or sets the Artifactory root URL, for example https://server/artifactory.
    /// </summary>
    public string ApiUrl { get; set; } = "https://artifactory.exemple.com/artifactory";

    /// <summary>
    /// Gets or sets the target Artifactory repository key.
    /// </summary>
    public string RepositoryKey { get; set; } = "repo-local";

    /// <summary>
    /// Gets or sets an optional folder prefix inside the repository before the SemVer folder.
    /// </summary>
    public string PathPrefix { get; set; } = "";

    /// <summary>
    /// Gets or sets the ZIP file name stored in each SemVer folder. Use {version} as a placeholder when needed.
    /// </summary>
    public string PackageFileName { get; set; } = "package_{version}.zip";

    /// <summary>
    /// Gets or sets the Artifactory user name used by Basic authentication.
    /// </summary>
    public string Username { get; set; } = "";

    /// <summary>
    /// Gets or sets the Artifactory token, API key, identity token, or password depending on <see cref="AuthMode" />.
    /// </summary>
    public string AccessToken { get; set; } = "";

    /// <summary>
    /// Gets or sets the authentication mode. Supported values are Basic, Bearer, ApiKey, and None.
    /// </summary>
    public string AuthMode { get; set; } = "Basic";

    /// <summary>
    /// Gets or sets the HTTP timeout in seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 300;
}
