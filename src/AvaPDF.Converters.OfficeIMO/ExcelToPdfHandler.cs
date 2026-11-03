using AvaPDF.Application.Operations;
using AvaPDF.Domain;
using OfficeIMO.Excel;
using OfficeIMO.Excel.Pdf;

namespace AvaPDF.Converters.OfficeIMO;

public sealed class ExcelToPdfHandler : PdfOperationHandler<DocumentToPdfRequest>
{
    private static readonly HashSet<string> Extensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".xls", ".xlsx", ".xlsm", ".xltx", ".xltm"
    };

    public override PdfOperationKind Kind => PdfOperationKind.ExcelToPdf;

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
        using var workbook = string.Equals(Path.GetExtension(request.Source.Name), ".xls", StringComparison.OrdinalIgnoreCase)
            ? ExcelDocument.LoadLegacyXls(input, options: null)
            : await ExcelDocument.LoadAsync(input, options: null, cancellationToken).ConfigureAwait(false);
        var result = workbook.SaveAsPdfResult(output, new ExcelToPdfOptions(), cancellationToken);
        return DocumentConversion.Complete(result);
    }
}
