using AvaPDF.Domain;
using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace AvaPDF.Desktop.Services;

public interface IFileDialogService
{
    Task<IReadOnlyList<string>> PickFilesAsync(PdfInputKind inputKind, CancellationToken cancellationToken = default);

    Task<string?> PickFolderAsync(CancellationToken cancellationToken = default);
}

public sealed class FileDialogService : IFileDialogService
{
    public TopLevel? TopLevel { get; set; }

    public async Task<IReadOnlyList<string>> PickFilesAsync(
        PdfInputKind inputKind,
        CancellationToken cancellationToken = default)
    {
        if (TopLevel is null)
        {
            return [];
        }

        var (title, filterName, patterns) = GetFilter(inputKind);
        var filter = new FilePickerFileType(filterName) { Patterns = patterns };
        var files = await TopLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = title,
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

    private static (string Title, string FilterName, string[] Patterns) GetFilter(PdfInputKind inputKind) =>
        inputKind switch
        {
            PdfInputKind.Images => ("选择图片", "图片文件",
                ["*.jpg", "*.jpeg", "*.png", "*.bmp", "*.gif", "*.webp", "*.tif", "*.tiff", "*.svg"]),
            PdfInputKind.Word => ("选择 Word 文档", "Word 文档", ["*.docx", "*.docm", "*.dotx", "*.dotm"]),
            PdfInputKind.Excel => ("选择 Excel 工作簿", "Excel 工作簿", ["*.xlsx", "*.xlsm", "*.xltx", "*.xltm"]),
            PdfInputKind.PowerPoint => ("选择 PowerPoint 演示文稿", "PowerPoint 演示文稿", ["*.pptx", "*.pptm"]),
            PdfInputKind.Markdown => ("选择 Markdown 文档", "Markdown 文档", ["*.md", "*.markdown"]),
            PdfInputKind.Html => ("选择 HTML 文档", "HTML 文档", ["*.html", "*.htm"]),
            PdfInputKind.Rtf => ("选择 RTF 文档", "RTF 富文本", ["*.rtf"]),
            PdfInputKind.Text => ("选择纯文本", "纯文本文件", ["*.txt"]),
            _ => ("选择 PDF 文件", "PDF 文档", ["*.pdf"])
        };
}
