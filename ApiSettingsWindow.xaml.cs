using System.Windows;

namespace VersionManager;

public partial class ApiSettingsWindow : Window
{
    public string ApiUrl { get; set; } = "";

    public ApiSettingsWindow(string currentUrl)
    {
        InitializeComponent();
        ApiUrl = currentUrl;
        DataContext = this;
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