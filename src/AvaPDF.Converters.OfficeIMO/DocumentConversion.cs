using AvaPDF.Application.Operations;
using AvaPDF.Domain;
using OfficeIMO.Pdf;

namespace AvaPDF.Converters.OfficeIMO;

internal static class DocumentConversion
{
    public static async Task<PdfOperationResult> ExecuteAsync(
        DocumentToPdfRequest request,
        IProgress<PdfOperationProgress>? progress,
        CancellationToken cancellationToken,
        IReadOnlySet<string> extensions,
        Func<Stream, Stream, CancellationToken, Task<IReadOnlyList<string>>> convertAsync)
    {
        var extension = Path.GetExtension(request.Source.Name);
        if (!extensions.Contains(extension))
        {
            throw new ArgumentException($"“{request.Source.Name}”不是支持的 {string.Join(" / ", extensions)} 文件。");
        }

        Report(progress, 10, $"正在读取 {request.Source.Name}");
        var fileName = $"{Stem(request.Source.Name)}.pdf";
        await using var input = await request.Source.OpenReadAsync(cancellationToken).ConfigureAwait(false);
        await using var output = await request.Output.OpenWriteAsync(fileName, cancellationToken).ConfigureAwait(false);
        Report(progress, 35, "正在转换文档");
        var warnings = await convertAsync(input, output, cancellationToken).ConfigureAwait(false);
        await output.FlushAsync(cancellationToken).ConfigureAwait(false);
        Report(progress, 100, "转换完成");
        var message = warnings.Count == 0
            ? $"已将 {request.Source.Name} 转换为 PDF。"
            : $"已生成 PDF，但有 {warnings.Count} 条转换警告，请检查结果。";
        return PdfOperationResult.Single(fileName, message, warnings);
    }

    public static IReadOnlyList<string> Complete(PdfSaveResult result)
    {
        if (!result.Succeeded)
        {
            var diagnostics = string.Join(Environment.NewLine, result.Diagnostics);
            throw new InvalidDataException($"PDF 转换失败：{diagnostics}");
        }

        return result.Warnings
            .Select(warning => $"{warning.Source}: {warning.Message}")
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    public static string? ResolveResourceRoot(string? directory)
    {
        if (string.IsNullOrWhiteSpace(directory))
        {
            return null;
        }

        var fullPath = Path.GetFullPath(directory);
        if (!Directory.Exists(fullPath))
        {
            throw new DirectoryNotFoundException($"资源目录不存在：{fullPath}");
        }

        return fullPath;
    }

    private static string Stem(string name)
    {
        var stem = Path.GetFileNameWithoutExtension(name);
        if (string.IsNullOrWhiteSpace(stem))
        {
            return "document";
        }

        var invalid = Path.GetInvalidFileNameChars();
        return string.Concat(stem.Select(character => invalid.Contains(character) ? '_' : character));
    }

    private static void Report(IProgress<PdfOperationProgress>? progress, double percent, string message) =>
        progress?.Report(new PdfOperationProgress(percent, message));
}
