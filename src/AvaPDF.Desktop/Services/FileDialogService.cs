using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace AvaPDF.Desktop.Services;

public interface IFileDialogService
{
    Task<IReadOnlyList<string>> PickFilesAsync(bool images, CancellationToken cancellationToken = default);

    Task<string?> PickFolderAsync(CancellationToken cancellationToken = default);
}

public sealed class FileDialogService : IFileDialogService
{
    public TopLevel? TopLevel { get; set; }

    public async Task<IReadOnlyList<string>> PickFilesAsync(
        bool images,
        CancellationToken cancellationToken = default)
    {
        if (TopLevel is null)
        {
            return [];
        }

        var filter = images
            ? new FilePickerFileType("图片文件")
            {
                Patterns = ["*.jpg", "*.jpeg", "*.png", "*.bmp", "*.gif", "*.webp", "*.tif", "*.tiff", "*.svg"]
            }
            : new FilePickerFileType("PDF 文档") { Patterns = ["*.pdf"] };
        var files = await TopLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = images ? "选择图片" : "选择 PDF 文件",
            AllowMultiple = true,
            FileTypeFilter = [filter]
        });

        cancellationToken.ThrowIfCancellationRequested();
        return files.Select(file => file.TryGetLocalPath())
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Cast<string>()
            .ToArray();
    }

    public async Task<string?> PickFolderAsync(CancellationToken cancellationToken = default)
    {
        if (TopLevel is null)
        {
            return null;
        }

        var folders = await TopLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "选择输出目录",
            AllowMultiple = false
        });
        cancellationToken.ThrowIfCancellationRequested();
        return folders.Count > 0 ? folders[0].TryGetLocalPath() : null;
    }
}
