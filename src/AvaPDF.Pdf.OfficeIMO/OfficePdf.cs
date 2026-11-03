using AvaPDF.Application.Abstractions;
using AvaPDF.Domain;
using OfficeIMO.Pdf;

namespace AvaPDF.Pdf.OfficeIMO;

internal static class OfficePdf
{
    public static async Task<PdfDocument> LoadAsync(IPdfSource source, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(source);
        await using var input = await source.OpenReadAsync(cancellationToken).ConfigureAwait(false);
        return PdfDocument.Load(input, null, cancellationToken);
    }

    public static async Task SaveAsync(
        PdfDocument document,
        IPdfOutputTarget output,
        string fileName,
        CancellationToken cancellationToken)
    {
        await using var stream = await output.OpenWriteAsync(fileName, cancellationToken).ConfigureAwait(false);
        document.Save(stream, cancellationToken);
        await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    public static int GetPageCount(PdfDocument document, CancellationToken cancellationToken) =>
        document.Inspect(options: null, cancellationToken).PageCount;

    public static string Stem(IPdfSource source)
    {
        var stem = Path.GetFileNameWithoutExtension(source.Name);
        if (string.IsNullOrWhiteSpace(stem))
        {
            return "document";
        }

        var invalid = Path.GetInvalidFileNameChars();
        return string.Concat(stem.Select(character => invalid.Contains(character) ? '_' : character));
    }

    public static void RequirePdf(IPdfSource source)
    {
        if (!string.Equals(Path.GetExtension(source.Name), ".pdf", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"“{source.Name}”不是 PDF 文件。 ");
        }
    }

    public static void Report(
        IProgress<PdfOperationProgress>? progress,
        double? percent,
        string message) => progress?.Report(new PdfOperationProgress(percent, message));
}
