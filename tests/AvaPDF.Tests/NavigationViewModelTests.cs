using AvaPDF.Application.Operations;
using AvaPDF.Desktop.Services;
using AvaPDF.Desktop.ViewModels;
using AvaPDF.Domain;
using Ursa.Controls;
using Xunit;

namespace AvaPDF.Tests;

public sealed class NavigationViewModelTests
{
    [Fact]
    public void NavMenu_uses_real_items_and_keeps_selection_across_categories()
    {
        var viewModel = CreateViewModel();
        var optimize = PdfToolCatalog.Get(PdfOperationKind.Optimize);
        var watermark = PdfToolCatalog.Get(PdfOperationKind.Watermark);

        viewModel.SelectToolCommand.Execute(optimize);
        Assert.Same(optimize, viewModel.SelectedTool);
        Assert.Same(optimize, viewModel.Workspace.CurrentTool);

        var securityCategory = viewModel.ToolCategories.Single(item => Equals(item.Header, "安全与信息"));
        var watermarkItem = securityCategory.ItemsSource!.Cast<NavMenuItem>()
            .Single(item => Equals(item.Header, "添加水印"));
        Assert.Same(watermark, watermarkItem.CommandParameter);
        Assert.Equal("印", watermarkItem.Icon);

        viewModel.SelectedNavigationNode = watermarkItem;
        Assert.Same(watermark, viewModel.SelectedTool);
        Assert.Same(watermark, viewModel.Workspace.CurrentTool);
    }

    private static MainWindowViewModel CreateViewModel() => new(
        new PdfJobService([]),
        new FakeFileDialogService(),
        new FakeShellService(),
        new FakeThemeService());

    private sealed class FakeFileDialogService : IFileDialogService
    {
        public Task<IReadOnlyList<string>> PickFilesAsync(PdfInputKind inputKind, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<string>>([]);

        public Task<string?> PickFolderAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<string?>(null);
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
