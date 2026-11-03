using AvaPDF.Application.Common;
using AvaPDF.Application.Operations;
using AvaPDF.Domain;
using OfficeIMO.Pdf;

namespace AvaPDF.Pdf.OfficeIMO.Features;

public sealed class SplitPdfHandler : PdfOperationHandler<SplitPdfRequest>
{
    public override PdfOperationKind Kind => PdfOperationKind.Split;

    protected override async Task<PdfOperationResult> HandleAsync(
        SplitPdfRequest request,
        IProgress<PdfOperationProgress>? progress,
        CancellationToken cancellationToken)
    {
        OfficePdf.RequirePdf(request.Source);
        OfficePdf.Report(progress, 5, "正在读取 PDF");
        var document = await OfficePdf.LoadAsync(request.Source, cancellationToken).ConfigureAwait(false);
        var pageCount = OfficePdf.GetPageCount(document, cancellationToken);

        IReadOnlyList<PdfDocument> parts;
        switch (request.Mode)
        {
            case PdfSplitMode.EveryPage:
                parts = document.Pages.Split(cancellationToken);
                break;
            case PdfSplitMode.PageCount:
                if (request.PagesPerPart < 1)
                {
                    throw new ArgumentException("每份页数必须大于 0。 ");
                }

                parts = document.Pages.Split(request.PagesPerPart, cancellationToken);
                break;
            case PdfSplitMode.CustomRanges:
                PageRangeExpression.Resolve(request.PageRanges, pageCount);
                parts = document.Pages.Split(request.PageRanges!, cancellationToken);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(request), "不支持的拆分方式。 ");
        }

        var stem = OfficePdf.Stem(request.Source);
        var outputFiles = new List<string>(parts.Count);
        for (var index = 0; index < parts.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var fileName = $"{stem}-part-{index + 1:000}.pdf";
            OfficePdf.Report(progress, 25 + index * 70.0 / parts.Count, $"正在保存第 {index + 1}/{parts.Count} 份");
            await OfficePdf.SaveAsync(parts[index], request.Output, fileName, cancellationToken).ConfigureAwait(false);
            outputFiles.Add(fileName);
        }

        OfficePdf.Report(progress, 100, "拆分完成");
        return new PdfOperationResult(outputFiles, $"已生成 {parts.Count} 个 PDF 文件。");
    }
}
