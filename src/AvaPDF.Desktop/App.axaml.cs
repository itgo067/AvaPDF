using AvaPDF.Converters.OfficeIMO;
using AvaPDF.Desktop.Services;
using AvaPDF.Desktop.ViewModels;
using AvaPDF.Desktop.Views;
using AvaPDF.Pdf.OfficeIMO;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;

namespace AvaPDF.Desktop;

public sealed class App : Avalonia.Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var services = new ServiceCollection();
            services.AddOfficeImoPdf();
            services.AddOfficeImoDocumentConverters();
            services.AddSingleton<IFileDialogService, FileDialogService>();
            services.AddSingleton<IShellService, ShellService>();
            services.AddSingleton<IThemeService, ThemeService>();
            services.AddSingleton<MainWindowViewModel>();

            var provider = services.BuildServiceProvider();
            provider.GetRequiredService<IThemeService>().ApplySavedTheme();
            var viewModel = provider.GetRequiredService<MainWindowViewModel>();
            var mainWindow = new MainWindow
            {
                DataContext = viewModel
            };
            desktop.MainWindow = mainWindow;
            mainWindow.Loaded += (_, _) =>
                viewModel.ApplyStartupOptions(StartupOptions.Parse(desktop.Args));

            if (provider.GetRequiredService<IFileDialogService>() is FileDialogService dialogs)
            {
                dialogs.TopLevel = mainWindow;
            }
        }

        base.OnFrameworkInitializationCompleted();
    }
}
