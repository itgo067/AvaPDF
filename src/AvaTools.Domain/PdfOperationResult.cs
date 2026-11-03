namespace AvaTools.Domain;

public sealed record PdfOperationResult(
    IReadOnlyList<string> OutputFiles,
    string Message)
{
    public static PdfOperationResult Single(string outputFile, string message) =>
        new([outputFile], message);
}

public sealed record PdfOperationProgress(double? Percent, string Message);
