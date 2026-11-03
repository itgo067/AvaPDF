using AvaPDF.Application.Abstractions;
using AvaPDF.Domain;

namespace AvaPDF.Application.Operations;

public abstract record PdfOperationRequest(PdfOperationKind Kind, IPdfOutputTarget Output);

public abstract record SinglePdfRequest(
    PdfOperationKind Kind,
    IPdfSource Source,
    IPdfOutputTarget Output) : PdfOperationRequest(Kind, Output);

public sealed record MergePdfRequest(
    IReadOnlyList<IPdfSource> Sources,
    IPdfOutputTarget Output) : PdfOperationRequest(PdfOperationKind.Merge, Output);

public enum PdfSplitMode
{
    EveryPage,
    PageCount,
    CustomRanges
}

public sealed record SplitPdfRequest(
    IPdfSource Source,
    PdfSplitMode Mode,
    int PagesPerPart,
    string? PageRanges,
    IPdfOutputTarget Output) : SinglePdfRequest(PdfOperationKind.Split, Source, Output);

public sealed record ExtractPdfRequest(
    IPdfSource Source,
    string PageRanges,
    IPdfOutputTarget Output) : SinglePdfRequest(PdfOperationKind.Extract, Source, Output);

public sealed record DeletePdfRequest(
    IPdfSource Source,
    string PageRanges,
    IPdfOutputTarget Output) : SinglePdfRequest(PdfOperationKind.Delete, Source, Output);

public sealed record RotatePdfRequest(
    IPdfSource Source,
    string? PageRanges,
    int Angle,
    IPdfOutputTarget Output) : SinglePdfRequest(PdfOperationKind.Rotate, Source, Output);

public sealed record ReorderPdfRequest(
    IPdfSource Source,
    string PageOrder,
    IPdfOutputTarget Output) : SinglePdfRequest(PdfOperationKind.Reorder, Source, Output);

public enum PdfOptimizationLevel
{
    Balanced,
    Maximum,
    Web,
    Archival
}

public sealed record OptimizePdfRequest(
    IPdfSource Source,
    PdfOptimizationLevel Level,
    IPdfOutputTarget Output) : SinglePdfRequest(PdfOperationKind.Optimize, Source, Output);

public sealed record ImagesToPdfRequest(
    IReadOnlyList<IPdfSource> Sources,
    IPdfOutputTarget Output) : PdfOperationRequest(PdfOperationKind.ImagesToPdf, Output);

public sealed record ExtractTextPdfRequest(
    IPdfSource Source,
    string? PageRanges,
    IPdfOutputTarget Output) : SinglePdfRequest(PdfOperationKind.ExtractText, Source, Output);

/// <summary>Converts one source document. ResourceBaseDirectory is an optional host-approved resource root.</summary>
public sealed record DocumentToPdfRequest(
    PdfOperationKind Kind,
    IPdfSource Source,
    string? ResourceBaseDirectory,
    IPdfOutputTarget Output) : SinglePdfRequest(Kind, Source, Output);
