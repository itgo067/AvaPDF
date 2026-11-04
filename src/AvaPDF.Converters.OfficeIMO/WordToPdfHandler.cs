using AvaPDF.Application.Operations;
using OfficeIMO;
using AvaPDF.Domain;
using OfficeIMO.Word;
using OfficeIMO.Word.Pdf;

namespace AvaPDF.Converters.OfficeIMO;

public sealed class WordToPdfHandler : PdfOperationHandler<DocumentToPdfRequest>
{
    private static readonly HashSet<string> Extensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".doc", ".docx", ".docm", ".dotx", ".dotm"
    };

    public override PdfOperationKind Kind => PdfOperationKind.WordToPdf;

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
        var options = new WordToPdfOptions();
        DocumentConversion.ConfigureUnicodeFallbacks(options.PdfOptions);
        if (string.Equals(Path.GetExtension(request.Source.Name), ".doc", StringComparison.OrdinalIgnoreCase))
        {
            var conversion = LegacyDocPdfConverter.ToPdfDocumentResult(
                input,
                options,
                importOptions: null,
                lossPolicy: OfficeConversionLossPolicy.Block,
                cancellationToken);
            var saveResult = conversion.SaveResult(output, cancellationToken);
            var importWarnings = conversion.SourceConversionReports
                .SelectMany(report => report.FidelityDiagnostics)
                .Select(finding => finding.ToString() ?? "旧版 Word 导入警告");
            return importWarnings.Concat(DocumentConversion.Complete(saveResult))
                .Distinct(StringComparer.Ordinal)
                .ToArray();
        }

        using var document = await WordDocument.LoadAsync(input, options: null, cancellationToken).ConfigureAwait(false);
        var result = document.SaveAsPdfResult(output, options, cancellationToken);
        return DocumentConversion.Complete(result);
    }
}
