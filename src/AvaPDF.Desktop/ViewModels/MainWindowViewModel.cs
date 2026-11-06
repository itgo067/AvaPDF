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
    private readonly IShellService _shell;
    private readonly IUpdateService? _updateService;

    [ObservableProperty]
    private PdfToolDescriptor _selectedTool;

    [ObservableProperty]
    private UpdateInfo? _availableUpdate;

    [ObservableProperty]
    private ThemeOption _selectedThemeOption;

    [ObservableProperty]
    private object? _selectedNavigationNode;

    public MainWindowViewModel(
        PdfJobService jobs,
        IFileDialogService dialogs,
        IShellService shell,
        IThemeService themeService,
        IUpdateService? updateService = null)
    {
        _themeService = themeService;
        _shell = shell;
        _updateService = updateService;
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
                IsVerticalCollapsed = true,
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

    public bool HasUpdate => AvailableUpdate is not null;

    public string UpdateMessage => AvailableUpdate is null
        ? string.Empty
        : $"发现 AvaPDF {AvailableUpdate.LatestVersion} 可更新（当前 {AvailableUpdate.CurrentVersion}）";

    public async Task CheckForUpdatesAsync()
    {
        if (_updateService is not null)
        {
            AvailableUpdate = await _updateService.CheckForUpdateAsync();
        }
    }

    public void OpenUpdate()
    {
        if (AvailableUpdate is not null)
        {
            _shell.OpenFile(AvailableUpdate.ReleaseUrl);
        }
    }

    public void IgnoreUpdate()
    {
        if (AvailableUpdate is not null && _updateService is not null)
        {
            _updateService.IgnoreVersion(AvailableUpdate.LatestVersion);
            AvailableUpdate = null;
        }
    }

    public async Task ApplyStartupOptions(StartupOptions options)
    {
        if (options.AutoRun)
        {
            await ApplyQuickAction(options);
            return;
        }

        if (options.Tool.HasValue)
        {
            SelectedTool = PdfToolCatalog.Get(options.Tool.Value);
        }
        else if (options.Files.Count > 0)
        {
            var selection = ShellFileClassifier.Classify(options.Files);
            PdfOperationKind? inferredTool = selection switch
            {
                ShellSelectionKind.SinglePdf => PdfOperationKind.ExtractText,
                ShellSelectionKind.MultiplePdfs => PdfOperationKind.Merge,
                ShellSelectionKind.SingleImage or ShellSelectionKind.MultipleImages => PdfOperationKind.ImagesToPdf,
                ShellSelectionKind.SingleDocument or ShellSelectionKind.MultipleDocuments => ShellFileClassifier.GetConversionTool(options.Files[0]),
                _ => null
            };
            if (inferredTool.HasValue)
            {
                SelectedTool = PdfToolCatalog.Get(inferredTool.Value);
            }
        }

        var accepted = options.Files
            .Where(File.Exists)
            .Where(path => FileDialogService.IsSupportedFile(path, SelectedTool.InputKind))
            .ToArray();
        Workspace.AddFiles(accepted);
        var rejected = options.Files.Count - accepted.Length;
        if (!string.IsNullOrWhiteSpace(options.Error))
        {
            Workspace.StatusMessage = options.Error;
        }
        else if (rejected > 0)
        {
            Workspace.StatusMessage = $"已忽略 {rejected} 个不存在或格式不匹配的启动文件。";
        }
        else if (accepted.Length > 0)
        {
            Workspace.StatusMessage = $"已从外部调用载入 {accepted.Length} 个文件，请确认后开始处理。";
        }
    }

    private async Task ApplyQuickAction(StartupOptions options)
    {
        var files = options.Files.Where(File.Exists).ToArray();
        if (files.Length != options.Files.Count || files.Length == 0)
        {
            Workspace.ErrorMessage = "快速任务包含不存在或不可读取的输入文件。";
            Workspace.StatusMessage = "任务未执行。";
            return;
        }

        var selection = ShellFileClassifier.Classify(files);
        if (selection == ShellSelectionKind.SinglePdf
            && options.Tool == PdfOperationKind.ExtractText)
        {
            SelectedTool = PdfToolCatalog.Get(PdfOperationKind.ExtractText);
            Workspace.OutputDirectory = Path.GetDirectoryName(files[0])!;
            Workspace.AddFiles(files);
            await Workspace.RunCurrentToolAsync();
            return;
        }

        if (options.Tool is null && selection is ShellSelectionKind.SingleDocument
            or ShellSelectionKind.MultipleDocuments or ShellSelectionKind.SingleImage)
        {
            var firstTool = ShellFileClassifier.GetConversionTool(files[0]);
            if (firstTool.HasValue)
            {
                SelectedTool = PdfToolCatalog.Get(firstTool.Value);
            }
            await Workspace.RunBatchConversionsAsync(files);
            return;
        }

        Workspace.ErrorMessage = "当前选中内容不支持此快速任务；请在 AvaPDF 工作区中确认参数后执行。";
        Workspace.StatusMessage = "任务未执行。";
    }

    [RelayCommand]
    private void SelectTool(PdfToolDescriptor tool) => SelectedTool = tool;

    partial void OnSelectedToolChanged(PdfToolDescriptor value) => Workspace.Configure(value);

    partial void OnAvailableUpdateChanged(UpdateInfo? value)
    {
        OnPropertyChanged(nameof(HasUpdate));
        OnPropertyChanged(nameof(UpdateMessage));
    }

    partial void OnSelectedNavigationNodeChanged(object? value)
    {
        if (value is NavMenuItem { CommandParameter: PdfToolDescriptor tool })
        {
            SelectedTool = tool;
        }
    }

    partial void OnSelectedThemeOptionChanged(ThemeOption value) => _themeService.SetTheme(value.Mode);
}
