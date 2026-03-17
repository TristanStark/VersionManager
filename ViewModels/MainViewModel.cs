using System.Collections.ObjectModel;
using System.IO;
using Microsoft.Win32;
using VersionManager.Helpers;
using VersionManager.Models;
using VersionManager.Services.Interfaces;
using VersionManager.ViewModels.Base;
using System.Windows;
using VersionManager.Models;
using VersionManager.Services.Interfaces;
using System.Runtime.Serialization;

namespace VersionManager.ViewModels;

public sealed class MainViewModel : ViewModelBase
{
    private readonly IApiService _apiService;
    private readonly IZipService _zipService;

    private string _latestVersion = "-";
    private string _apiUrl = "";
    private bool _isApiConnected;
    private DateTime? _lastPingTime;

    private string _selectedZipPath = "";
    private long _selectedZipSizeBytes;
    private int _selectedZipFileCount;
    private double _progressValue;

    private string _selectedFileName = "-";
    private string _selectedFilePath = "-";
    private string _selectedFileType = "-";
    private string _selectedFileSize = "-";
    private string _selectedFileModified = "-";
    private string _selectedFileSha256 = "-";

    private readonly ISettingsService _settingsService;
    private AppSettings _appSettings;

    private VersionInfo? _selectedVersion;
    private ZipTreeNodeViewModel? _selectedZipNode;
    private bool _isBusy;
    private readonly IVersionBuildService _versionBuildService;

    public string LatestVersion
    {
        get => _latestVersion;
        set => SetProperty(ref _latestVersion, value);
    }

    public string ApiUrl
    {
        get => _apiUrl;
        set => SetProperty(ref _apiUrl, value);
    }

    public bool IsApiConnected
    {
        get => _isApiConnected;
        set => SetProperty(ref _isApiConnected, value);
    }

    public DateTime? LastPingTime
    {
        get => _lastPingTime;
        set => SetProperty(ref _lastPingTime, value);
    }

    public string SelectedZipPath
    {
        get => _selectedZipPath;
        set => SetProperty(ref _selectedZipPath, value);
    }

    public long SelectedZipSizeBytes
    {
        get => _selectedZipSizeBytes;
        set
        {
            if (SetProperty(ref _selectedZipSizeBytes, value))
                OnPropertyChanged(nameof(SelectedZipReadableSize));
        }
    }

    public string SelectedZipReadableSize => FileSizeHelper.ToReadableSize(SelectedZipSizeBytes);

    public int SelectedZipFileCount
    {
        get => _selectedZipFileCount;
        set => SetProperty(ref _selectedZipFileCount, value);
    }

    public double ProgressValue
    {
        get => _progressValue;
        set => SetProperty(ref _progressValue, value);
    }

    public string SelectedFileName
    {
        get => _selectedFileName;
        set => SetProperty(ref _selectedFileName, value);
    }

    public string SelectedFilePath
    {
        get => _selectedFilePath;
        set => SetProperty(ref _selectedFilePath, value);
    }

    public string SelectedFileType
    {
        get => _selectedFileType;
        set => SetProperty(ref _selectedFileType, value);
    }

    public string SelectedFileSize
    {
        get => _selectedFileSize;
        set => SetProperty(ref _selectedFileSize, value);
    }

    public string SelectedFileModified
    {
        get => _selectedFileModified;
        set => SetProperty(ref _selectedFileModified, value);
    }

    public string SelectedFileSha256
    {
        get => _selectedFileSha256;
        set => SetProperty(ref _selectedFileSha256, value);
    }

    public VersionInfo? SelectedVersion
    {
        get => _selectedVersion;
        set => SetProperty(ref _selectedVersion, value);
    }

