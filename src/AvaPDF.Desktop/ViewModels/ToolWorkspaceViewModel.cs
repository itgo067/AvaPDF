using System.Collections.ObjectModel;
using System.Collections.Specialized;
using AvaPDF.Application.Abstractions;
using AvaPDF.Application.Operations;
using AvaPDF.Desktop.IO;
using AvaPDF.Desktop.Services;
using AvaPDF.Domain;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AvaPDF.Desktop.ViewModels;

public partial class ToolWorkspaceViewModel : ObservableObject
{
    private readonly PdfJobService _jobs;
    private readonly IFileDialogService _dialogs;
    private readonly IShellService _shell;
    private CancellationTokenSource? _cancellation;

    [ObservableProperty]
    private PdfToolDescriptor _currentTool = PdfToolCatalog.Tools[0];

    [ObservableProperty]
    private string _outputDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        "AvaPDF");

    [ObservableProperty]
    private string _pageRanges = "1-3";

    [ObservableProperty]
    private string _pageOrder = "3,1-2";

    [ObservableProperty]
    private int _pagesPerPart = 2;

    [ObservableProperty]
    private int _rotationAngle = 90;

    [ObservableProperty]
    private SplitModeOption _selectedSplitMode;

    [ObservableProperty]
    private OptimizationOption _selectedOptimization;

    [ObservableProperty]
    private bool _showPageRanges;

    [ObservableProperty]
    private bool _showPageOrder;

    [ObservableProperty]
    private bool _showSplitOptions;

    [ObservableProperty]
    private bool _showSplitPageCount;

    [ObservableProperty]
    private bool _showSplitRanges;

    [ObservableProperty]
    private bool _showRotation;

    [ObservableProperty]
    private bool _showOptimization;

    [ObservableProperty]
    private bool _isRunning;

    [ObservableProperty]
    private double _progressValue;

    [ObservableProperty]
    private string _statusMessage = "选择文件后即可开始处理。";

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private bool _hasOutputs;

    [ObservableProperty]
    private string _pageRangeWatermark = "例如 1-3,5";

    [ObservableProperty]
    private string _inputHint = "支持 PDF 文件，可调整处理顺序";

    public ToolWorkspaceViewModel(PdfJobService jobs, IFileDialogService dialogs, IShellService shell)
    {
        _jobs = jobs;
        _dialogs = dialogs;
        _shell = shell;
        _selectedSplitMode = SplitModes[0];
        _selectedOptimization = OptimizationLevels[0];
        Files.CollectionChanged += OnFilesChanged;
    }

    public ObservableCollection<LocalFileItem> Files { get; } = [];

    public ObservableCollection<string> OutputFiles { get; } = [];

    public IReadOnlyList<SplitModeOption> SplitModes { get; } =
    [
        new(PdfSplitMode.EveryPage, "每一页拆为一个文件"),
        new(PdfSplitMode.PageCount, "每 N 页拆为一个文件"),
        new(PdfSplitMode.CustomRanges, "按自定义范围拆分")
    ];

    public IReadOnlyList<OptimizationOption> OptimizationLevels { get; } =
    [
        new(PdfOptimizationLevel.Balanced, "均衡（推荐）"),
        new(PdfOptimizationLevel.Maximum, "最大无损压缩"),
        new(PdfOptimizationLevel.Web, "Web 发布"),
        new(PdfOptimizationLevel.Archival, "归档兼容")
    ];

    public IReadOnlyList<int> RotationAngles { get; } = [90, 180, 270];

    public void Configure(PdfToolDescriptor tool)
    {
        if (IsRunning)
        {
            _cancellation?.Cancel();
        }

        CurrentTool = tool;
        Files.Clear();
        OutputFiles.Clear();
        HasOutputs = false;
        ErrorMessage = string.Empty;
        ProgressValue = 0;
        StatusMessage = "选择文件后即可开始处理。";
        ShowSplitOptions = tool.Kind == PdfOperationKind.Split;
        ShowRotation = tool.Kind == PdfOperationKind.Rotate;
        ShowOptimization = tool.Kind == PdfOperationKind.Optimize;
        ShowPageOrder = tool.Kind == PdfOperationKind.Reorder;
        ShowPageRanges = tool.Kind is PdfOperationKind.Extract or PdfOperationKind.Delete
            or PdfOperationKind.Rotate or PdfOperationKind.ExtractText;
        PageRanges = tool.Kind switch
        {
            PdfOperationKind.Extract => "1-3",
            PdfOperationKind.Delete => "2",
            PdfOperationKind.Reorder => "3,1-2",
            _ => string.Empty
        };
        PageRangeWatermark = tool.Kind is PdfOperationKind.Rotate or PdfOperationKind.ExtractText
            ? "留空处理全部页面，或输入 1-3,5"
            : "例如 1-3,5";
        InputHint = tool.InputKind == PdfInputKind.Images
            ? "支持 JPG、PNG、BMP、WEBP、SVG 等，按列表顺序生成页面"
            : tool.Kind == PdfOperationKind.Merge
                ? "至少选择两个 PDF，按列表顺序合并"
                : "选择一个 PDF 文件";
        SelectedSplitMode = SplitModes[0];
        UpdateSplitVisibility();
    }

    [RelayCommand]
    private async Task AddFilesAsync()
    {
        if (IsRunning)
        {
            return;
        }

        var paths = await _dialogs.PickFilesAsync(CurrentTool.InputKind == PdfInputKind.Images);
        var maximum = CurrentTool.MaximumInputs;
        foreach (var path in paths)
        {
            if (maximum.HasValue && Files.Count >= maximum.Value)
            {
                Files.RemoveAt(0);
            }

            Files.Add(new LocalFileItem(path));
        }
    }

    [RelayCommand]
    private void RemoveFile(LocalFileItem? file)
    {
        if (file is not null && !IsRunning)
        {
            Files.Remove(file);
        }
    }

    [RelayCommand]
    private void ClearFiles()
    {
        if (!IsRunning)
        {
            Files.Clear();
        }
    }

    [RelayCommand]
    private void MoveUp(LocalFileItem? file) => Move(file, -1);

    [RelayCommand]
    private void MoveDown(LocalFileItem? file) => Move(file, 1);

    [RelayCommand]
    private async Task BrowseOutputFolderAsync()
    {
        var folder = await _dialogs.PickFolderAsync();
        if (!string.IsNullOrWhiteSpace(folder))
        {
            OutputDirectory = folder;
        }
    }

    [RelayCommand]
    private void OpenOutputFolder()
    {
        try
        {
            _shell.OpenFolder(OutputDirectory);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or System.ComponentModel.Win32Exception)
        {
            ErrorMessage = $"无法打开输出目录：{exception.Message}";
        }
    }

    [RelayCommand]
    private void OpenOutputFile(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        try
        {
            _shell.OpenFile(path);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or System.ComponentModel.Win32Exception)
        {
            ErrorMessage = $"无法打开结果文件：{exception.Message}";
        }
    }

    [RelayCommand(CanExecute = nameof(CanRun))]
    private async Task RunAsync()
    {
        IsRunning = true;
        ErrorMessage = string.Empty;
        OutputFiles.Clear();
        HasOutputs = false;
        ProgressValue = 0;
        StatusMessage = "正在准备任务…";
        var cancellation = new CancellationTokenSource();
        _cancellation = cancellation;
        var target = new LocalPdfOutputTarget(OutputDirectory.Trim());

        try
        {
            var request = BuildRequest(target);
            var progress = new Progress<PdfOperationProgress>(update =>
            {
                if (update.Percent.HasValue)
                {
                    ProgressValue = update.Percent.Value;
                }

                StatusMessage = update.Message;
            });
            var result = await Task.Run(
                () => _jobs.ExecuteAsync(request, progress, cancellation.Token),
                cancellation.Token);
            foreach (var path in target.CreatedPaths)
            {
                OutputFiles.Add(path);
            }

            HasOutputs = OutputFiles.Count > 0;
            ProgressValue = 100;
            StatusMessage = result.Message;
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "任务已取消，未处理的源文件保持不变。";
            ProgressValue = 0;
        }
        catch (Exception exception)
        {
            ErrorMessage = $"处理失败：{exception.Message}";
            StatusMessage = "请检查文件和参数后重试。";
            ProgressValue = 0;
        }
        finally
        {
            cancellation.Dispose();
            _cancellation = null;
            IsRunning = false;
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        if (_cancellation is not null)
        {
            StatusMessage = "正在取消任务…";
            _cancellation.Cancel();
        }
    }

    private PdfOperationRequest BuildRequest(IPdfOutputTarget target)
    {
        var sources = Files.Select(file => new LocalPdfSource(file.Path)).Cast<IPdfSource>().ToArray();
        return CurrentTool.Kind switch
        {
            PdfOperationKind.Merge => new MergePdfRequest(sources, target),
            PdfOperationKind.Split => new SplitPdfRequest(sources[0], SelectedSplitMode.Value, PagesPerPart, PageRanges, target),
            PdfOperationKind.Extract => new ExtractPdfRequest(sources[0], PageRanges, target),
            PdfOperationKind.Delete => new DeletePdfRequest(sources[0], PageRanges, target),
            PdfOperationKind.Rotate => new RotatePdfRequest(sources[0], PageRanges, RotationAngle, target),
            PdfOperationKind.Reorder => new ReorderPdfRequest(sources[0], PageOrder, target),
            PdfOperationKind.Optimize => new OptimizePdfRequest(sources[0], SelectedOptimization.Value, target),
            PdfOperationKind.ImagesToPdf => new ImagesToPdfRequest(sources, target),
            PdfOperationKind.ExtractText => new ExtractTextPdfRequest(sources[0], PageRanges, target),
            _ => throw new NotSupportedException($"不支持的工具：{CurrentTool.Kind}")
        };
    }

    private bool CanRun() => !IsRunning
        && !string.IsNullOrWhiteSpace(OutputDirectory)
        && Files.Count >= CurrentTool.MinimumInputs
        && (!CurrentTool.MaximumInputs.HasValue || Files.Count <= CurrentTool.MaximumInputs.Value);

    private void Move(LocalFileItem? file, int offset)
    {
        if (file is null || IsRunning)
        {
            return;
        }

        var oldIndex = Files.IndexOf(file);
        var newIndex = oldIndex + offset;
        if (oldIndex >= 0 && newIndex >= 0 && newIndex < Files.Count)
        {
            Files.Move(oldIndex, newIndex);
        }
    }

    private void OnFilesChanged(object? sender, NotifyCollectionChangedEventArgs e) =>
        RunCommand.NotifyCanExecuteChanged();

    partial void OnIsRunningChanged(bool value) => RunCommand.NotifyCanExecuteChanged();

    partial void OnOutputDirectoryChanged(string value) => RunCommand.NotifyCanExecuteChanged();

    partial void OnSelectedSplitModeChanged(SplitModeOption value) => UpdateSplitVisibility();

    private void UpdateSplitVisibility()
    {
        ShowSplitPageCount = ShowSplitOptions && SelectedSplitMode.Value == PdfSplitMode.PageCount;
        ShowSplitRanges = ShowSplitOptions && SelectedSplitMode.Value == PdfSplitMode.CustomRanges;
        if (ShowSplitRanges && string.IsNullOrWhiteSpace(PageRanges))
        {
            PageRanges = "1-2,3-4";
        }
    }
}
