using System.Windows;
using System.Windows.Controls;
using VersionManager.ViewModels;

namespace VersionManager;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }

    private void ZipTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (DataContext is MainViewModel vm && e.NewValue is ZipTreeNodeViewModel node)
        {
            vm.SelectedZipNode = node;
        }
    }
}