using AvaPDF.Application.Operations;
using AvaPDF.Desktop;
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

        Assert.All(viewModel.ToolCategories, category => Assert.True(category.IsVerticalCollapsed));
        var securityCategory = viewModel.ToolCategories.Single(item => Equals(item.Header, "安全与信息"));
        var watermarkItem = securityCategory.ItemsSource!.Cast<NavMenuItem>()
            .Single(item => Equals(item.Header, "添加水印"));
        Assert.Same(watermark, watermarkItem.CommandParameter);
        Assert.Null(watermarkItem.Icon);

        viewModel.SelectedNavigationNode = watermarkItem;
        Assert.Same(watermark, viewModel.SelectedTool);
        Assert.Same(watermark, viewModel.Workspace.CurrentTool);
    }

    [Fact]
    public void Startup_file_is_loaded_after_tool_initialization()
    {
        var viewModel = CreateViewModel();
        var directory = Directory.CreateTempSubdirectory();
        var path = Path.Combine(directory.FullName, "sample file.pdf");
        File.WriteAllText(path, "%PDF-1.4\n");

        viewModel.ApplyStartupOptions(new StartupOptions([path], PdfOperationKind.Optimize));

        Assert.Equal(PdfOperationKind.Optimize, viewModel.SelectedTool.Kind);
        var file = Assert.Single(viewModel.Workspace.Files);
        Assert.Equal(path, file.Path);
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
