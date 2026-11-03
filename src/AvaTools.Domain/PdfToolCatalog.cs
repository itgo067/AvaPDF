namespace AvaTools.Domain;

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
        new(PdfOperationKind.ExtractText, "提取文本", "提取 PDF 中的原生文本并保存为 TXT", "优化与转换", "文")
    ];

    public static IReadOnlyList<PdfToolDescriptor> Tools => ToolList;

    public static PdfToolDescriptor Get(PdfOperationKind kind) =>
        ToolList.Single(tool => tool.Kind == kind);
}
