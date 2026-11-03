using Avalonia.Controls;
using Avalonia.Threading;
using Ursa.Controls;

namespace AvaPDF.Desktop.Views;

public partial class MainWindow : Window
{
    private bool _allowNavigationSelection;

    public MainWindow() => InitializeComponent();

    private void OnToolSelectionChanging(object? sender, SelectionChangingEventArgs e)
    {
        if (!_allowNavigationSelection)
        {
            e.CanSelect = false;
        }
    }

    private void OnToolNavMenuLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e) =>
        Dispatcher.UIThread.Post(() => _allowNavigationSelection = true, DispatcherPriority.Background);
}
