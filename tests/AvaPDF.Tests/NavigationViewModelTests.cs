using AvaPDF.Application.Operations;
using AvaPDF.Desktop.Services;
using AvaPDF.Desktop.ViewModels;
using AvaPDF.Domain;
using Xunit;

namespace AvaPDF.Tests;

public sealed class NavigationViewModelTests
{
    [Fact]
    public void Selecting_tools_across_categories_keeps_one_selection_and_workspace()
    {
        var viewModel = CreateViewModel();
        var optimize = PdfToolCatalog.Get(PdfOperationKind.Optimize);
        var watermark = PdfToolCatalog.Get(PdfOperationKind.Watermark);

        viewModel.SelectToolCommand.Execute(optimize);
        Assert.Same(optimize, viewModel.SelectedTool);
        Assert.Same(optimize, viewModel.Workspace.CurrentTool);
        Assert.Single(viewModel.ToolCategories.SelectMany(category => category.Tools), item => item.IsSelected);

        viewModel.SelectToolCommand.Execute(watermark);
        Assert.Same(watermark, viewModel.SelectedTool);
        Assert.Same(watermark, viewModel.Workspace.CurrentTool);
        Assert.Single(viewModel.ToolCategories.SelectMany(category => category.Tools), item => item.IsSelected);

        var category = viewModel.ToolCategories.Single(category => category.Name == "安全与信息");
        viewModel.ToggleCategoryCommand.Execute(category);
        Assert.True(category.IsExpanded);
        Assert.Same(watermark, viewModel.SelectedTool);
        Assert.Single(viewModel.ToolCategories.SelectMany(category => category.Tools), item => item.IsSelected);
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
