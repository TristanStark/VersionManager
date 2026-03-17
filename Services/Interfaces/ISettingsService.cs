using VersionManager.Models;

namespace VersionManager.Services.Interfaces;

public interface ISettingsService
{
    AppSettings Load();
    void Save(AppSettings settings);
}