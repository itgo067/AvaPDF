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
    public void Parses_auto_run()
    {
        var options = StartupOptions.Parse(["--auto", "--tool", "extracttext", "--file", "one.pdf"]);

        Assert.True(options.AutoRun);
        Assert.Equal(PdfOperationKind.ExtractText, options.Tool);
    }

    [Fact]
    public void Reads_and_deletes_owned_job_file()
    {
        Directory.CreateDirectory(StartupOptions.JobDirectory);
        var path = Path.Combine(StartupOptions.JobDirectory, $"AvaPDF-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, "{\"tool\":\"extracttext\",\"autoRun\":true,\"files\":[\"one.pdf\",\"two.pdf\"]}");

        var options = StartupOptions.Parse(["--job", path]);

        Assert.True(options.AutoRun);
        Assert.Equal(PdfOperationKind.ExtractText, options.Tool);
        Assert.Equal(["one.pdf", "two.pdf"], options.Files);
        Assert.False(File.Exists(path));
    }

    [Fact]
    public void Rejects_unknown_or_incomplete_arguments()
    {
        Assert.NotNull(StartupOptions.Parse(["--tool", "unknown"]).Error);
        Assert.NotNull(StartupOptions.Parse(["--file"]).Error);
        Assert.NotNull(StartupOptions.Parse(["--unknown"]).Error);
    }
}
