using AvaPDF.Application.Common;
using OfficeIMO.Drawing;
using AvaPDF.Application.Operations;
using AvaPDF.Domain;
using OfficeIMO.Pdf;

namespace AvaPDF.Pdf.OfficeIMO.Features;

public sealed class WatermarkPdfHandler : PdfOperationHandler<WatermarkPdfRequest>
{
    private const long MaximumImageBytes = 128L * 1024 * 1024;

    public override PdfOperationKind Kind => PdfOperationKind.Watermark;

    protected override async Task<PdfOperationResult> HandleAsync(
        WatermarkPdfRequest request,
        IProgress<PdfOperationProgress>? progress,
        CancellationToken cancellationToken)
    {
        OfficePdf.RequirePdf(request.Source);
        if (request.Opacity is <= 0 or > 1)
        {
            throw new ArgumentException("水印透明度必须大于 0 且不超过 1。 ");
        }

        if (request.FontSize is <= 0 or > 512)
        {
            throw new ArgumentException("水印文字大小必须在 0 到 512 点之间。 ");
        }

        byte[]? imageBytes = null;
        if (request.WatermarkKind == PdfWatermarkKind.Text)
        {
            if (string.IsNullOrWhiteSpace(request.Text))
            {
                throw new ArgumentException("请输入水印文字。 ");
            }
        }
        else
        {
            if (request.ImageSource is null)
            {
                throw new ArgumentException("请选择水印图片。 ");
            }

            await using var imageInput = await request.ImageSource.OpenReadAsync(cancellationToken).ConfigureAwait(false);
            if (imageInput.CanSeek && imageInput.Length > MaximumImageBytes)
            {
                throw new InvalidDataException("水印图片超过 128 MiB 限制。 ");
            }

            var buffer = new MemoryStream();
            await imageInput.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
            if (buffer.Length > MaximumImageBytes)
            {
                throw new InvalidDataException("水印图片超过 128 MiB 限制。 ");
            }

            imageBytes = buffer.ToArray();
        }

        OfficePdf.Report(progress, 15, "正在读取 PDF");
        var document = await OfficePdf.LoadAsync(request.Source, cancellationToken).ConfigureAwait(false);
        var pageCount = OfficePdf.GetPageCount(document, cancellationToken);
        var selectedPages = PageRangeExpression.Resolve(request.PageRanges, pageCount, allowEmpty: true, distinct: true);
        OfficePdf.Report(progress, 45, "正在添加水印");
        PdfDocument result;
        if (request.WatermarkKind == PdfWatermarkKind.Text)
        {
            var renderingOptions = new PdfOptions().UseTextFallbacks(PdfTextFallbackFeatures.Default);
            renderingOptions.TryUseDefaultDocumentFontFallback(true);
            renderingOptions.TryRegisterDefaultDocumentMonospaceFontFallback(true);
            var width = request.Width ?? 360;
            var height = request.Height ?? 120;
            var options = new PdfCanvasStampOptions
            {
                TargetPages = string.IsNullOrWhiteSpace(request.PageRanges) ? null : PdfPageSelector.Parse(request.PageRanges),
                Opacity = request.Opacity,
                BehindContent = true,
                RenderingOptions = renderingOptions,
                CancellationToken = cancellationToken
            };
            result = document.Stamp.Content((canvas, page) =>
            {
                var centerX = page.Width / 2;
                var centerY = page.Height / 2;
                canvas.Effect(
                    OfficeTransform.RotateDegrees(request.RotationDegrees, centerX, centerY),
                    1,
                    rotated => rotated.Text(
                        request.Text!,
                        centerX - width / 2,
                        centerY - height / 2,
                        width,
                        height,
                        request.FontSize,
                        PdfColor.FromRgb(120, 120, 120),
                        PdfAlign.Center));
            }, options, null);
        }
        else
        {
            var options = new PdfWatermarkOptions
            {
                ImageBytes = imageBytes!,
                TargetPages = string.IsNullOrWhiteSpace(request.PageRanges) ? null : PdfPageSelector.Parse(request.PageRanges),
                Opacity = request.Opacity,
                RotationDegrees = request.RotationDegrees,
                Width = request.Width ?? 180,
                Height = request.Height ?? 80,
                BehindContent = true,
                CancellationToken = cancellationToken
            };
            result = document.Stamp.Watermark(options, null);
        }
        var fileName = $"{OfficePdf.Stem(request.Source)}-watermarked.pdf";
        await OfficePdf.SaveAsync(result, request.Output, fileName, cancellationToken).ConfigureAwait(false);
        OfficePdf.Report(progress, 100, "水印添加完成");
        return PdfOperationResult.Single(fileName, "水印已添加，源文件保持不变。");
    }
}
