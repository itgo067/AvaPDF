using System.Text;
using AvaPDF.Application.Abstractions;
using AvaPDF.Application.Operations;
using AvaPDF.Converters.OfficeIMO;
using AvaPDF.Domain;
using AvaPDF.Pdf.OfficeIMO.Features;
using OfficeIMO.Excel;
using OfficeIMO.Pdf;
using OfficeIMO.PowerPoint;
using OfficeIMO.Word;

var mergeOutput = new MemoryOutputTarget();
await new MergePdfHandler().HandleAsync(
    new MergePdfRequest(
        [Smoke.Source("one.pdf", Smoke.CreatePdf("Alpha")), Smoke.Source("two.pdf", Smoke.CreatePdf("Beta"))],
        mergeOutput),
    null,
    CancellationToken.None);
Smoke.Require(Smoke.Read(mergeOutput.Files["merged.pdf"]).PageCount == 2, "merge");

var splitOutput = new MemoryOutputTarget();
await new SplitPdfHandler().HandleAsync(
    new SplitPdfRequest(
        Smoke.Source("source.pdf", Smoke.CreatePdf("One", "Two", "Three")),
        PdfSplitMode.EveryPage,
        1,
        null,
        splitOutput),
    null,
    CancellationToken.None);
Smoke.Require(splitOutput.Files.Count == 3, "split");
Smoke.Require(splitOutput.Files.Values.All(bytes => Smoke.Read(bytes).PageCount == 1), "split output");

var extractOutput = new MemoryOutputTarget();
await new ExtractPdfHandler().HandleAsync(
    new ExtractPdfRequest(Smoke.Source("source.pdf", Smoke.CreatePdf("One", "Two", "Three")), "3,1", extractOutput),
    null,
    CancellationToken.None);
Smoke.Require(
    Smoke.Texts(Smoke.Read(extractOutput.Files["source-extracted.pdf"])).SequenceEqual(["Three", "One"]),
    "extract");

var deleteOutput = new MemoryOutputTarget();
await new DeletePdfHandler().HandleAsync(
    new DeletePdfRequest(Smoke.Source("source.pdf", Smoke.CreatePdf("One", "Two", "Three")), "2", deleteOutput),
    null,
    CancellationToken.None);
Smoke.Require(
    Smoke.Texts(Smoke.Read(deleteOutput.Files["source-pages-deleted.pdf"])).SequenceEqual(["One", "Three"]),
    "delete");

var rotateOutput = new MemoryOutputTarget();
await new RotatePdfHandler().HandleAsync(
    new RotatePdfRequest(Smoke.Source("source.pdf", Smoke.CreatePdf("One", "Two")), "2", 90, rotateOutput),
    null,
    CancellationToken.None);
Smoke.Require(Smoke.Read(rotateOutput.Files["source-rotated.pdf"]).Pages[1].RotationDegrees == 90, "rotate");

var reorderOutput = new MemoryOutputTarget();
await new ReorderPdfHandler().HandleAsync(
    new ReorderPdfRequest(Smoke.Source("source.pdf", Smoke.CreatePdf("One", "Two", "Three")), "3,1-2", reorderOutput),
    null,
    CancellationToken.None);
Smoke.Require(
    Smoke.Texts(Smoke.Read(reorderOutput.Files["source-reordered.pdf"])).SequenceEqual(["Three", "One", "Two"]),
    "reorder");

var optimizeOutput = new MemoryOutputTarget();
await new OptimizePdfHandler().HandleAsync(
    new OptimizePdfRequest(
        Smoke.Source("source.pdf", Smoke.CreatePdf("One", "Two")),
        PdfOptimizationLevel.Balanced,
        optimizeOutput),
    null,
    CancellationToken.None);
Smoke.Require(Smoke.Read(optimizeOutput.Files["source-optimized.pdf"]).PageCount == 2, "optimize");

var imagesOutput = new MemoryOutputTarget();
var png = Convert.FromBase64String(
    "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=");
await new ImagesToPdfHandler().HandleAsync(
    new ImagesToPdfRequest([Smoke.Source("one.png", png), Smoke.Source("two.png", png)], imagesOutput),
    null,
    CancellationToken.None);
Smoke.Require(Smoke.Read(imagesOutput.Files["images.pdf"]).PageCount == 2, "images-to-pdf");

var textOutput = new MemoryOutputTarget();
await new ExtractTextPdfHandler().HandleAsync(
    new ExtractTextPdfRequest(Smoke.Source("source.pdf", Smoke.CreatePdf("One", "Two")), "2", textOutput),
    null,
    CancellationToken.None);
