using VersionManager.ViewModels.Base;

namespace VersionManager.Models;

public sealed class WorkflowStep : ViewModelBase
{
    private string _label = "";
    private string _state = "Pending";

    public string Label
    {
        get => _label;
        set => SetProperty(ref _label, value);
    }

    public string State
    {
        get => _state;
        set => SetProperty(ref _state, value);
    }
}