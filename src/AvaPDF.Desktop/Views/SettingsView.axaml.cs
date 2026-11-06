using Avalonia.Controls;

namespace AvaPDF.Desktop.Views;

public partial class SettingsView : UserControl
{
    public SettingsView() => InitializeComponent();

    private void OnSaveProxyClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e) =>
        (DataContext as ViewModels.MainWindowViewModel)?.SaveProxySettings();

    private async void OnAboutClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (TopLevel.GetTopLevel(this) is Window window)
        {
            await new AboutWindow().ShowDialog(window);
        }
    }
}
