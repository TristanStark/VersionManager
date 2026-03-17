using System.Collections.ObjectModel;
using VersionManager.Helpers;
using VersionManager.ViewModels.Base;

namespace VersionManager.ViewModels;

public sealed class ZipTreeNodeViewModel : ViewModelBase
{
    private string _name = "";
    private string _fullPath = "";
    private bool _isDirectory;
    private long _sizeBytes;
    private DateTime? _lastModified;
    private bool _isExpanded;
    private bool _isSelected;

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public string FullPath
    {
        get => _fullPath;
        set => SetProperty(ref _fullPath, value);
    }

    public bool IsDirectory
    {
        get => _isDirectory;
        set
        {
            if (SetProperty(ref _isDirectory, value))
            {
                OnPropertyChanged(nameof(Icon));
                OnPropertyChanged(nameof(DisplaySize));
            }
        }
    }

    public long SizeBytes
    {
        get => _sizeBytes;
        set
        {
            if (SetProperty(ref _sizeBytes, value))
            {
                OnPropertyChanged(nameof(DisplaySize));
            }
        }
    }

    public DateTime? LastModified
    {
        get => _lastModified;
        set => SetProperty(ref _lastModified, value);
    }

    public bool IsExpanded
    {
        get => _isExpanded;
        set => SetProperty(ref _isExpanded, value);
    }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public ObservableCollection<ZipTreeNodeViewModel> Children { get; } = new();

    public Dictionary<string, ZipTreeNodeViewModel> ChildMap { get; } = new(StringComparer.OrdinalIgnoreCase);

    public string Icon => IsDirectory ? "📁" : "📄";

    public string DisplaySize => IsDirectory ? "" : FileSizeHelper.ToReadableSize(SizeBytes);

    public void ReplaceChildren(IEnumerable<ZipTreeNodeViewModel> orderedChildren)
    {
        Children.Clear();
        foreach (var child in orderedChildren)
            Children.Add(child);
    }
}