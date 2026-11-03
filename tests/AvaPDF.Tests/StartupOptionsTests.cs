using AvaPDF.Desktop;
using AvaPDF.Domain;
using Xunit;

namespace AvaPDF.Tests;

public sealed class StartupOptionsTests
{
    [Fact]
    public void Parses_tool_and_repeated_files()
    {
        var options = StartupOptions.Parse(
            ["--tool", "optimize", "--file", "one.pdf", "--file=two.pdf"]);

        Assert.Equal(PdfOperationKind.Optimize, options.Tool);
        Assert.Equal(["one.pdf", "two.pdf"], options.Files);
        Assert.Null(options.Error);
    }

    [Theory]
    [InlineData("word", PdfOperationKind.WordToPdf)]
    [InlineData("word-to-pdf", PdfOperationKind.WordToPdf)]
    [InlineData("Update_Metadata", PdfOperationKind.UpdateMetadata)]
    [InlineData("添加水印", PdfOperationKind.Watermark)]
    public void Supports_stable_tool_aliases(string alias, PdfOperationKind expected)
    {
        var options = StartupOptions.Parse([$"--tool={alias}"]);

        Assert.Equal(expected, options.Tool);
    }

    [Fact]
    public void Rejects_unknown_or_incomplete_arguments()
    {
        Assert.NotNull(StartupOptions.Parse(["--tool", "unknown"]).Error);
        Assert.NotNull(StartupOptions.Parse(["--file"]).Error);
        Assert.NotNull(StartupOptions.Parse(["--unknown"]).Error);
    }
}
