using System.Text;
using AvaPDF.Application.Abstractions;
using AvaPDF.Application.Operations;
using AvaPDF.Converters.OfficeIMO;
using AvaPDF.Domain;
using OfficeIMO.Excel;
using OfficeIMO.Pdf;
using OfficeIMO.PowerPoint;
using OfficeIMO.Word;
using Xunit;

namespace AvaPDF.Tests;

public sealed class DocumentConversionTests
{
    [Fact]
    public async Task Word_converts_a_real_docx_stream()
    {
        using var document = WordDocument.Create();
        document.AddParagraph("Alpha Word");
        using var source = new MemoryStream();
        document.Save(source);

        var read = await ConvertAsync(new WordToPdfHandler(), PdfOperationKind.WordToPdf, "sample.docx", source.ToArray());

        Assert.Contains("Alpha Word", Text(read));
    }

    [Fact]
    public async Task Excel_converts_a_real_xlsx_stream()
    {
        using var workbook = ExcelDocument.Create();
        var sheet = workbook.Sheets.Count > 0 ? workbook.Sheets[0] : workbook.AddWorksheet("Sheet1");
        sheet.Cell(1, 1, "Alpha Excel");
        using var source = new MemoryStream();
        workbook.Save(source);

        var read = await ConvertAsync(new ExcelToPdfHandler(), PdfOperationKind.ExcelToPdf, "sample.xlsx", source.ToArray());

        Assert.Contains("Alpha Excel", Text(read));
    }

    [Fact]
    public async Task PowerPoint_converts_a_real_pptx_stream()
    {
        using var presentation = PowerPointPresentation.Create();
        var slide = presentation.AddSlide();
        slide.AddTextBoxPoints("Alpha PowerPoint", 50, 50, 350, 60);
        using var source = new MemoryStream();
        presentation.Save(source);

        var read = await ConvertAsync(new PowerPointToPdfHandler(), PdfOperationKind.PowerPointToPdf, "sample.pptx", source.ToArray());

        Assert.Contains("Alpha PowerPoint", Text(read));
    }

    [Fact]
    public async Task Markdown_converts_headings_tables_and_code()
    {
        var markdown = """
            # MarkdownTitle

            **Alpha Markdown**

            | Name | Value |
            | --- | --- |
            | PDF | 42 |

            ```text
            Code sample
            ```
            """;

        var read = await ConvertAsync(
            new MarkdownToPdfHandler(),
            PdfOperationKind.MarkdownToPdf,
            "sample.md",
            Utf8(markdown));

        var text = Text(read);
        Assert.Contains("MarkdownTitle", text);
        Assert.Contains("Alpha Markdown", text);
        Assert.Contains("Code sample", text);
    }

    [Fact]
    public async Task Html_converts_static_content()
    {
        const string html = "<!doctype html><html><body><h1>HtmlTitle</h1><p>Alpha HTML</p></body></html>";

        var read = await ConvertAsync(new HtmlToPdfHandler(), PdfOperationKind.HtmlToPdf, "sample.html", Utf8(html));

        var text = Text(read);
        Assert.Contains("HtmlTitle", text);
        Assert.Contains("Alpha HTML", text);
    }

    [Fact]
    public async Task Rtf_converts_rich_text()
    {
        const string rtf = @"{\rtf1\ansi RtfTitle\par Alpha RTF}";

        var read = await ConvertAsync(new RtfToPdfHandler(), PdfOperationKind.RtfToPdf, "sample.rtf", Utf8(rtf));

        var text = Text(read);
        Assert.Contains("RtfTitle", text);
        Assert.Contains("Alpha RTF", text);
    }

    [Fact]
    public async Task Txt_converts_unicode_text()
    {
        var read = await ConvertAsync(
            new TxtToPdfHandler(),
            PdfOperationKind.TxtToPdf,
            "sample.txt",
            Utf8("你好，AvaPDF。\n第二行 Alpha TXT"));

        var text = Text(read);
        Assert.Contains("你好", text);
        Assert.Contains("Alpha TXT", text);
    }

    private static async Task<PdfDocumentReadResult> ConvertAsync(
        IPdfOperationHandler handler,
        PdfOperationKind kind,
        string sourceName,
        byte[] sourceBytes)
    {
        var output = new MemoryOutputTarget();
        var source = new DelegatePdfSource(
            sourceName,
            _ => ValueTask.FromResult<Stream>(new MemoryStream(sourceBytes, writable: false)));
        var request = new DocumentToPdfRequest(kind, source, ResourceBaseDirectory: null, output);
        var result = await handler.HandleAsync(request, null, CancellationToken.None);
        Assert.True(result.OutputFiles.Count == 1);
        return PdfDocument.Load(output.Files[result.OutputFiles[0]]).Read();
    }

    private static byte[] Utf8(string value) => Encoding.UTF8.GetBytes(value);

    private static string Text(PdfDocumentReadResult document) => string.Join(
        '\n',
        document.Pages.SelectMany(page => page.TextBlocks).Select(block => block.Text));

    private sealed class MemoryOutputTarget : IPdfOutputTarget
    {
        private readonly Dictionary<string, MemoryStream> _streams = new(StringComparer.OrdinalIgnoreCase);

        public IReadOnlyDictionary<string, byte[]> Files => _streams.ToDictionary(
            pair => pair.Key,
            pair => pair.Value.ToArray(),
            StringComparer.OrdinalIgnoreCase);

        public ValueTask<Stream> OpenWriteAsync(string fileName, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var stream = new MemoryStream();
            _streams[fileName] = stream;
            return ValueTask.FromResult<Stream>(stream);
        }
    }
}
