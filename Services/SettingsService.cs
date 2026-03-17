using System.IO;
using System.Text.Json;
using VersionManager.Models;
using VersionManager.Services.Interfaces;

namespace VersionManager.Services;

public sealed class SettingsService : ISettingsService
{
    private readonly string _filePath;

    public SettingsService()
    {
        _filePath = Path.Combine(AppContext.BaseDirectory, "settings.json");
    }

    public AppSettings Load()
    {
        try
        {
            if (!File.Exists(_filePath))
            {
                var defaultSettings = new AppSettings();
                Save(defaultSettings);
                return defaultSettings;
            }

            string json = File.ReadAllText(_filePath);
            var settings = JsonSerializer.Deserialize<AppSettings>(json);

            return settings ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    public void Save(AppSettings settings)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        string json = JsonSerializer.Serialize(settings, options);
        File.WriteAllText(_filePath, json);
    }
}