    public ZipTreeNodeViewModel? SelectedZipNode
    {
        get => _selectedZipNode;
        set
        {
            if (SetProperty(ref _selectedZipNode, value))
                UpdateSelectedNodeDetails();
        }
    }

    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }

    public ObservableCollection<VersionInfo> VersionHistory { get; } = new();
    public ObservableCollection<WorkflowStep> WorkflowSteps { get; } = new();
    public ObservableCollection<string> Logs { get; } = new();
    public ObservableCollection<ZipTreeNodeViewModel> ZipTreeRoots { get; } = new();

    public AsyncRelayCommand RefreshCommand { get; }
    public RelayCommand BrowseZipCommand { get; }
    public AsyncRelayCommand CreateNewVersionCommand { get; }
    public RelayCommand OpenApiSettingsCommand { get; }


    public MainViewModel()
        : this(new Services.ApiService(), new Services.ZipService(), new Services.SettingsService())
    {
    }

    public MainViewModel(
        IApiService apiService,
        IZipService zipService,
        ISettingsService settingsService)
    {
        _apiService = apiService;
        _zipService = zipService;
        _settingsService = settingsService;
        _versionBuildService = new Services.VersionBuildService();

        _appSettings = _settingsService.Load();
        _apiUrl = _appSettings.ApiUrl;

        RefreshCommand = new AsyncRelayCommand(RefreshDataAsync);
        BrowseZipCommand = new RelayCommand(BrowseZip);
        CreateNewVersionCommand = new AsyncRelayCommand(CreateNewVersionAsync);
        OpenApiSettingsCommand = new RelayCommand(OpenApiSettings);

        InitializeWorkflow();
        _ = RefreshDataAsync();
    }

    private void InitializeWorkflow()
    {
        WorkflowSteps.Clear();
        WorkflowSteps.Add(new WorkflowStep { Label = "Sélectionner le ZIP", State = "Pending" });
        WorkflowSteps.Add(new WorkflowStep { Label = "Télécharger la version actuelle", State = "Pending" });
        WorkflowSteps.Add(new WorkflowStep { Label = "Appliquer les modifications", State = "Pending" });
        WorkflowSteps.Add(new WorkflowStep { Label = "Saisir le nouveau numéro", State = "Pending" });
        WorkflowSteps.Add(new WorkflowStep { Label = "Recréer l’archive", State = "Pending" });
        WorkflowSteps.Add(new WorkflowStep { Label = "Envoyer à l’API", State = "Pending" });
    }

    private async Task RefreshDataAsync()
    {
        try
        {
            IsBusy = true;
            ProgressValue = 10;
            AddLog("Chargement des informations API...");

            var status = await _apiService.GetApiStatusAsync();
            LatestVersion = status.LatestVersion;
            ApiUrl = _appSettings.ApiUrl;
            IsApiConnected = status.IsConnected;
            LastPingTime = status.LastPingTime;

            ProgressValue = 50;
            AddLog("Chargement de l'historique des versions...");

            var history = await _apiService.GetVersionHistoryAsync();
            VersionHistory.Clear();
            foreach (var item in history)
                VersionHistory.Add(item);

            ProgressValue = 100;
            AddLog("Rafraîchissement terminé.");
        }
        catch (Exception ex)
        {
            AddLog($"Erreur lors du rafraîchissement : {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void BrowseZip()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Fichiers ZIP (*.zip)|*.zip",
            Title = "Sélectionner un ZIP d'update"
        };

        if (dialog.ShowDialog() != true)
            return;

        try
        {
            IsBusy = true;
            ProgressValue = 5;

            string zipPath = dialog.FileName;
            SelectedZipPath = zipPath;

            var fileInfo = new FileInfo(zipPath);
            SelectedZipSizeBytes = fileInfo.Length;

            AddLog($"ZIP sélectionné : {zipPath}");
            ProgressValue = 20;

            var entries = _zipService.ReadEntries(zipPath);
            SelectedZipFileCount = entries.Count(e => !e.IsDirectory);

            ProgressValue = 50;

            var tree = _zipService.BuildTree(entries);
            ZipTreeRoots.Clear();
            foreach (var root in tree)
                ZipTreeRoots.Add(root);

            ProgressValue = 100;
            SetWorkflowState(0, "Done");
            AddLog($"Analyse du ZIP terminée. {SelectedZipFileCount} fichiers détectés.");

            ClearSelectedFileDetails();
        }
        catch (Exception ex)
        {
            AddLog($"Erreur lecture ZIP : {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void CreateNewVersion()
    {
        AddLog("Workflow 'Créer nouvelle version' à brancher : download version courante, merge, saisie version, rezip, upload.");
        SetWorkflowState(1, "Pending");
        SetWorkflowState(2, "Pending");
        SetWorkflowState(3, "Pending");
        SetWorkflowState(4, "Pending");
        SetWorkflowState(5, "Pending");
    }

    private void UpdateSelectedNodeDetails()
    {
        if (SelectedZipNode == null)
        {
            ClearSelectedFileDetails();
            return;
        }

        if (SelectedZipNode.IsDirectory)
        {
            SelectedFileName = SelectedZipNode.Name;
            SelectedFilePath = SelectedZipNode.FullPath;
            SelectedFileType = "Dossier";
            SelectedFileSize = "-";
            SelectedFileModified = SelectedZipNode.LastModified?.ToString("dd/MM/yyyy HH:mm:ss") ?? "-";
            SelectedFileSha256 = "-";
            return;
        }

        if (string.IsNullOrWhiteSpace(SelectedZipPath) || !File.Exists(SelectedZipPath))
        {
            SelectedFileName = SelectedZipNode.Name;
            SelectedFilePath = SelectedZipNode.FullPath;
            SelectedFileType = "Fichier";
            SelectedFileSize = SelectedZipNode.DisplaySize;
            SelectedFileModified = SelectedZipNode.LastModified?.ToString("dd/MM/yyyy HH:mm:ss") ?? "-";
            SelectedFileSha256 = "-";
            return;
        }

        try
        {
            var details = _zipService.BuildFileDetails(SelectedZipPath, SelectedZipNode.FullPath);
            SelectedFileName = details.Name;
            SelectedFilePath = details.FullPath;
            SelectedFileType = details.FileType;
            SelectedFileSize = FileSizeHelper.ToReadableSize(details.SizeBytes);
            SelectedFileModified = details.LastModified?.ToString("dd/MM/yyyy HH:mm:ss") ?? "-";
            SelectedFileSha256 = details.Sha256;
        }
        catch (Exception ex)
        {
            AddLog($"Erreur détail fichier : {ex.Message}");
        }
    }

    private void OpenApiSettings()
    {
        var window = new ApiSettingsWindow(ApiUrl)
        {
            Owner = Application.Current.MainWindow
        };

        bool? result = window.ShowDialog();
        if (result != true)
            return;

        ApiUrl = window.ApiUrl.Trim();

        if (!Uri.TryCreate(window.ApiUrl.Trim(), UriKind.Absolute, out _))
        {
            MessageBox.Show("L'URL saisie n'est pas valide.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        _appSettings.ApiUrl = ApiUrl;
        _settingsService.Save(_appSettings);

        AddLog($"URL API mise à jour : {ApiUrl}");
    }

    private void ClearSelectedFileDetails()
    {
        SelectedFileName = "-";
        SelectedFilePath = "-";
        SelectedFileType = "-";
        SelectedFileSize = "-";
        SelectedFileModified = "-";
        SelectedFileSha256 = "-";
    }

    private void SetWorkflowState(int index, string state)
    {
        if (index < 0 || index >= WorkflowSteps.Count)
            return;

        WorkflowSteps[index].State = state;
    }

    private void AddLog(string message)
    {
        Logs.Add($"[{DateTime.Now:HH:mm:ss}] {message}");
    }

    public static string IncrementVersion(string version, int maxPerSegment = 9)
    {
        if (string.IsNullOrWhiteSpace(version))
            throw new ArgumentException("La version ne peut pas être vide.", nameof(version));

        string[] parts = version.Split('.');
        int[] numbers = new int[parts.Length];

        for (int i = 0; i < parts.Length; i++)
        {
            if (!int.TryParse(parts[i], out numbers[i]))
                throw new FormatException($"Segment invalide : '{parts[i]}'");

            if (numbers[i] < 0 || numbers[i] > maxPerSegment)
                throw new FormatException(
                    $"Le segment '{numbers[i]}' dépasse la valeur max autorisée ({maxPerSegment}).");
        }

        int carry = 1;

        for (int i = numbers.Length - 1; i >= 0; i--)
        {
            if (carry == 0)
                break;

            numbers[i] += carry;

            if (numbers[i] > maxPerSegment)
            {
                numbers[i] = 0;
                carry = 1;
            }
            else
            {
                carry = 0;
            }
        }

        if (carry == 1)
        {
            // Cas où tout a débordé : 9.9.9 -> 1.0.0.0
            int[] extended = new int[numbers.Length + 1];
            extended[0] = 1;
            Array.Copy(numbers, 0, extended, 1, numbers.Length);
            numbers = extended;
        }

        return string.Join(".", numbers);
    }
    private string? GetNewVersionNumber()
    {
        string oldVersion = LatestVersion;
        return IncrementVersion(oldVersion);
    }

    private async Task CreateNewVersionAsync()
    {
        if (string.IsNullOrWhiteSpace(SelectedZipPath) || !File.Exists(SelectedZipPath))
        {
            AddLog("Aucun ZIP de patch sélectionné.");
            return;
        }

        string? newVersion = GetNewVersionNumber();
        if (string.IsNullOrWhiteSpace(newVersion))
            return;

        try
        {
            IsBusy = true;
            AddLog($"Démarrage de la création de la version {newVersion}...");

            var logProgress = new Progress<string>(msg => AddLog(msg));
            var valueProgress = new Progress<double>(v => ProgressValue = v);

            var result = await _versionBuildService.BuildAndUploadAsync(
                SelectedZipPath,
                newVersion,
                logProgress,
                valueProgress);

            AddLog(result.Message);

            if (result.Success)
                await RefreshDataAsync();
        }
        catch (Exception ex)
        {
            AddLog($"Erreur : {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }
}