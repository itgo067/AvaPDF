using AvaPDF.Application.Operations;
using AvaPDF.Domain;
using OfficeIMO.Rtf;
using OfficeIMO.Rtf.Pdf;

namespace AvaPDF.Converters.OfficeIMO;

public sealed class RtfToPdfHandler : PdfOperationHandler<DocumentToPdfRequest>
{
    private static readonly HashSet<string> Extensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".rtf"
    };

    public override PdfOperationKind Kind => PdfOperationKind.RtfToPdf;

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
        var document = await RtfDocument.LoadAsync(input, options: null, encoding: null, cancellationToken).ConfigureAwait(false);
        var result = document.SaveAsPdfResult(output, new RtfToPdfOptions(), cancellationToken);
        return DocumentConversion.Complete(result);
    }
}