var text = Encoding.UTF8.GetString(textOutput.Files["source-text.txt"]);
Smoke.Require(text.Contains("Two") && !text.Contains("One"), "extract-text");

using (var word = WordDocument.Create())
{
    word.AddParagraph("Alpha Word");
    using var source = new MemoryStream();
    word.Save(source);
    var converted = await Smoke.ConvertDocumentAsync(new WordToPdfHandler(), PdfOperationKind.WordToPdf, "sample.docx", source.ToArray());
    Smoke.Require(Smoke.AllText(converted).Contains("Alpha Word"), "word-to-pdf");
}

using (var workbook = ExcelDocument.Create())
{
    var sheet = workbook.Sheets.Count > 0 ? workbook.Sheets[0] : workbook.AddWorksheet("Sheet1");
    sheet.Cell(1, 1, "Alpha Excel");
    using var source = new MemoryStream();
    workbook.Save(source);
    var converted = await Smoke.ConvertDocumentAsync(new ExcelToPdfHandler(), PdfOperationKind.ExcelToPdf, "sample.xlsx", source.ToArray());
    Smoke.Require(Smoke.AllText(converted).Contains("Alpha Excel"), "excel-to-pdf");
}

using (var presentation = PowerPointPresentation.Create())
{
    var slide = presentation.AddSlide();
    slide.AddTextBoxPoints("Alpha PowerPoint", 50, 50, 350, 60);
    using var source = new MemoryStream();
    presentation.Save(source);
    var converted = await Smoke.ConvertDocumentAsync(new PowerPointToPdfHandler(), PdfOperationKind.PowerPointToPdf, "sample.pptx", source.ToArray());
    Smoke.Require(Smoke.AllText(converted).Contains("Alpha PowerPoint"), "powerpoint-to-pdf");
}

var markdown = await Smoke.ConvertDocumentAsync(
    new MarkdownToPdfHandler(),
    PdfOperationKind.MarkdownToPdf,
    "sample.md",
    Encoding.UTF8.GetBytes("# MarkdownTitle\n\n**Alpha Markdown**"));
Smoke.Require(Smoke.AllText(markdown).Contains("Alpha Markdown"), "markdown-to-pdf");

var html = await Smoke.ConvertDocumentAsync(
    new HtmlToPdfHandler(),
    PdfOperationKind.HtmlToPdf,
    "sample.html",
    Encoding.UTF8.GetBytes("<h1>HtmlTitle</h1><p>Alpha HTML</p>"));
Smoke.Require(Smoke.AllText(html).Contains("Alpha HTML"), "html-to-pdf");

var rtf = await Smoke.ConvertDocumentAsync(
    new RtfToPdfHandler(),
    PdfOperationKind.RtfToPdf,
    "sample.rtf",
    Encoding.UTF8.GetBytes(@"{\rtf1\ansi RtfTitle\par Alpha RTF}"));
Smoke.Require(Smoke.AllText(rtf).Contains("Alpha RTF"), "rtf-to-pdf");

var txt = await Smoke.ConvertDocumentAsync(
    new TxtToPdfHandler(),
    PdfOperationKind.TxtToPdf,
    "sample.txt",
    Encoding.UTF8.GetBytes("你好，AvaPDF。\nAlpha TXT"));
Smoke.Require(Smoke.AllText(txt).Contains("Alpha TXT"), "txt-to-pdf");

using (var legacyWord = WordDocument.Create())
{
    legacyWord.AddParagraph("Legacy Word");
    using var source = new MemoryStream();
    legacyWord.Save(source, WordFileFormat.Doc);
    var converted = await Smoke.ConvertDocumentAsync(new WordToPdfHandler(), PdfOperationKind.WordToPdf, "legacy.doc", source.ToArray());
    Smoke.Require(Smoke.AllText(converted).Contains("Legacy Word"), "legacy-doc-to-pdf");
}

using (var legacyWorkbook = ExcelDocument.Create())
{
    var sheet = legacyWorkbook.Sheets.Count > 0 ? legacyWorkbook.Sheets[0] : legacyWorkbook.AddWorksheet("Sheet1");
    sheet.Cell(1, 1, "Legacy Excel");
    using var source = new MemoryStream();
    legacyWorkbook.Save(source, ExcelFileFormat.Xls);
    var converted = await Smoke.ConvertDocumentAsync(new ExcelToPdfHandler(), PdfOperationKind.ExcelToPdf, "legacy.xls", source.ToArray());
    Smoke.Require(Smoke.AllText(converted).Contains("Legacy Excel"), "legacy-xls-to-pdf");
}

