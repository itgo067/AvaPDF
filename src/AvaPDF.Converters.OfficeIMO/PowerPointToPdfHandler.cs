using AvaPDF.Application.Operations;
using AvaPDF.Domain;
using OfficeIMO.PowerPoint;
using OfficeIMO.PowerPoint.Pdf;

namespace AvaPDF.Converters.OfficeIMO;

public sealed class PowerPointToPdfHandler : PdfOperationHandler<DocumentToPdfRequest>
{
    private static readonly HashSet<string> Extensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".ppt", ".pptx", ".pptm"
    };

    public override PdfOperationKind Kind => PdfOperationKind.PowerPointToPdf;

    protected override Task<PdfOperationResult> HandleAsync(
        DocumentToPdfRequest request,
        IProgress<PdfOperationProgress>? progress,
        CancellationToken cancellationToken) =>
        DocumentConversion.ExecuteAsync(request, progress, cancellationToken, Extensions,
            (input, output, token) => ConvertAsync(request, input, output, token));

    private static async Task<IReadOnlyList<string>> ConvertAsync(
        DocumentToPdfRequest request,
        Stream input,
        Stream output,
        CancellationToken cancellationToken)
    {
        var options = new PowerPointToPdfOptions();
        DocumentConversion.ConfigureUnicodeFallbacks(options.PdfOptions);
        using var presentation = string.Equals(Path.GetExtension(request.Source.Name), ".ppt", StringComparison.OrdinalIgnoreCase)
            ? PowerPointPresentation.LoadLegacyPpt(input, options: null)
            : await PowerPointPresentation.LoadAsync(input, options: null, cancellationToken).ConfigureAwait(false);
        var result = presentation.SaveAsPdfResult(output, options, cancellationToken);
        return DocumentConversion.Complete(result);
    }
}
