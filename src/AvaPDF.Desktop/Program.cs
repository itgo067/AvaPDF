using Avalonia;

namespace AvaPDF.Desktop;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        Velopack.VelopackApp.Build().Run();
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>()
        .UsePlatformDetect()
        .LogToTrace();
}
