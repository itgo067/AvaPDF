namespace AvaTools.Domain;

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
    ExtractText
}
