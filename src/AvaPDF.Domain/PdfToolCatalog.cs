namespace AvaPDF.Domain;

public static class PdfToolCatalog
{
    private static readonly IReadOnlyList<PdfToolDescriptor> ToolList =
    [
        new(PdfOperationKind.Merge, "合并 PDF", "按列表顺序将多个 PDF 合并为一个文件", "页面工具", "合", MaximumInputs: null),
        new(PdfOperationKind.Split, "拆分 PDF", "逐页、按页数或按自定义范围拆分", "页面工具", "拆"),
        new(PdfOperationKind.Extract, "提取页面", "将指定页面提取为新的 PDF", "页面工具", "取"),
        new(PdfOperationKind.Delete, "删除页面", "从文档中移除指定页面", "页面工具", "删"),
        new(PdfOperationKind.Rotate, "旋转页面", "将所选页面顺时针旋转 90°、180° 或 270°", "页面工具", "转"),
        new(PdfOperationKind.Reorder, "重排页面", "按新的页码顺序重新组织全部页面", "页面工具", "排"),
        new(PdfOperationKind.Optimize, "优化 PDF", "无损压缩结构并清理重复对象", "优化与转换", "优"),
        new(PdfOperationKind.ImagesToPdf, "图片转 PDF", "将 JPG、PNG、BMP、WEBP 等图片按顺序转成 PDF", "优化与转换", "图", PdfInputKind.Images, MaximumInputs: null),
        new(PdfOperationKind.ExtractText, "提取文本", "提取 PDF 中的原生文本并保存为 TXT", "优化与转换", "文"),
        new(PdfOperationKind.WordToPdf, "Word 转 PDF", "将 DOC、DOCX、DOCM 等 Word 文档转换为 PDF", "文档转换", "W", PdfInputKind.Word),
        new(PdfOperationKind.ExcelToPdf, "Excel 转 PDF", "将 XLS、XLSX、XLSM 等工作簿转换为 PDF", "文档转换", "X", PdfInputKind.Excel),
        new(PdfOperationKind.PowerPointToPdf, "PowerPoint 转 PDF", "将 PPT、PPTX、PPTM 演示文稿转换为 PDF", "文档转换", "P", PdfInputKind.PowerPoint),
        new(PdfOperationKind.MarkdownToPdf, "Markdown 转 PDF", "保留标题、表格、代码块、图片与文档主题", "文档转换", "M", PdfInputKind.Markdown),
        new(PdfOperationKind.HtmlToPdf, "HTML 转 PDF", "将静态 HTML 文档转换为可搜索的 PDF", "文档转换", "H", PdfInputKind.Html),
        new(PdfOperationKind.RtfToPdf, "RTF 转 PDF", "将 RTF 富文本文档转换为 PDF", "文档转换", "R", PdfInputKind.Rtf),
        new(PdfOperationKind.TxtToPdf, "TXT 转 PDF", "保留纯文本的换行、空白与制表符并生成 PDF", "文档转换", "T", PdfInputKind.Text),
        new(PdfOperationKind.Watermark, "添加水印", "添加文字或图片水印，支持页面范围、透明度与旋转", "安全与信息", "印"),
        new(PdfOperationKind.Encrypt, "添加密码", "使用 AES-256 为 PDF 添加打开密码与所有者密码", "安全与信息", "密"),
        new(PdfOperationKind.UpdateMetadata, "编辑元数据", "修改 PDF 标题、作者、主题和关键词", "安全与信息", "息"),
        new(PdfOperationKind.Decrypt, "删除密码", "验证所有者密码后生成无密码 PDF", "安全与信息", "锁")
    ];

    public static IReadOnlyList<PdfToolDescriptor> Tools => ToolList;

    public static PdfToolDescriptor Get(PdfOperationKind kind) =>
        ToolList.Single(tool => tool.Kind == kind);
}
