using System.Text;
using AvaPDF.Application.Common;
using AvaPDF.Application.Operations;
using AvaPDF.Domain;

namespace AvaPDF.Pdf.OfficeIMO.Features;

public sealed class ExtractTextPdfHandler : PdfOperationHandler<ExtractTextPdfRequest>
{
    public override PdfOperationKind Kind => PdfOperationKind.ExtractText;

    protected override async Task<PdfOperationResult> HandleAsync(
        ExtractTextPdfRequest request,
        IProgress<PdfOperationProgress>? progress,
        CancellationToken cancellationToken)
    {
        OfficePdf.RequirePdf(request.Source);
        OfficePdf.Report(progress, 10, "正在读取文本");
        var document = await OfficePdf.LoadAsync(request.Source, cancellationToken).ConfigureAwait(false);
        var pageCount = OfficePdf.GetPageCount(document, cancellationToken);
        var selectedPages = PageRangeExpression.Resolve(request.PageRanges, pageCount, allowEmpty: true, distinct: true)
            .ToHashSet();
        var read = document.Read(cancellationToken: cancellationToken);
        var text = new StringBuilder();

        foreach (var page in read.Pages.Where(page => selectedPages.Contains(page.PageNumber)))
        {
            cancellationToken.ThrowIfCancellationRequested();
            text.AppendLine($"===== 第 {page.PageNumber} 页 =====");
            foreach (var block in page.TextBlocks)
            {
                text.AppendLine(block.Text);
            }

            text.AppendLine();
        }

        var fileName = $"{OfficePdf.Stem(request.Source)}-text.txt";
        OfficePdf.Report(progress, 75, "正在写入 TXT");
        await using var output = await request.Output.OpenWriteAsync(fileName, cancellationToken).ConfigureAwait(false);
        await output.WriteAsync(new UTF8Encoding(encoderShouldEmitUTF8Identifier: false).GetBytes(text.ToString()), cancellationToken)
            .ConfigureAwait(false);
        await output.FlushAsync(cancellationToken).ConfigureAwait(false);
        OfficePdf.Report(progress, 100, "文本提取完成");
        return PdfOperationResult.Single(fileName, "原生文本已提取。扫描件需要 OCR 功能，当前版本不会识别图片中的文字。");
    }
}
