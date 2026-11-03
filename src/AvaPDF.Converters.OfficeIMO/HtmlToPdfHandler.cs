using AvaPDF.Application.Operations;
using AvaPDF.Domain;
using OfficeIMO.Html;
using OfficeIMO.Html.Pdf;
using OfficeIMO.Pdf;

namespace AvaPDF.Converters.OfficeIMO;

public sealed class HtmlToPdfHandler : PdfOperationHandler<DocumentToPdfRequest>
{
    private static readonly HashSet<string> Extensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".html", ".htm"
    };

    public override PdfOperationKind Kind => PdfOperationKind.HtmlToPdf;

    protected override Task<PdfOperationResult> HandleAsync(
        DocumentToPdfRequest request,
        IProgress<PdfOperationProgress>? progress,
        CancellationToken cancellationToken) =>
        DocumentConversion.ExecuteAsync(request, progress, cancellationToken, Extensions,
            (input, output, token) => ConvertAsync(request, input, output, token));

    private static Task<IReadOnlyList<string>> ConvertAsync(
        DocumentToPdfRequest request,
        Stream input,
        Stream output,
        CancellationToken cancellationToken)
    {
        var resourceRoot = DocumentConversion.ResolveResourceRoot(request.ResourceBaseDirectory);
        var inputOptions = new HtmlConversionDocumentOptions();
        var pdfOptions = new HtmlToPdfOptions();
        if (resourceRoot is not null)
        {
            inputOptions.Trust = HtmlInputTrust.Trusted;
            inputOptions.BaseUri = new Uri(resourceRoot + Path.DirectorySeparatorChar);
            pdfOptions.ResourcePolicy = PdfResourcePolicy.CreateTrustedHost();
        }

        var document = HtmlConversionDocument.Load(input, inputOptions, encoding: null);
        var report = document.SaveAsPdf(output, pdfOptions, cancellationToken);
        IReadOnlyList<string> warnings = report.Warnings
            .Select(warning => warning.ToString())
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        return Task.FromResult(warnings);
    }
}
