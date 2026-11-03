using System.Text;
using AvaTools.Application.Abstractions;
using AvaTools.Application.Operations;
using AvaTools.Pdf.OfficeIMO.Features;
using OfficeIMO.Pdf;
using Xunit;

namespace AvaTools.Tests;

public sealed class PdfHandlerTests
{
    [Fact]
    public async Task Merge_combines_documents_in_input_order()
    {
        var output = new MemoryOutputTarget();
        var handler = new MergePdfHandler();
        var request = new MergePdfRequest(
            [Source("first.pdf", CreatePdf("Alpha")), Source("second.pdf", CreatePdf("Beta"))],
            output);

        var result = await handler.HandleAsync(request, null, CancellationToken.None);

        var merged = Read(output.Files[result.OutputFiles[0]]);
        Assert.Equal(2, merged.PageCount);
        Assert.Contains("Alpha", merged.Pages[0].TextBlocks.Select(block => block.Text).Single());
        Assert.Contains("Beta", merged.Pages[1].TextBlocks.Select(block => block.Text).Single());
    }

    [Fact]
    public async Task Split_creates_one_pdf_per_page()
    {
        var output = new MemoryOutputTarget();
        var request = new SplitPdfRequest(
            Source("source.pdf", CreatePdf("One", "Two", "Three")),
            PdfSplitMode.EveryPage,
            1,
            null,
            output);

        var result = await new SplitPdfHandler().HandleAsync(request, null, CancellationToken.None);

        Assert.Equal(3, result.OutputFiles.Count);
        Assert.All(result.OutputFiles, file => Assert.Equal(1, Read(output.Files[file]).PageCount));
    }

    [Fact]
    public async Task Extract_returns_only_selected_pages()
    {
        var output = new MemoryOutputTarget();
        var request = new ExtractPdfRequest(
            Source("source.pdf", CreatePdf("One", "Two", "Three")),
            "3,1",
            output);

        var result = await new ExtractPdfHandler().HandleAsync(request, null, CancellationToken.None);

        var extracted = Read(output.Files[result.OutputFiles[0]]);
        Assert.Equal(2, extracted.PageCount);
        Assert.Equal("Three", extracted.Pages[0].TextBlocks.Single().Text);
        Assert.Equal("One", extracted.Pages[1].TextBlocks.Single().Text);
    }

    [Fact]
    public async Task Delete_rejects_deleting_every_page()
    {
        var request = new DeletePdfRequest(
            Source("source.pdf", CreatePdf("One", "Two")),
            "1-2",
            new MemoryOutputTarget());

        await Assert.ThrowsAsync<ArgumentException>(() =>
            new DeletePdfHandler().HandleAsync(request, null, CancellationToken.None));
    }

    [Fact]
    public async Task Reorder_applies_the_requested_page_order()
    {
        var output = new MemoryOutputTarget();
        var request = new ReorderPdfRequest(
            Source("source.pdf", CreatePdf("One", "Two", "Three")),
            "3,1-2",
            output);

        var result = await new ReorderPdfHandler().HandleAsync(request, null, CancellationToken.None);

        var reordered = Read(output.Files[result.OutputFiles[0]]);
        Assert.Equal(
            ["Three", "One", "Two"],
            reordered.Pages.Select(page => page.TextBlocks.Single().Text).ToArray());
    }

    [Fact]
    public async Task ExtractText_writes_utf8_text_file()
    {
        var output = new MemoryOutputTarget();
        var request = new ExtractTextPdfRequest(
            Source("source.pdf", CreatePdf("Alpha", "Beta")),
            "2",
            output);

        var result = await new ExtractTextPdfHandler().HandleAsync(request, null, CancellationToken.None);

        var text = Encoding.UTF8.GetString(output.Files[result.OutputFiles[0]]);
        Assert.Contains("第 2 页", text);
        Assert.Contains("Beta", text);
        Assert.DoesNotContain("Alpha", text);
    }

    [Fact]
    public async Task Delete_keeps_unselected_pages()
    {
        var output = new MemoryOutputTarget();
        var request = new DeletePdfRequest(
            Source("source.pdf", CreatePdf("One", "Two", "Three")),
            "2",
            output);

        var result = await new DeletePdfHandler().HandleAsync(request, null, CancellationToken.None);

        var deleted = Read(output.Files[result.OutputFiles[0]]);
        Assert.Equal(["One", "Three"], deleted.Pages.Select(page => page.TextBlocks.Single().Text).ToArray());
    }

    [Fact]
    public async Task Rotate_changes_selected_page_rotation()
    {
        var output = new MemoryOutputTarget();
        var request = new RotatePdfRequest(
            Source("source.pdf", CreatePdf("One", "Two")),
            "2",
            90,
            output);

        var result = await new RotatePdfHandler().HandleAsync(request, null, CancellationToken.None);

        var rotated = Read(output.Files[result.OutputFiles[0]]);
        Assert.Equal(0, rotated.Pages[0].RotationDegrees);
        Assert.Equal(90, rotated.Pages[1].RotationDegrees);
    }

    [Fact]
    public async Task Optimize_returns_a_readable_pdf()
    {
        var output = new MemoryOutputTarget();
        var request = new OptimizePdfRequest(
            Source("source.pdf", CreatePdf("One", "Two")),
            PdfOptimizationLevel.Balanced,
            output);

        var result = await new OptimizePdfHandler().HandleAsync(request, null, CancellationToken.None);

        Assert.Equal(2, Read(output.Files[result.OutputFiles[0]]).PageCount);
    }

    [Fact]
    public async Task ImagesToPdf_creates_one_page_per_image()
    {
        var output = new MemoryOutputTarget();
        var png = Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=");
        var request = new ImagesToPdfRequest(
            [Source("one.png", png), Source("two.png", png)],
            output);

        var result = await new ImagesToPdfHandler().HandleAsync(request, null, CancellationToken.None);

        Assert.Equal(2, Read(output.Files[result.OutputFiles[0]]).PageCount);
    }

    private static IPdfSource Source(string name, byte[] bytes) =>
        new DelegatePdfSource(name, _ => ValueTask.FromResult<Stream>(new MemoryStream(bytes, writable: false)));

    private static byte[] CreatePdf(params string[] pageTexts)
    {
        var document = PdfDocument.Create(pdf =>
        {
            foreach (var text in pageTexts)
            {
                pdf.Page(page => page.Content(content => content.Paragraph(paragraph => paragraph.Text(text))));
            }
        });
        return document.ToBytes();
    }

    private static PdfDocumentReadResult Read(byte[] bytes) => PdfDocument.Load(bytes).Read();

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
