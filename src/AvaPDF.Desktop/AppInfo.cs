using System.Reflection;

namespace AvaPDF.Desktop;

public static class AppInfo
{
    private static readonly string InformationalVersion =
        typeof(AppInfo).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? "0.0.0";

    public static string Version => InformationalVersion.Split('+')[0];

    public static string CommitHash
    {
        get
        {
            var separator = InformationalVersion.IndexOf('+');
            return separator >= 0 && separator < InformationalVersion.Length - 1
                ? InformationalVersion[(separator + 1)..]
                : string.Empty;
        }
    }

    public static string ShortCommitHash => CommitHash.Length > 7 ? CommitHash[..7] : CommitHash;
}
