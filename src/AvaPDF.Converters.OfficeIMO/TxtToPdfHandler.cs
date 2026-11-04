using AvaPDF.Application.Operations;
using AvaPDF.Domain;
using OfficeIMO.Pdf;

namespace AvaPDF.Converters.OfficeIMO;

public sealed class TxtToPdfHandler : PdfOperationHandler<DocumentToPdfRequest>
{
    private const long MaximumInputBytes = 64L * 1024 * 1024;
    private static readonly HashSet<string> Extensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".txt"
    };

    public override PdfOperationKind Kind => PdfOperationKind.TxtToPdf;

    protected override Task<PdfOperationResult> HandleAsync(
        DocumentToPdfRequest request,
        IProgress<PdfOperationProgress>? progress,
        CancellationToken cancellationToken) =>
        DocumentConversion.ExecuteAsync(request, progress, cancellationToken, Extensions, ConvertAsync);

    private static Task<IReadOnlyList<string>> ConvertAsync(
        Stream input,
        Stream output,
        CancellationToken cancellationToken)
    {
        var pdfOptions = new PdfOptions();
        pdfOptions.TryUseDefaultDocumentFontFallback(true);
        pdfOptions.TryRegisterDefaultDocumentMonospaceFontFallback(true);
        DocumentConversion.ConfigureUnicodeFallbacks(pdfOptions);
        var conversion = PdfPlainTextConverter.ToPdfDocumentResult(
            input,
            new PdfPlainTextOptions { PdfOptions = pdfOptions },
            MaximumInputBytes,
            cancellationToken);
        var result = conversion.SaveResult(output, cancellationToken);
        return Task.FromResult(DocumentConversion.Complete(result));
    }
}
