using AvaPDF.Application.Operations;
using AvaPDF.Converters.OfficeIMO;
using AvaPDF.Desktop;
using AvaPDF.Desktop.Services;
using AvaPDF.Desktop.ViewModels;
using AvaPDF.Domain;
using AvaPDF.Pdf.OfficeIMO;
using Microsoft.Extensions.DependencyInjection;
using OfficeIMO.Pdf;
using Xunit;

namespace AvaPDF.Tests;

public sealed class ShellQuickActionTests
{
    [Fact]
    public async Task Extract_text_quick_action_creates_txt_beside_pdf()
    {
        var directory = Directory.CreateTempSubdirectory();
        try
        {
            var path = Path.Combine(directory.FullName, "sample 中文 file.pdf");
            File.WriteAllBytes(path, CreatePdf("Alpha quick action"));
            var viewModel = CreateViewModel();

            await viewModel.ApplyStartupOptions(new StartupOptions(
                [path], PdfOperationKind.ExtractText, AutoRun: true));

            var output = Assert.Single(viewModel.Workspace.OutputFiles);
            Assert.Equal(directory.FullName, Path.GetDirectoryName(output));
            Assert.Equal(".txt", Path.GetExtension(output));
            Assert.Contains("Alpha quick action", await File.ReadAllTextAsync(output));
            Assert.Equal(string.Empty, viewModel.Workspace.ErrorMessage);
        }
        finally
        {
            Directory.Delete(directory.FullName, true);
        }
    }

    [Fact]
    public async Task Batch_conversion_creates_one_pdf_per_text_file()
    {
        var directory = Directory.CreateTempSubdirectory();
        try
        {
            var first = Path.Combine(directory.FullName, "first.txt");
            var second = Path.Combine(directory.FullName, "second.txt");
            await File.WriteAllTextAsync(first, "Alpha first");
            await File.WriteAllTextAsync(second, "Beta second");
            var viewModel = CreateViewModel();

            await viewModel.ApplyStartupOptions(new StartupOptions(
                [first, second], null, AutoRun: true));

            Assert.Equal(2, viewModel.Workspace.OutputFiles.Count);
            Assert.Equal(string.Empty, viewModel.Workspace.ErrorMessage);
            var firstPdf = PdfDocument.Load(await File.ReadAllBytesAsync(viewModel.Workspace.OutputFiles[0])).Read();
            var secondPdf = PdfDocument.Load(await File.ReadAllBytesAsync(viewModel.Workspace.OutputFiles[1])).Read();
            Assert.Contains("Alpha first", firstPdf.Pages[0].TextBlocks.Select(block => block.Text).Single());
            Assert.Contains("Beta second", secondPdf.Pages[0].TextBlocks.Select(block => block.Text).Single());
        }
        finally
        {
            Directory.Delete(directory.FullName, true);
        }
    }

    private static MainWindowViewModel CreateViewModel()
    {
        var services = new ServiceCollection();
        services.AddOfficeImoPdf();
        services.AddOfficeImoDocumentConverters();
        using var provider = services.BuildServiceProvider();
        return new MainWindowViewModel(
            provider.GetRequiredService<PdfJobService>(),
            new FakeFileDialogService(),
            new FakeShellService(),
            new FakeThemeService());
    }

    private static byte[] CreatePdf(string text) => PdfDocument.Create(pdf =>
        pdf.Page(page => page.Content(content => content.Paragraph(paragraph => paragraph.Text(text))))).ToBytes();

    private sealed class FakeFileDialogService : IFileDialogService
    {
        public Task<IReadOnlyList<string>> PickFilesAsync(PdfInputKind inputKind, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<string>>([]);

        public Task<string?> PickFolderAsync(CancellationToken cancellationToken = default) => Task.FromResult<string?>(null);
    }

    private sealed class FakeShellService : IShellService
    {
        public void OpenFolder(string path)
        {
        }

        public void OpenFile(string path)
        {
        }
    }

    private sealed class FakeThemeService : IThemeService
    {
        public AppThemeMode CurrentMode => AppThemeMode.System;

        public void ApplySavedTheme()
        {
        }

        public void SetTheme(AppThemeMode mode)
        {
        }
    }
}
