namespace AvaPDF.Domain;

public enum PdfInputKind
{
    Pdf,
    Images,
    Word,
    Excel,
    PowerPoint,
    Markdown,
    Html,
    Rtf,
    Text
}

/// <summary>UI-neutral metadata used to build a tool catalog on any host.</summary>
public sealed record PdfToolDescriptor(
    PdfOperationKind Kind,
    string Name,
    string Description,
    string Category,
    string Glyph,
    PdfInputKind InputKind = PdfInputKind.Pdf,
    int MinimumInputs = 1,
    int? MaximumInputs = 1);
