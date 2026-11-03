using AvaPDF.Application.Operations;
using AvaPDF.Domain;
using OfficeIMO.Markdown;
using OfficeIMO.Markdown.Pdf;
using OfficeIMO.Pdf;

namespace AvaPDF.Converters.OfficeIMO;

public sealed class MarkdownToPdfHandler : PdfOperationHandler<DocumentToPdfRequest>
{
    private static readonly HashSet<string> Extensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".md", ".markdown"
    };

    public override PdfOperationKind Kind => PdfOperationKind.MarkdownToPdf;

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
        var document = await MarkdownDoc.LoadAsync(input, options: null, encoding: null, cancellationToken).ConfigureAwait(false);
        var options = new MarkdownToPdfOptions { IncludeImages = true };
        var resourceRoot = DocumentConversion.ResolveResourceRoot(request.ResourceBaseDirectory);
        if (resourceRoot is not null)
        {
            options.BaseDirectory = resourceRoot;
            options.ResourcePolicy = PdfResourcePolicy.CreateTrustedHost();
        }

        var result = document.SaveAsPdfResult(output, options, cancellationToken);
        return DocumentConversion.Complete(result);
    }
}
