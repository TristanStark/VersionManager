using System.Windows;
using VersionManager.Models;

namespace VersionManager;

public partial class ApiSettingsWindow : Window
{
    public string ApiUrl { get; set; } = "";
    public string RepositoryKey { get; set; } = "";
    public string PathPrefix { get; set; } = "";
    public string PackageFileName { get; set; } = "";
    public string AuthMode { get; set; } = "Basic";
    public string Username { get; set; } = "";
    public string AccessToken { get; set; } = "";
    public int TimeoutSeconds { get; set; } = 300;

    public ApiSettingsWindow(AppSettings currentSettings)
    {
        InitializeComponent();
        ApiUrl = currentSettings.ApiUrl;
        RepositoryKey = currentSettings.RepositoryKey;
        PathPrefix = currentSettings.PathPrefix;
        PackageFileName = currentSettings.PackageFileName;
        AuthMode = currentSettings.AuthMode;
        Username = currentSettings.Username;
        AccessToken = currentSettings.AccessToken;
        TimeoutSeconds = currentSettings.TimeoutSeconds;
        DataContext = this;
    }

    public AppSettings ToAppSettings()
    {
        return new AppSettings
        {
            ApiUrl = ApiUrl.Trim(),
            RepositoryKey = RepositoryKey.Trim(),
            PathPrefix = PathPrefix.Trim().Trim('/'),
            PackageFileName = PackageFileName.Trim(),
            AuthMode = string.IsNullOrWhiteSpace(AuthMode) ? "Basic" : AuthMode.Trim(),
            Username = Username.Trim(),
            AccessToken = AccessToken.Trim(),
            TimeoutSeconds = TimeoutSeconds <= 0 ? 300 : TimeoutSeconds
        };
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
