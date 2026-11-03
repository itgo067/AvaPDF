using AvaTools.Application.Operations;
using AvaTools.Domain;
using OfficeIMO.Pdf;

namespace AvaTools.Pdf.OfficeIMO.Features;

public sealed class MergePdfHandler : PdfOperationHandler<MergePdfRequest>
{
    public override PdfOperationKind Kind => PdfOperationKind.Merge;

    protected override async Task<PdfOperationResult> HandleAsync(
        MergePdfRequest request,
        IProgress<PdfOperationProgress>? progress,
        CancellationToken cancellationToken)
    {
        if (request.Sources.Count < 2)
        {
            throw new ArgumentException("合并 PDF 至少需要选择两个文件。 ");
        }

        var documents = new List<PdfDocument>(request.Sources.Count);
        for (var index = 0; index < request.Sources.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var source = request.Sources[index];
            OfficePdf.RequirePdf(source);
            OfficePdf.Report(progress, index * 55.0 / request.Sources.Count, $"正在读取 {source.Name}");
            documents.Add(await OfficePdf.LoadAsync(source, cancellationToken).ConfigureAwait(false));
        }

        OfficePdf.Report(progress, 65, "正在合并文档");
        var merged = PdfDocument.Merge(documents, cancellationToken);
        const string fileName = "merged.pdf";
        OfficePdf.Report(progress, 85, "正在写入合并结果");
        await OfficePdf.SaveAsync(merged, request.Output, fileName, cancellationToken).ConfigureAwait(false);
        OfficePdf.Report(progress, 100, "合并完成");
        return PdfOperationResult.Single(fileName, $"已按顺序合并 {documents.Count} 个 PDF。");
    }
}
