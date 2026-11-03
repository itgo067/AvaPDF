using AvaPDF.Domain;

namespace AvaPDF.Desktop;

public sealed record StartupOptions(
    IReadOnlyList<string> Files,
    PdfOperationKind? Tool,
    string? Error = null)
{
    public static StartupOptions Parse(IReadOnlyList<string>? arguments)
    {
        if (arguments is null || arguments.Count == 0)
        {
            return new StartupOptions([], null);
        }

        var files = new List<string>();
        PdfOperationKind? tool = null;
        for (var index = 0; index < arguments.Count; index++)
        {
            var argument = arguments[index];
            if (argument == "--file")
            {
                if (++index >= arguments.Count || string.IsNullOrWhiteSpace(arguments[index]))
                {
                    return Fail("--file 需要提供文件路径。");
                }

                files.Add(arguments[index]);
                continue;
            }

            if (argument.StartsWith("--file=", StringComparison.OrdinalIgnoreCase))
            {
                var path = argument["--file=".Length..];
                if (string.IsNullOrWhiteSpace(path))
                {
                    return Fail("--file 需要提供文件路径。");
                }

                files.Add(path);
                continue;
            }

            if (argument == "--tool")
            {
                if (++index >= arguments.Count || !TryParseTool(arguments[index], out var parsed))
                {
                    return Fail("--tool 包含无法识别的工具标识。");
                }

                tool = parsed;
                continue;
            }

            if (argument.StartsWith("--tool=", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryParseTool(argument["--tool=".Length..], out var parsed))
                {
                    return Fail("--tool 包含无法识别的工具标识。");
                }

                tool = parsed;
                continue;
            }

            if (argument.StartsWith("-", StringComparison.Ordinal))
            {
                return Fail($"无法识别启动参数：{argument}");
            }

            files.Add(argument);
        }

        return new StartupOptions(files, tool);
    }

    private static StartupOptions Fail(string message) => new([], null, message);

    private static bool TryParseTool(string? value, out PdfOperationKind tool)
    {
        tool = default;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var normalized = Normalize(value);
        var alias = normalized switch
        {
            "WORD" or "DOC" or "DOCX" => PdfOperationKind.WordToPdf,
            "EXCEL" or "XLS" or "XLSX" => PdfOperationKind.ExcelToPdf,
            "POWERPOINT" or "PPT" or "PPTX" => PdfOperationKind.PowerPointToPdf,
            "MARKDOWN" or "MD" => PdfOperationKind.MarkdownToPdf,
            "HTML" => PdfOperationKind.HtmlToPdf,
            "RTF" => PdfOperationKind.RtfToPdf,
            "TXT" or "TEXT" => PdfOperationKind.TxtToPdf,
            "IMAGE" or "IMAGES" => PdfOperationKind.ImagesToPdf,
            _ => (PdfOperationKind?)null
        };
        if (alias.HasValue)
        {
            tool = alias.Value;
            return true;
        }

        var match = PdfToolCatalog.Tools.FirstOrDefault(candidate =>
            Normalize(candidate.Kind.ToString()) == normalized
            || Normalize(candidate.Name) == normalized);
        if (match is null)
        {
            return false;
        }

        tool = match.Kind;
        return true;
    }

    private static string Normalize(string value) =>
        value.Replace("-", string.Empty, StringComparison.Ordinal)
            .Replace("_", string.Empty, StringComparison.Ordinal)
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .ToUpperInvariant();
}
