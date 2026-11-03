namespace AvaPDF.Domain;

public sealed record PdfOperationResult(
    IReadOnlyList<string> OutputFiles,
    string Message,
    IReadOnlyList<string>? Warnings = null)
{
    public IReadOnlyList<string> ConversionWarnings => Warnings ?? [];

    public static PdfOperationResult Single(string outputFile, string message, IReadOnlyList<string>? warnings = null) =>
        new([outputFile], message, warnings);
}

public sealed record PdfOperationProgress(double? Percent, string Message);
