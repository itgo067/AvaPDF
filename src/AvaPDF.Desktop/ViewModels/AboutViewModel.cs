namespace AvaPDF.Desktop.ViewModels;

public sealed class AboutViewModel
{
    public string Version => AppInfo.Version;

    public string CommitHash => AppInfo.ShortCommitHash;

    public bool HasCommitHash => !string.IsNullOrWhiteSpace(CommitHash);
}
