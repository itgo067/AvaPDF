using AvaPDF.Desktop.Services;
using AvaPDF.Domain;
using Xunit;

namespace AvaPDF.Tests;

public sealed class ShellFileClassifierTests
{
    [Theory]
    [InlineData("one.pdf", ShellSelectionKind.SinglePdf)]
    [InlineData("one.docx", ShellSelectionKind.SingleDocument)]
    [InlineData("one.png", ShellSelectionKind.SingleImage)]
    public void Classifies_single_supported_files(string path, ShellSelectionKind expected)
    {
        Assert.Equal(expected, ShellFileClassifier.Classify([path]));
    }

    [Fact]
    public void Classifies_multi_file_scenarios()
    {
        Assert.Equal(ShellSelectionKind.MultiplePdfs, ShellFileClassifier.Classify(["one.pdf", "two.pdf"]));
        Assert.Equal(ShellSelectionKind.MultipleImages, ShellFileClassifier.Classify(["01.png", "02.jpg"]));
        Assert.Equal(ShellSelectionKind.MultipleDocuments, ShellFileClassifier.Classify(["one.docx", "two.txt"]));
        Assert.Equal(ShellSelectionKind.MultipleDocuments, ShellFileClassifier.Classify(["one.docx", "one.png"]));
    }

    [Fact]
    public void Rejects_mixed_pdf_or_unsupported_selections()
    {
        Assert.Equal(ShellSelectionKind.Unsupported, ShellFileClassifier.Classify(["one.pdf", "one.png"]));
        Assert.Equal(ShellSelectionKind.Unsupported, ShellFileClassifier.Classify(["one.docx", "one.exe"]));
        Assert.Equal(ShellSelectionKind.Unsupported, ShellFileClassifier.Classify([]));
    }

    [Theory]
    [InlineData("one.doc", PdfOperationKind.WordToPdf)]
    [InlineData("one.xlsx", PdfOperationKind.ExcelToPdf)]
    [InlineData("one.pptx", PdfOperationKind.PowerPointToPdf)]
    [InlineData("one.md", PdfOperationKind.MarkdownToPdf)]
    [InlineData("one.html", PdfOperationKind.HtmlToPdf)]
    [InlineData("one.rtf", PdfOperationKind.RtfToPdf)]
    [InlineData("one.txt", PdfOperationKind.TxtToPdf)]
    [InlineData("one.webp", PdfOperationKind.ImagesToPdf)]
    public void Maps_existing_converters(string path, PdfOperationKind expected)
    {
        Assert.Equal(expected, ShellFileClassifier.GetConversionTool(path));
    }
}
