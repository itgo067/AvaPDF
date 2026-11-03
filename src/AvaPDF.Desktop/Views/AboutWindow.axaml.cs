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

    private void OnCloseClick(object? sender, RoutedEventArgs e) => Close();
}
