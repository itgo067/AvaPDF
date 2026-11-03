using AvaPDF.Application.Operations;
using AvaPDF.Desktop.Services;
using AvaPDF.Domain;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ursa.Controls;

namespace AvaPDF.Desktop.ViewModels;

public sealed record ThemeOption(AppThemeMode Mode, string Label, string Glyph);

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
        ToolCategories = Tools
            .GroupBy(tool => tool.Category)
            .Select(group => new NavMenuItem
            {
                Header = group.Key,
                ItemsSource = group.Select(tool => new NavMenuItem
                {
                    Header = tool.Name,
                    Command = SelectToolCommand,
                    CommandParameter = tool
                }).ToArray()
            })
            .ToArray();
    }

    public IReadOnlyList<PdfToolDescriptor> Tools { get; }

    public IReadOnlyList<NavMenuItem> ToolCategories { get; }

    public IReadOnlyList<ThemeOption> ThemeOptions { get; }

    public ToolWorkspaceViewModel Workspace { get; }

    [RelayCommand]
    private void SelectTool(PdfToolDescriptor tool) => SelectedTool = tool;

    partial void OnSelectedToolChanged(PdfToolDescriptor value) => Workspace.Configure(value);

    partial void OnSelectedNavigationNodeChanged(object? value)
    {
        if (value is NavMenuItem { CommandParameter: PdfToolDescriptor tool })
        {
            SelectedTool = tool;
        }
    }

    partial void OnSelectedThemeOptionChanged(ThemeOption value) => _themeService.SetTheme(value.Mode);
}
