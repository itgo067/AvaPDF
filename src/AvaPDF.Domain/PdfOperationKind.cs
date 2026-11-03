namespace AvaPDF.Domain;

/// <summary>Stable operation identifiers shared by desktop, web and persistence layers.</summary>
public enum PdfOperationKind
{
    Merge,
    Split,
    Extract,
    Delete,
    Rotate,
    Reorder,
    Optimize,
    ImagesToPdf,
    ExtractText,
    WordToPdf,
    ExcelToPdf,
    PowerPointToPdf,
    MarkdownToPdf,
    HtmlToPdf,
    RtfToPdf,
    TxtToPdf,
    Watermark,
    Encrypt,
    UpdateMetadata,
    Decrypt
}
