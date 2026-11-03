using AvaPDF.Desktop.Services;
using AvaPDF.Desktop.ViewModels;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform.Storage;

namespace AvaPDF.Desktop.Views;

public partial class ToolWorkspaceView : UserControl
{
    public ToolWorkspaceView() => InitializeComponent();

    private void OnInputDragEnter(object? sender, DragEventArgs e) => UpdateDragState(e);

    private void OnInputDragOver(object? sender, DragEventArgs e) => UpdateDragState(e);

    private void OnInputDragLeave(object? sender, DragEventArgs e) => SetDragOver(false);

    private void OnInputDrop(object? sender, DragEventArgs e)
    {
        SetDragOver(false);
        if (DataContext is not ToolWorkspaceViewModel viewModel || viewModel.IsRunning)
        {
            e.DragEffects = DragDropEffects.None;
            return;
        }

        var paths = GetDroppedPaths(e);
        var accepted = paths.Where(path => FileDialogService.IsSupportedFile(path, viewModel.CurrentTool.InputKind)).ToArray();
        viewModel.AddFiles(accepted);
        var rejected = paths.Length - accepted.Length;
        if (rejected > 0)
        {
            viewModel.StatusMessage = accepted.Length > 0
                ? $"已添加 {accepted.Length} 个文件，忽略 {rejected} 个格式不支持的文件。"
                : "拖入的文件格式与当前工具不匹配。";
        }

        e.DragEffects = accepted.Length > 0 ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private void UpdateDragState(DragEventArgs e)
    {
        var canDrop = DataContext is ToolWorkspaceViewModel { IsRunning: false } viewModel
            && GetDroppedPaths(e).Any(path => FileDialogService.IsSupportedFile(path, viewModel.CurrentTool.InputKind));
        e.DragEffects = canDrop ? DragDropEffects.Copy : DragDropEffects.None;
        SetDragOver(canDrop);
        e.Handled = true;
    }

    private void SetDragOver(bool isOver) => InputDropZone.Classes.Set("drag-over", isOver);

    private static string[] GetDroppedPaths(DragEventArgs e)
    {
        var items = e.DataTransfer.TryGetFiles();
        if (items is null)
        {
            return [];
        }

        return items.Select(item => item.TryGetLocalPath())
            .Where(path => !string.IsNullOrWhiteSpace(path) && File.Exists(path))
            .Cast<string>()
            .ToArray();
    }
}
