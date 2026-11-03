using AvaPDF.Desktop.Services;
using AvaPDF.Domain;
using Xunit;

namespace AvaPDF.Tests;

public sealed class FileDialogServiceTests
{
    [Theory]
    [InlineData("document.pdf", PdfInputKind.Pdf, true)]
    [InlineData("document.doc", PdfInputKind.Word, true)]
    [InlineData("document.docx", PdfInputKind.Word, true)]
    [InlineData("image.png", PdfInputKind.Images, true)]
    [InlineData("image.png", PdfInputKind.Pdf, false)]
    [InlineData("document.txt", PdfInputKind.Word, false)]
    [InlineData("README", PdfInputKind.Markdown, false)]
    public void Drag_and_picker_share_extension_rules(string path, PdfInputKind kind, bool expected) =>
        Assert.Equal(expected, FileDialogService.IsSupportedFile(path, kind));
}
