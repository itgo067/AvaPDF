using AvaTools.Application.Common;
using AvaTools.Application.Operations;
using AvaTools.Domain;
using OfficeIMO.Pdf;

namespace AvaTools.Pdf.OfficeIMO.Features;

public sealed class ExtractPdfHandler : PdfOperationHandler<ExtractPdfRequest>
{
    public override PdfOperationKind Kind => PdfOperationKind.Extract;

    protected override async Task<PdfOperationResult> HandleAsync(
        ExtractPdfRequest request,
        IProgress<PdfOperationProgress>? progress,
        CancellationToken cancellationToken)
    {
        OfficePdf.RequirePdf(request.Source);
        var document = await OfficePdf.LoadAsync(request.Source, cancellationToken).ConfigureAwait(false);
        var pages = PageRangeExpression.Resolve(request.PageRanges, OfficePdf.GetPageCount(document, cancellationToken));
        OfficePdf.Report(progress, 40, $"正在提取 {pages.Count} 页");
        var result = document.Pages.Extract(request.PageRanges, cancellationToken);
        var fileName = $"{OfficePdf.Stem(request.Source)}-extracted.pdf";
        await OfficePdf.SaveAsync(result, request.Output, fileName, cancellationToken).ConfigureAwait(false);
        OfficePdf.Report(progress, 100, "提取完成");
        return PdfOperationResult.Single(fileName, $"已提取 {pages.Count} 页。");
    }
}

public sealed class DeletePdfHandler : PdfOperationHandler<DeletePdfRequest>
{
    public override PdfOperationKind Kind => PdfOperationKind.Delete;

    protected override async Task<PdfOperationResult> HandleAsync(
        DeletePdfRequest request,
        IProgress<PdfOperationProgress>? progress,
        CancellationToken cancellationToken)
    {
        OfficePdf.RequirePdf(request.Source);
        var document = await OfficePdf.LoadAsync(request.Source, cancellationToken).ConfigureAwait(false);
        var pageCount = OfficePdf.GetPageCount(document, cancellationToken);
        var pages = PageRangeExpression.Resolve(request.PageRanges, pageCount, distinct: true);
        if (pages.Count >= pageCount)
        {
            throw new ArgumentException("不能删除文档的全部页面。 ");
        }

        OfficePdf.Report(progress, 40, $"正在删除 {pages.Count} 页");
        var result = document.Pages.Delete(request.PageRanges, cancellationToken);
        var fileName = $"{OfficePdf.Stem(request.Source)}-pages-deleted.pdf";
        await OfficePdf.SaveAsync(result, request.Output, fileName, cancellationToken).ConfigureAwait(false);
        OfficePdf.Report(progress, 100, "删除完成");
        return PdfOperationResult.Single(fileName, $"已删除 {pages.Count} 页，保留 {pageCount - pages.Count} 页。");
    }
}

public sealed class RotatePdfHandler : PdfOperationHandler<RotatePdfRequest>
{
    public override PdfOperationKind Kind => PdfOperationKind.Rotate;

    protected override async Task<PdfOperationResult> HandleAsync(
        RotatePdfRequest request,
        IProgress<PdfOperationProgress>? progress,
        CancellationToken cancellationToken)
    {
        if (request.Angle is not (90 or 180 or 270))
        {
            throw new ArgumentException("旋转角度仅支持 90°、180° 或 270°。 ");
        }

        OfficePdf.RequirePdf(request.Source);
        var document = await OfficePdf.LoadAsync(request.Source, cancellationToken).ConfigureAwait(false);
        var pageCount = OfficePdf.GetPageCount(document, cancellationToken);
        var pages = PageRangeExpression.Resolve(request.PageRanges, pageCount, allowEmpty: true, distinct: true);
        var selection = string.IsNullOrWhiteSpace(request.PageRanges)
            ? Enumerable.Range(1, pageCount).ToArray()
            : pages.ToArray();

        OfficePdf.Report(progress, 40, $"正在旋转 {selection.Length} 页");
        var result = document.Pages.RotateBy(request.Angle, selection);
        var fileName = $"{OfficePdf.Stem(request.Source)}-rotated.pdf";
        await OfficePdf.SaveAsync(result, request.Output, fileName, cancellationToken).ConfigureAwait(false);
        OfficePdf.Report(progress, 100, "旋转完成");
        return PdfOperationResult.Single(fileName, $"已将 {selection.Length} 页顺时针旋转 {request.Angle}°。");
    }
}

public sealed class ReorderPdfHandler : PdfOperationHandler<ReorderPdfRequest>
{
    public override PdfOperationKind Kind => PdfOperationKind.Reorder;

    protected override async Task<PdfOperationResult> HandleAsync(
        ReorderPdfRequest request,
        IProgress<PdfOperationProgress>? progress,
        CancellationToken cancellationToken)
    {
        OfficePdf.RequirePdf(request.Source);
        var document = await OfficePdf.LoadAsync(request.Source, cancellationToken).ConfigureAwait(false);
        var pageCount = OfficePdf.GetPageCount(document, cancellationToken);
        var order = PageRangeExpression.Resolve(request.PageOrder, pageCount);
        if (order.Count != pageCount || order.Distinct().Count() != pageCount)
        {
            throw new ArgumentException($"重排顺序必须且不重复地包含全部 {pageCount} 页，例如 3,1-2。 ");
        }

        OfficePdf.Report(progress, 40, "正在重排页面");
        var result = document.Pages.Reorder(cancellationToken, order.ToArray());
        var fileName = $"{OfficePdf.Stem(request.Source)}-reordered.pdf";
        await OfficePdf.SaveAsync(result, request.Output, fileName, cancellationToken).ConfigureAwait(false);
        OfficePdf.Report(progress, 100, "重排完成");
        return PdfOperationResult.Single(fileName, $"已重排全部 {pageCount} 页。");
    }
}
