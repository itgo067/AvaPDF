using Avalonia.Controls;

namespace AvaPDF.Desktop.Views;

public partial class SettingsView : UserControl
{
    public SettingsView() => InitializeComponent();

    private void OnSaveProxyClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e) =>
        (DataContext as ViewModels.MainWindowViewModel)?.SaveProxySettings();
}
