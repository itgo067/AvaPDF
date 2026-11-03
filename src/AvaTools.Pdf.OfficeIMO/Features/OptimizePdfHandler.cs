using AvaTools.Application.Operations;
using AvaTools.Domain;
using OfficeIMO.Pdf;

namespace AvaTools.Pdf.OfficeIMO.Features;

public sealed class OptimizePdfHandler : PdfOperationHandler<OptimizePdfRequest>
{
    public override PdfOperationKind Kind => PdfOperationKind.Optimize;

    protected override async Task<PdfOperationResult> HandleAsync(
        OptimizePdfRequest request,
        IProgress<PdfOperationProgress>? progress,
        CancellationToken cancellationToken)
    {
        OfficePdf.RequirePdf(request.Source);
        OfficePdf.Report(progress, 10, "正在分析 PDF");
        var document = await OfficePdf.LoadAsync(request.Source, cancellationToken).ConfigureAwait(false);
        var profile = request.Level switch
        {
            PdfOptimizationLevel.Balanced => PdfOptimizationProfile.Balanced,
            PdfOptimizationLevel.Maximum => PdfOptimizationProfile.MaximumCompression,
            PdfOptimizationLevel.Web => PdfOptimizationProfile.Web,
            PdfOptimizationLevel.Archival => PdfOptimizationProfile.Archival,
            _ => throw new ArgumentOutOfRangeException(nameof(request), "不支持的优化配置。 ")
        };

        OfficePdf.Report(progress, 35, "正在执行无损优化");
        var optimized = document.Optimization.Apply(profile);
        var fileName = $"{OfficePdf.Stem(request.Source)}-optimized.pdf";
        OfficePdf.Report(progress, 75, "正在写入优化结果");
        await using var output = await request.Output.OpenWriteAsync(fileName, cancellationToken).ConfigureAwait(false);
        await output.WriteAsync(optimized.Bytes, cancellationToken).ConfigureAwait(false);
        await output.FlushAsync(cancellationToken).ConfigureAwait(false);
        OfficePdf.Report(progress, 100, "优化完成");
        var savedKib = optimized.SavedBytes / 1024.0;
        var message = optimized.Applied
            ? $"无损优化完成，文件减少 {savedKib:F1} KiB。"
            : "未发现更小的无损编码结果，已保留原始 PDF 内容。";
        return PdfOperationResult.Single(fileName, message);
    }
}
