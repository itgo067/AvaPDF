using System.Text;
using AvaPDF.Application.Abstractions;
using AvaPDF.Application.Operations;
using AvaPDF.Domain;
using AvaPDF.Pdf.OfficeIMO.Features;
using OfficeIMO.Pdf;
using Xunit;

namespace AvaPDF.Tests;

public sealed class SecurityPdfTests
{
    [Fact]
    public async Task Watermark_adds_unicode_text()
    {
        var output = new MemoryOutputTarget();
        var request = new WatermarkPdfRequest(
            Source("source.pdf", CreatePdf("Body text")),
            PdfWatermarkKind.Text,
            "内部资料",
            null,
            null,
            0.3,
            -30,
            48,
            360,
            120,
            output);

        var result = await new WatermarkPdfHandler().HandleAsync(request, null, CancellationToken.None);
        var text = Text(PdfDocument.Load(output.Files[result.OutputFiles[0]]).Read());

        Assert.Contains("内部资料", text);
        Assert.Contains("Body text", text);
    }

    [Fact]
    public async Task Watermark_adds_an_image()
    {
        var output = new MemoryOutputTarget();
        var png = Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=");
        var request = new WatermarkPdfRequest(
            Source("source.pdf", CreatePdf("Body text")),
            PdfWatermarkKind.Image,
            null,
            Source("mark.png", png),
            null,
            0.5,
            0,
            48,
            180,
            80,
            output);

        var result = await new WatermarkPdfHandler().HandleAsync(request, null, CancellationToken.None);
        var read = PdfDocument.Load(output.Files[result.OutputFiles[0]]).Read();

        Assert.True(read.PageCount == 1);
        Assert.Contains("Body text", Text(read));
    }

    [Fact]
    public async Task Encrypt_and_decrypt_round_trip_preserves_content()
    {
        var encryptedOutput = new MemoryOutputTarget();
        var encryptRequest = new EncryptPdfRequest(
            Source("source.pdf", CreatePdf("Secret body")),
            "reader-password",
            "owner-password",
            encryptedOutput);
        var encryptedResult = await new EncryptPdfHandler().HandleAsync(encryptRequest, null, CancellationToken.None);
        var encryptedBytes = encryptedOutput.Files[encryptedResult.OutputFiles[0]];

        var opened = PdfDocument.Load(encryptedBytes, new PdfLoadOptions { Password = "reader-password" }).Read();
        Assert.Contains("Secret body", Text(opened));

        var decryptedOutput = new MemoryOutputTarget();
        var decryptRequest = new DecryptPdfRequest(
            Source("encrypted.pdf", encryptedBytes),
            "owner-password",
            decryptedOutput);
        var decryptedResult = await new DecryptPdfHandler().HandleAsync(decryptRequest, null, CancellationToken.None);
        var decryptedBytes = decryptedOutput.Files[decryptedResult.OutputFiles[0]];

        var decrypted = PdfDocument.Load(decryptedBytes).Read();
        Assert.Contains("Secret body", Text(decrypted));
    }

    [Fact]
    public async Task Metadata_updates_only_supplied_fields()
    {
        var output = new MemoryOutputTarget();
        var request = new UpdateMetadataPdfRequest(
            Source("source.pdf", CreatePdf("Body")),
            "AvaPDF title",
            "Unit test",
            "Metadata",
            "pdf,test",
            output);

        var result = await new UpdateMetadataPdfHandler().HandleAsync(request, null, CancellationToken.None);
        var read = PdfDocument.Load(output.Files[result.OutputFiles[0]]).Read();

        Assert.Equal("AvaPDF title", read.Metadata.Title);
        Assert.Equal("Unit test", read.Metadata.Author);
        Assert.Equal("Metadata", read.Metadata.Subject);
        Assert.Equal("pdf,test", read.Metadata.Keywords);
    }

    private static IPdfSource Source(string name, byte[] bytes) =>
        new DelegatePdfSource(name, _ => ValueTask.FromResult<Stream>(new MemoryStream(bytes, writable: false)));

    private static byte[] CreatePdf(string text) => PdfDocument.Create(pdf =>
        pdf.Content(content => content.Paragraph(paragraph => paragraph.Text(text)))).ToBytes();

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
