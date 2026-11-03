using AvaPDF.Application.Operations;
using AvaPDF.Domain;
using OfficeIMO.Word;
using OfficeIMO.Word.Pdf;

namespace AvaPDF.Converters.OfficeIMO;

public sealed class WordToPdfHandler : PdfOperationHandler<DocumentToPdfRequest>
{
    private static readonly HashSet<string> Extensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".docx", ".docm", ".dotx", ".dotm"
    };

    public override PdfOperationKind Kind => PdfOperationKind.WordToPdf;

    protected override Task<PdfOperationResult> HandleAsync(
        DocumentToPdfRequest request,
        IProgress<PdfOperationProgress>? progress,
        CancellationToken cancellationToken) =>
        DocumentConversion.ExecuteAsync(request, progress, cancellationToken, Extensions, ConvertAsync);

    private static async Task<IReadOnlyList<string>> ConvertAsync(
        Stream input,
        Stream output,
        CancellationToken cancellationToken)
    {
        using var document = await WordDocument.LoadAsync(input, options: null, cancellationToken).ConfigureAwait(false);
        var result = document.SaveAsPdfResult(output, new WordToPdfOptions(), cancellationToken);
        return DocumentConversion.Complete(result);
    }
}
