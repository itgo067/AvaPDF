using AvaPDF.Converters.OfficeIMO;
using AvaPDF.Desktop.Services;
using AvaPDF.Desktop.ViewModels;
using AvaPDF.Desktop.Views;
using AvaPDF.Pdf.OfficeIMO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using Microsoft.Extensions.DependencyInjection;

namespace AvaPDF.Desktop;

public sealed partial class App : Avalonia.Application
{
    private TrayWindowService? _tray;
    private MainWindow? _mainWindow;
    private MainWindowViewModel? _viewModel;
    private IShellService? _shell;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var trayIcon = new TrayIcon
            {
                Icon = new WindowIcon(AssetLoader.Open(new Uri("avares://AvaPDF.Desktop/Assets/AppIcon.ico"))),
                ToolTipText = "AvaPDF · 本地 PDF 工作台"
            };
            trayIcon.Clicked += OnTrayIconClicked;
            var trayIcons = new TrayIcons { trayIcon };
            SetValue(TrayIcon.IconsProperty, trayIcons);
            if (trayIcons is { Count: > 0 })
            {
                var menu = new NativeMenu();
                menu.Items.Add(new NativeMenuItem { Header = "显示主窗口" });
                menu.Items.Add(new NativeMenuItem { Header = "隐藏主窗口" });
                menu.Items.Add(new NativeMenuItemSeparator());
                menu.Items.Add(new NativeMenuItem { Header = "打开输出目录" });
                menu.Items.Add(new NativeMenuItemSeparator());
                menu.Items.Add(new NativeMenuItem { Header = "退出 AvaPDF" });
                ((NativeMenuItem)menu.Items[0]).Click += OnTrayShowClick;
                ((NativeMenuItem)menu.Items[1]).Click += OnTrayHideClick;
                ((NativeMenuItem)menu.Items[3]).Click += OnTrayOpenOutputClick;
                ((NativeMenuItem)menu.Items[5]).Click += OnTrayExitClick;
                trayIcons[0].Menu = menu;
            }
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            var services = new ServiceCollection();
            services.AddOfficeImoPdf();
            services.AddOfficeImoDocumentConverters();
            services.AddSingleton<IFileDialogService, FileDialogService>();
            services.AddSingleton<IShellService, ShellService>();
            services.AddSingleton<IThemeService, ThemeService>();
            services.AddSingleton<IUpdateService, UpdateService>();
            services.AddSingleton<IVelopackUpdateService, VelopackUpdateService>();
            services.AddSingleton<IProxySettingsService, ProxySettingsService>();
            services.AddSingleton<MainWindowViewModel>();

            var provider = services.BuildServiceProvider();
            provider.GetRequiredService<IThemeService>().ApplySavedTheme();
            var viewModel = provider.GetRequiredService<MainWindowViewModel>();
            var mainWindow = new MainWindow
            {
                DataContext = viewModel
            };
            _mainWindow = mainWindow;
            _viewModel = viewModel;
            _shell = provider.GetRequiredService<IShellService>();
            _tray = new TrayWindowService(
                () =>
                {
                    mainWindow.Show();
                    mainWindow.WindowState = WindowState.Normal;
                    mainWindow.Activate();
                },
                () => mainWindow.Hide(),
                () => desktop.Shutdown());
            mainWindow.Closing += (_, e) =>
            {
                if (_tray.ShouldCancelClose)
                {
                    e.Cancel = true;
                    _tray.Hide();
                }
            };
            desktop.MainWindow = mainWindow;
            mainWindow.Loaded += async (_, _) =>
            {
                await viewModel.ApplyStartupOptions(StartupOptions.Parse(desktop.Args));
                await viewModel.CheckForUpdatesAsync();
            };

            if (provider.GetRequiredService<IFileDialogService>() is FileDialogService dialogs)
            {
                dialogs.TopLevel = mainWindow;
            }
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void OnTrayIconClicked(object? sender, EventArgs e)
    {
        if (_mainWindow is null || _tray is null)
        {
            return;
        }
        if (_mainWindow.IsVisible)
        {
            _tray.Hide();
        }
        else
        {
            _tray.Show();
        }
    }

    private void OnTrayShowClick(object? sender, EventArgs e) => _tray?.Show();

    private void OnTrayHideClick(object? sender, EventArgs e) => _tray?.Hide();

    private void OnTrayOpenOutputClick(object? sender, EventArgs e)
    {
        if (_shell is not null && _viewModel is not null)
        {
            _shell.OpenFolder(_viewModel.Workspace.OutputDirectory);
        }
    }

    private void OnTrayExitClick(object? sender, EventArgs e) => _tray?.Exit();
}
