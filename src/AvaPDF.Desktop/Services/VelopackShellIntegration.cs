using Microsoft.Win32;
using System.ComponentModel;
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

        var paths = GetPaths();
        if (!File.Exists(paths.SourceDll))
        {
            throw new FileNotFoundException("The packaged AvaPDF shell extension was not found.", paths.SourceDll);
        }

        Directory.CreateDirectory(paths.ShellDirectory);
        if (File.Exists(paths.StableDll) && !FileBytesEqual(paths.SourceDll, paths.StableDll))
        {
            ReleaseShellProcesses([paths.StableDll]);
            File.Copy(paths.SourceDll, paths.StableDll, true);
        }
        else if (!File.Exists(paths.StableDll))
        {
            File.Copy(paths.SourceDll, paths.StableDll, true);
        }

        using (var clsid = Registry.CurrentUser.CreateSubKey(ClsidKey))
        {
            clsid.SetValue("AppExecutable", paths.Launcher, RegistryValueKind.String);
        }
        using (var inproc = Registry.CurrentUser.CreateSubKey(ClsidKey + @"\InprocServer32"))
        {
            inproc.SetValue(null, paths.StableDll, RegistryValueKind.String);
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

        var paths = GetPaths();
        if (Directory.Exists(paths.ShellDirectory))
        {
            var shellFiles = Directory.GetFiles(paths.ShellDirectory, "*.dll")
                .Concat(Directory.GetFiles(paths.ShellDirectory, "*.previous"))
                .ToArray();
            if (shellFiles.Length > 0)
            {
                ReleaseShellProcesses(shellFiles);
            }
            foreach (var previous in Directory.GetFiles(paths.ShellDirectory, "*.previous"))
            {
                File.Delete(previous);
            }
        }

        Registry.CurrentUser.DeleteSubKeyTree(HandlerKey, false);
        Registry.CurrentUser.DeleteSubKeyTree(ClsidKey, false);
        SHChangeNotify(0x08000000, 0x0000, IntPtr.Zero, IntPtr.Zero);
    }

    private static ShellPaths GetPaths()
    {
        var processPath = Environment.ProcessPath ?? throw new InvalidOperationException("The Velopack process path is unavailable.");
        var currentDirectory = Path.GetDirectoryName(processPath) ?? throw new InvalidOperationException("The Velopack current directory is unavailable.");
        var installDirectory = Directory.GetParent(currentDirectory)?.FullName
            ?? throw new InvalidOperationException("The Velopack install directory is unavailable.");
        var shellDirectory = Path.Combine(installDirectory, "shell");
        return new ShellPaths(
            shellDirectory,
            Path.Combine(currentDirectory, "shell", "AvaPDF.Shell.dll"),
            Path.Combine(shellDirectory, "AvaPDF.Shell.dll"),
            Path.Combine(installDirectory, "AvaPDF.exe"));
    }

    private static void ReleaseShellProcesses(string[] files)
    {
        var sessionKey = Guid.NewGuid().ToString("N");
        var result = RmStartSession(out var session, 0, sessionKey);
        if (result != 0)
        {
            throw new Win32Exception(result, "Unable to start a Restart Manager session for the shell extension.");
        }
        try
        {
            result = RmRegisterResources(session, (uint)files.Length, files, 0, IntPtr.Zero, 0, null);
            if (result != 0)
            {
                throw new Win32Exception(result, "Unable to register the shell extension with Restart Manager.");
            }
            result = RmShutdown(session, RmForceShutdown, IntPtr.Zero);
            if (result != 0)
            {
                throw new Win32Exception(result, "Unable to release processes using the shell extension.");
            }
            result = RmRestart(session, 0, IntPtr.Zero);
            if (result != 0)
            {
                throw new Win32Exception(result, "Unable to restart applications released by the shell extension update.");
            }
        }
        finally
        {
            RmEndSession(session);
        }
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

    private const int RmForceShutdown = 1;

    [DllImport("rstrtmgr.dll", CharSet = CharSet.Unicode)]
    private static extern int RmStartSession(out int session, int sessionFlags, string sessionKey);

    [DllImport("rstrtmgr.dll", CharSet = CharSet.Unicode)]
    private static extern int RmRegisterResources(
        int session, uint fileCount, string[] files, uint applicationCount, IntPtr applications,
        uint serviceCount, string[]? services);

    [DllImport("rstrtmgr.dll")]
    private static extern int RmShutdown(int session, int action, IntPtr statusCallback);

    [DllImport("rstrtmgr.dll")]
    private static extern int RmRestart(int session, int restartFlags, IntPtr statusCallback);

    [DllImport("rstrtmgr.dll")]
    private static extern int RmEndSession(int session);

    [DllImport("shell32.dll")]
    private static extern void SHChangeNotify(uint eventId, uint flags, IntPtr item1, IntPtr item2);

    private sealed record ShellPaths(
        string ShellDirectory,
        string SourceDll,
        string StableDll,
        string Launcher);
}
