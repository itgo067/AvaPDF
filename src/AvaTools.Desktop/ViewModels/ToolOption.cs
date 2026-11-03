using AvaTools.Application.Operations;

namespace AvaTools.Desktop.ViewModels;

public sealed record SplitModeOption(PdfSplitMode Value, string Label);

public sealed record OptimizationOption(PdfOptimizationLevel Value, string Label);

public sealed class LocalFileItem(string path)
{
    public string Path { get; } = path;

    public string Name => System.IO.Path.GetFileName(Path);

    public string Details
    {
        get
        {
            var length = new FileInfo(Path).Length;
            return length >= 1024 * 1024
                ? $"{length / 1024.0 / 1024.0:F1} MB"
                : $"{Math.Max(1, length / 1024.0):F0} KB";
        }
    }
}
