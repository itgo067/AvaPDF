using AvaPDF.Desktop.ViewModels;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace AvaPDF.Desktop.Views;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();
        DataContext = new AboutViewModel();
    }

    private void OnGitHubClick(object? sender, RoutedEventArgs e) =>
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = AppInfo.GitHubUrl,
            UseShellExecute = true
        });

    private void OnCloseClick(object? sender, RoutedEventArgs e) => Close();
}
