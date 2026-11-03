using Avalonia.Controls;
using Avalonia.LogicalTree;
using Ursa.Controls;

namespace AvaPDF.Desktop.Views;

public partial class MainWindow : Window
{
    public MainWindow() => InitializeComponent();

    private async void OnAboutClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e) =>
        await new AboutWindow().ShowDialog(this);

    private void OnToolNavMenuContainerPrepared(object? sender, ContainerPreparedEventArgs e)
    {
        if (e.Container is NavMenuItem item)
        {
            CollapseIfGroup(item);
        }
    }

    private void OnToolNavMenuLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        foreach (var item in ToolNavMenu.GetLogicalDescendants().OfType<NavMenuItem>())
        {
            CollapseIfGroup(item);
        }
    }

    private static void CollapseIfGroup(NavMenuItem item)
    {
        if (item.ItemCount > 0)
        {
            item.IsVerticalCollapsed = true;
        }
    }
}