using (var legacyPresentation = PowerPointPresentation.Create())
{
    var slide = legacyPresentation.AddSlide();
    slide.AddTextBoxPoints("Legacy PowerPoint", 50, 50, 350, 60);
    using var source = new MemoryStream();
    legacyPresentation.Save(source, PowerPointFileFormat.Ppt);
    var converted = await Smoke.ConvertDocumentAsync(new PowerPointToPdfHandler(), PdfOperationKind.PowerPointToPdf, "legacy.ppt", source.ToArray());
    Smoke.Require(Smoke.AllText(converted).Contains("Legacy PowerPoint"), "legacy-ppt-to-pdf");
}

var watermarkOutput = new MemoryOutputTarget();
await new WatermarkPdfHandler().HandleAsync(
    new WatermarkPdfRequest(
        Smoke.Source("source.pdf", Smoke.CreatePdf("Body")),
        PdfWatermarkKind.Text,
        "内部资料",
        null,
        null,
        0.3,
        -30,
        48,
        360,
        120,
        watermarkOutput),
    null,
    CancellationToken.None);
Smoke.Require(Smoke.AllText(Smoke.Read(watermarkOutput.Files["source-watermarked.pdf"])).Contains("内部资料"), "watermark");

var encryptOutput = new MemoryOutputTarget();
await new EncryptPdfHandler().HandleAsync(
    new EncryptPdfRequest(Smoke.Source("source.pdf", Smoke.CreatePdf("Secret")), "reader", "owner", encryptOutput),
    null,
    CancellationToken.None);
var encryptedBytes = encryptOutput.Files["source-encrypted.pdf"];
Smoke.Require(PdfDocument.Load(encryptedBytes, new PdfLoadOptions { Password = "reader" }).Read().PageCount == 1, "encrypt");

var decryptOutput = new MemoryOutputTarget();
await new DecryptPdfHandler().HandleAsync(
    new DecryptPdfRequest(Smoke.Source("encrypted.pdf", encryptedBytes), "owner", decryptOutput),
    null,
    CancellationToken.None);
Smoke.Require(Smoke.Read(decryptOutput.Files["encrypted-decrypted.pdf"]).PageCount == 1, "decrypt");

var metadataOutput = new MemoryOutputTarget();
await new UpdateMetadataPdfHandler().HandleAsync(
    new UpdateMetadataPdfRequest(
        Smoke.Source("source.pdf", Smoke.CreatePdf("Body")),
        "AvaPDF title",
        "AOT smoke",
        "Metadata",
        "pdf,aot",
        metadataOutput),
    null,
    CancellationToken.None);
Smoke.Require(Smoke.Read(metadataOutput.Files["source-metadata.pdf"]).Metadata.Title == "AvaPDF title", "metadata");

Console.WriteLine("AOT_OFFICEIMO_SMOKE_OK merge split extract delete rotate reorder optimize images-to-pdf extract-text word excel powerpoint markdown html rtf txt legacy-doc legacy-xls legacy-ppt watermark encrypt decrypt metadata");

internal static class Smoke
{
    public static IPdfSource Source(string name, byte[] bytes) =>
        new DelegatePdfSource(name, _ => ValueTask.FromResult<Stream>(new MemoryStream(bytes, writable: false)));

    public static byte[] CreatePdf(params string[] pageTexts)
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

    public static PdfDocumentReadResult Read(byte[] bytes) => PdfDocument.Load(bytes).Read();

    public static string[] Texts(PdfDocumentReadResult document) =>
        document.Pages.Select(page => page.TextBlocks.Single().Text).ToArray();

    public static string AllText(PdfDocumentReadResult document) => string.Join(
        '\n',
        document.Pages.SelectMany(page => page.TextBlocks).Select(block => block.Text));

    public static async Task<PdfDocumentReadResult> ConvertDocumentAsync(
        IPdfOperationHandler handler,
        PdfOperationKind kind,
        string sourceName,
        byte[] sourceBytes)
    {
        var output = new MemoryOutputTarget();
        var request = new DocumentToPdfRequest(kind, Source(sourceName, sourceBytes), null, output);
        var result = await handler.HandleAsync(request, null, CancellationToken.None);
        return Read(output.Files[result.OutputFiles[0]]);
    }

    public static void Require(bool condition, string operation)
    {
        if (!condition)
        {
            throw new InvalidOperationException($"{operation} failed under Native AOT.");
        }
    }
}

internal sealed class MemoryOutputTarget : IPdfOutputTarget
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
