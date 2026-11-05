using AvaPDF.Domain;

namespace AvaPDF.Desktop.Services;

public enum ShellSelectionKind
{
    Unsupported,
    SinglePdf,
    MultiplePdfs,
    SingleDocument,
    MultipleDocuments,
    SingleImage,
    MultipleImages
}

public static class ShellFileClassifier
{
    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".webp", ".tif", ".tiff", ".svg"
    };

    public static ShellSelectionKind Classify(IReadOnlyList<string> files)
    {
        if (files.Count == 0 || files.Any(path => !IsSupported(path)))
        {
            return ShellSelectionKind.Unsupported;
        }

        var pdfCount = files.Count(path => IsPdf(path));
        var imageCount = files.Count(path => IsImage(path));
        if (pdfCount == files.Count)
        {
            return files.Count == 1 ? ShellSelectionKind.SinglePdf : ShellSelectionKind.MultiplePdfs;
        }
        if (imageCount == files.Count)
        {
            return files.Count == 1 ? ShellSelectionKind.SingleImage : ShellSelectionKind.MultipleImages;
        }
        if (pdfCount > 0)
        {
            return ShellSelectionKind.Unsupported;
        }
        return files.Count == 1 ? ShellSelectionKind.SingleDocument : ShellSelectionKind.MultipleDocuments;
    }

    public static PdfOperationKind? GetConversionTool(string path)
    {
        if (IsImage(path))
        {
            return PdfOperationKind.ImagesToPdf;
        }
        return Path.GetExtension(path).ToUpperInvariant() switch
        {
            ".DOC" or ".DOCX" or ".DOCM" or ".DOTX" or ".DOTM" => PdfOperationKind.WordToPdf,
            ".XLS" or ".XLSX" or ".XLSM" or ".XLTX" or ".XLTM" => PdfOperationKind.ExcelToPdf,
            ".PPT" or ".PPTX" or ".PPTM" => PdfOperationKind.PowerPointToPdf,
            ".MD" or ".MARKDOWN" => PdfOperationKind.MarkdownToPdf,
            ".HTML" or ".HTM" => PdfOperationKind.HtmlToPdf,
            ".RTF" => PdfOperationKind.RtfToPdf,
            ".TXT" => PdfOperationKind.TxtToPdf,
            _ => null
        };
    }

    public static bool IsSupported(string path) => IsPdf(path) || GetConversionTool(path).HasValue;

    private static bool IsPdf(string path) => string.Equals(Path.GetExtension(path), ".pdf", StringComparison.OrdinalIgnoreCase);

    private static bool IsImage(string path) => ImageExtensions.Contains(Path.GetExtension(path));
}
