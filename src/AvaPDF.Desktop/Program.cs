using Avalonia;

namespace AvaPDF.Desktop;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        var velopack = Velopack.VelopackApp.Build();
        if (OperatingSystem.IsWindows())
        {
            velopack = velopack
                .OnAfterInstallFastCallback(_ => Services.VelopackShellIntegration.Install())
                .OnAfterUpdateFastCallback(_ => Services.VelopackShellIntegration.Install())
                .OnBeforeUninstallFastCallback(_ => Services.VelopackShellIntegration.Uninstall());
        }
        velopack.Run();
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>()
        .UsePlatformDetect()
        .LogToTrace();
}
