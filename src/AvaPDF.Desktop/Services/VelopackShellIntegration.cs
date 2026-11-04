using Microsoft.Win32;
using System.Runtime.InteropServices;

namespace AvaPDF.Desktop.Services;

internal static class VelopackShellIntegration
{
    private const string Clsid = "{6F4A17D2-6A4B-4F1E-A6B3-0A9A0DFD0001}";
    private const string ClsidKey = @"Software\Classes\CLSID\" + Clsid;
    private const string HandlerKey = @"Software\Classes\SystemFileAssociations\.pdf\ShellEx\ContextMenuHandlers\AvaPDF";

    public static void Install()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var processPath = Environment.ProcessPath ?? throw new InvalidOperationException("The Velopack process path is unavailable.");
        var currentDirectory = Path.GetDirectoryName(processPath) ?? throw new InvalidOperationException("The Velopack current directory is unavailable.");
        var installDirectory = Directory.GetParent(currentDirectory)?.FullName
            ?? throw new InvalidOperationException("The Velopack install directory is unavailable.");
        var sourceDll = Path.Combine(currentDirectory, "shell", "AvaPDF.Shell.dll");
        var shellDirectory = Path.Combine(installDirectory, "shell");
        var stableDll = Path.Combine(shellDirectory, "AvaPDF.Shell.dll");
        var launcher = Path.Combine(installDirectory, "AvaPDF Velopack.exe");

        if (!File.Exists(sourceDll))
        {
            throw new FileNotFoundException("The packaged AvaPDF shell extension was not found.", sourceDll);
        }

        Directory.CreateDirectory(shellDirectory);
        foreach (var previous in Directory.GetFiles(shellDirectory, "*.previous"))
        {
            try
            {
                File.Delete(previous);
            }
            catch (IOException)
            {
                // A process still has the previous shell extension loaded.
            }
            catch (UnauthorizedAccessException)
            {
                // The file can be retried during the next install or update.
            }
        }
        if (!File.Exists(stableDll) || !FileBytesEqual(sourceDll, stableDll))
        {
            if (File.Exists(stableDll))
            {
                File.Move(stableDll, stableDll + ".previous", true);
            }
            File.Copy(sourceDll, stableDll, true);
        }

        using (var clsid = Registry.CurrentUser.CreateSubKey(ClsidKey))
        {
            clsid.SetValue("AppExecutable", launcher, RegistryValueKind.String);
        }
        using (var inproc = Registry.CurrentUser.CreateSubKey(ClsidKey + @"\InprocServer32"))
        {
            inproc.SetValue(null, stableDll, RegistryValueKind.String);
            inproc.SetValue("ThreadingModel", "Both", RegistryValueKind.String);
        }
        using (var handler = Registry.CurrentUser.CreateSubKey(HandlerKey))
        {
            handler.SetValue(null, Clsid, RegistryValueKind.String);
        }
        SHChangeNotify(0x08000000, 0x0000, IntPtr.Zero, IntPtr.Zero);
    }

    public static void Uninstall()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        Registry.CurrentUser.DeleteSubKeyTree(HandlerKey, false);
        Registry.CurrentUser.DeleteSubKeyTree(ClsidKey, false);
        SHChangeNotify(0x08000000, 0x0000, IntPtr.Zero, IntPtr.Zero);
    }

    private static bool FileBytesEqual(string first, string second)
    {
        using var left = new FileStream(first, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        using var right = new FileStream(second, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        if (left.Length != right.Length)
        {
            return false;
        }
        var leftBuffer = new byte[81920];
        var rightBuffer = new byte[81920];
        int read;
        while ((read = left.Read(leftBuffer)) > 0)
        {
            if (right.Read(rightBuffer, 0, read) != read)
            {
                return false;
            }
            if (!leftBuffer.AsSpan(0, read).SequenceEqual(rightBuffer.AsSpan(0, read)))
            {
                return false;
            }
        }
        return true;
    }

    [DllImport("shell32.dll")]
    private static extern void SHChangeNotify(uint eventId, uint flags, IntPtr item1, IntPtr item2);
}
