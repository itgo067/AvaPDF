using AvaTools.Application.Operations;
using AvaTools.Domain;
using OfficeIMO.Pdf;

namespace AvaTools.Pdf.OfficeIMO.Features;

public sealed class ImagesToPdfHandler : PdfOperationHandler<ImagesToPdfRequest>
{
    private const long MaximumImageBytes = 128L * 1024 * 1024;
    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".webp", ".tif", ".tiff", ".svg"
    };

    public override PdfOperationKind Kind => PdfOperationKind.ImagesToPdf;

    protected override async Task<PdfOperationResult> HandleAsync(
        ImagesToPdfRequest request,
        IProgress<PdfOperationProgress>? progress,
        CancellationToken cancellationToken)
    {
        if (request.Sources.Count == 0)
        {
            throw new ArgumentException("请至少选择一张图片。 ");
        }

        var images = new List<PdfImageDocumentSource>(request.Sources.Count);
        for (var index = 0; index < request.Sources.Count; index++)
        {
            var source = request.Sources[index];
            if (!SupportedExtensions.Contains(Path.GetExtension(source.Name)))
            {
                throw new ArgumentException($"不支持图片格式：{source.Name}。 ");
            }

            OfficePdf.Report(progress, index * 55.0 / request.Sources.Count, $"正在读取 {source.Name}");
            await using var input = await source.OpenReadAsync(cancellationToken).ConfigureAwait(false);
            if (input.CanSeek && input.Length > MaximumImageBytes)
            {
                throw new InvalidDataException($"图片 {source.Name} 超过 128 MiB 限制。 ");
            }

            var buffer = new MemoryStream();
            await input.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
            if (buffer.Length > MaximumImageBytes)
            {
                throw new InvalidDataException($"图片 {source.Name} 超过 128 MiB 限制。 ");
            }

            images.Add(new PdfImageDocumentSource(buffer.ToArray(), source.Name));
        }

        OfficePdf.Report(progress, 65, "正在生成 PDF 页面");
        var document = PdfDocument.CreateFromImages(images, options: null, cancellationToken);
        const string fileName = "images.pdf";
        OfficePdf.Report(progress, 85, "正在写入 PDF");
        await OfficePdf.SaveAsync(document, request.Output, fileName, cancellationToken).ConfigureAwait(false);
        OfficePdf.Report(progress, 100, "转换完成");
        return PdfOperationResult.Single(fileName, $"已将 {images.Count} 张图片转换为 {images.Count} 页 PDF。");
    }
}
