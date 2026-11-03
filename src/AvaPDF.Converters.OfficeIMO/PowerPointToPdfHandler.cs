using AvaPDF.Application.Operations;
using AvaPDF.Domain;
using OfficeIMO.PowerPoint;
using OfficeIMO.PowerPoint.Pdf;

namespace AvaPDF.Converters.OfficeIMO;

public sealed class PowerPointToPdfHandler : PdfOperationHandler<DocumentToPdfRequest>
{
    private static readonly HashSet<string> Extensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pptx", ".pptm"
    };

    public override PdfOperationKind Kind => PdfOperationKind.PowerPointToPdf;

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
        using var presentation = await PowerPointPresentation.LoadAsync(input, options: null, cancellationToken).ConfigureAwait(false);
        var result = presentation.SaveAsPdfResult(output, new PowerPointToPdfOptions(), cancellationToken);
        return DocumentConversion.Complete(result);
    }
}
