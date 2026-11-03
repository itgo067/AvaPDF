using System.Diagnostics;

namespace AvaPDF.Desktop.Services;

public interface IShellService
{
    void OpenFolder(string path);

    void OpenFile(string path);
}

public sealed class ShellService : IShellService
{
    public void OpenFolder(string path)
    {
        Directory.CreateDirectory(path);
        Open(path);
    }

    public void OpenFile(string path) => Open(path);

    private static void Open(string path) => Process.Start(new ProcessStartInfo
    {
        FileName = path,
        UseShellExecute = true
    });
}
