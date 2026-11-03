using AvaPDF.Application.Operations;
using AvaPDF.Domain;
using OfficeIMO.Pdf;

namespace AvaPDF.Pdf.OfficeIMO.Features;

public sealed class EncryptPdfHandler : PdfOperationHandler<EncryptPdfRequest>
{
    public override PdfOperationKind Kind => PdfOperationKind.Encrypt;

    protected override async Task<PdfOperationResult> HandleAsync(
        EncryptPdfRequest request,
        IProgress<PdfOperationProgress>? progress,
        CancellationToken cancellationToken)
    {
        OfficePdf.RequirePdf(request.Source);
        if (string.IsNullOrWhiteSpace(request.UserPassword))
        {
            throw new ArgumentException("打开密码不能为空。 ");
        }

        var options = new PdfStandardEncryptionOptions(request.UserPassword)
        {
            Algorithm = PdfStandardEncryptionAlgorithm.Aes256
        };
        if (!string.IsNullOrWhiteSpace(request.OwnerPassword))
        {
            options.OwnerPassword = request.OwnerPassword;
        }

        OfficePdf.Report(progress, 15, "正在读取 PDF");
        var document = await OfficePdf.LoadAsync(request.Source, cancellationToken).ConfigureAwait(false);
        OfficePdf.Report(progress, 45, "正在使用 AES-256 加密");
        var result = document.Security.Encrypt(options, cancellationToken);
        var fileName = $"{OfficePdf.Stem(request.Source)}-encrypted.pdf";
        await OfficePdf.SaveBytesAsync(result.Pdf, request.Output, fileName, cancellationToken).ConfigureAwait(false);
        OfficePdf.Report(progress, 100, "加密完成");
        return PdfOperationResult.Single(fileName, "AES-256 加密完成，请妥善保管打开密码和所有者密码。");
    }
}

public sealed class DecryptPdfHandler : PdfOperationHandler<DecryptPdfRequest>
{
    public override PdfOperationKind Kind => PdfOperationKind.Decrypt;

    protected override async Task<PdfOperationResult> HandleAsync(
        DecryptPdfRequest request,
        IProgress<PdfOperationProgress>? progress,
        CancellationToken cancellationToken)
    {
        OfficePdf.RequirePdf(request.Source);
        if (string.IsNullOrWhiteSpace(request.OwnerPassword))
        {
            throw new ArgumentException("请输入当前所有者密码。 ");
        }

        OfficePdf.Report(progress, 15, "正在验证密码");
        await using var input = await request.Source.OpenReadAsync(cancellationToken).ConfigureAwait(false);
        var document = PdfDocument.Load(input, new PdfLoadOptions { Password = request.OwnerPassword }, cancellationToken);
        OfficePdf.Report(progress, 45, "正在移除 Standard Security 加密");
        var result = document.Security.Decrypt(request.OwnerPassword, cancellationToken);
        var fileName = $"{OfficePdf.Stem(request.Source)}-decrypted.pdf";
        await OfficePdf.SaveBytesAsync(result.Pdf, request.Output, fileName, cancellationToken).ConfigureAwait(false);
        OfficePdf.Report(progress, 100, "密码已移除");
        return PdfOperationResult.Single(fileName, "已生成无密码 PDF，源文件仍保持加密。 ");
    }
}

public sealed class UpdateMetadataPdfHandler : PdfOperationHandler<UpdateMetadataPdfRequest>
{
    public override PdfOperationKind Kind => PdfOperationKind.UpdateMetadata;

    protected override async Task<PdfOperationResult> HandleAsync(
        UpdateMetadataPdfRequest request,
        IProgress<PdfOperationProgress>? progress,
        CancellationToken cancellationToken)
    {
        OfficePdf.RequirePdf(request.Source);
        OfficePdf.Report(progress, 15, "正在读取 PDF");
        var document = await OfficePdf.LoadAsync(request.Source, cancellationToken).ConfigureAwait(false);
        OfficePdf.Report(progress, 45, "正在更新元数据");
        var result = document.UpdateMetadata(
            cancellationToken,
            title: request.Title,
            author: request.Author,
            subject: request.Subject,
            keywords: request.Keywords);
        var fileName = $"{OfficePdf.Stem(request.Source)}-metadata.pdf";
        await OfficePdf.SaveAsync(result, request.Output, fileName, cancellationToken).ConfigureAwait(false);
        OfficePdf.Report(progress, 100, "元数据已更新");
        return PdfOperationResult.Single(fileName, "元数据已更新，未填写的字段保持原值。 ");
    }
}
