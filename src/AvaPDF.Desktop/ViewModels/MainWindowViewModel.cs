using AvaPDF.Application.Operations;
using AvaPDF.Desktop.Services;
using AvaPDF.Domain;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AvaPDF.Desktop.ViewModels;

public sealed record ThemeOption(AppThemeMode Mode, string Label, string Glyph);

public sealed partial class PdfToolNavigationItem : ObservableObject
{
    [ObservableProperty]
    private bool _isSelected;

    public PdfToolNavigationItem(PdfToolDescriptor descriptor) => Descriptor = descriptor;

    public PdfToolDescriptor Descriptor { get; }

    public string Name => Descriptor.Name;

    public string Glyph => Descriptor.Glyph;

    public IReadOnlyList<PdfToolNavigationItem>? Tools => null;

    public bool IsExpanded => false;
}

public sealed partial class PdfToolCategoryViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isExpanded;

    public PdfToolCategoryViewModel(string name, IReadOnlyList<PdfToolNavigationItem> tools, bool isExpanded)
    {
        Name = name;
        Tools = tools;
        _isExpanded = isExpanded;
    }

    public string Name { get; }

    public string Glyph => string.Empty;

    public IReadOnlyList<PdfToolNavigationItem> Tools { get; }
}

public partial class MainWindowViewModel : ObservableObject
{
    private readonly IThemeService _themeService;

    [ObservableProperty]
    private PdfToolDescriptor _selectedTool;

    [ObservableProperty]
    private ThemeOption _selectedThemeOption;

    [ObservableProperty]
    private object? _selectedNavigationNode;

    public MainWindowViewModel(
        PdfJobService jobs,
        IFileDialogService dialogs,
        IShellService shell,
        IThemeService themeService)
    {
        _themeService = themeService;
        Tools = PdfToolCatalog.Tools;
        var navigationItems = Tools.Select(tool => new PdfToolNavigationItem(tool)).ToArray();
        ToolCategories = Tools
            .GroupBy(tool => tool.Category)
            .Select((group, index) => new PdfToolCategoryViewModel(
                group.Key,
                navigationItems.Where(item => item.Descriptor.Category == group.Key).ToArray(),
                index == 0))
            .ToArray();
        ThemeOptions =
        [
            new ThemeOption(AppThemeMode.System, "跟随系统", "◐"),
            new ThemeOption(AppThemeMode.Light, "亮色模式", "☀"),
            new ThemeOption(AppThemeMode.Dark, "暗色模式", "☾")
        ];
        _selectedThemeOption = ThemeOptions.Single(option => option.Mode == themeService.CurrentMode);
        Workspace = new ToolWorkspaceViewModel(jobs, dialogs, shell);
        _selectedTool = Tools[0];
        Workspace.Configure(_selectedTool);
        UpdateToolSelection();
    }

    public IReadOnlyList<PdfToolDescriptor> Tools { get; }

    public IReadOnlyList<PdfToolCategoryViewModel> ToolCategories { get; }

    public IReadOnlyList<ThemeOption> ThemeOptions { get; }

    public ToolWorkspaceViewModel Workspace { get; }

    [RelayCommand]
    private void SelectTool(PdfToolDescriptor tool) => SelectedTool = tool;

    [RelayCommand]
    private static void ToggleCategory(PdfToolCategoryViewModel category) => category.IsExpanded = !category.IsExpanded;

    partial void OnSelectedToolChanged(PdfToolDescriptor value)
    {
        Workspace.Configure(value);
        UpdateToolSelection();
    }

    private void UpdateToolSelection()
    {
        foreach (var item in ToolCategories.SelectMany(category => category.Tools))
        {
            item.IsSelected = item.Descriptor.Kind == SelectedTool.Kind;
        }
    }

    partial void OnSelectedNavigationNodeChanged(object? value)
    {
        if (value is PdfToolNavigationItem item)
        {
            SelectedTool = item.Descriptor;
        }
    }

    partial void OnSelectedThemeOptionChanged(ThemeOption value) => _themeService.SetTheme(value.Mode);
}
