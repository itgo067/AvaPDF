using System.Globalization;

namespace AvaTools.Application.Common;

/// <summary>Parses one-based page expressions such as "1-3,5;8-9" without engine dependencies.</summary>
public static class PageRangeExpression
{
    public static IReadOnlyList<int> Resolve(
        string? expression,
        int pageCount,
        bool allowEmpty = false,
        bool distinct = false)
    {
        if (pageCount < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(pageCount), "PDF 至少需要包含一页。 ");
        }

        if (string.IsNullOrWhiteSpace(expression))
        {
            if (!allowEmpty)
            {
                throw new ArgumentException("请输入页码范围，例如 1-3,5。");
            }

            return Enumerable.Range(1, pageCount).ToArray();
        }

        var normalized = expression.Replace('，', ',').Replace('；', ';');
        var segments = normalized.Split([',', ';'], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0)
        {
            throw new ArgumentException("页码范围格式无效。");
        }

        var pages = new List<int>();
        var seen = new HashSet<int>();
        foreach (var segment in segments)
        {
            var bounds = segment.Split('-', StringSplitOptions.TrimEntries);
            if (bounds.Length is < 1 or > 2 || bounds.Any(string.IsNullOrWhiteSpace))
            {
                throw new ArgumentException($"无法识别页码范围“{segment}”。");
            }

            var start = ParsePage(bounds[0], segment);
            var end = bounds.Length == 2 ? ParsePage(bounds[1], segment) : start;
            if (start > end)
            {
                throw new ArgumentException($"页码范围“{segment}”的起始页不能大于结束页。");
            }

            if (end > pageCount)
            {
                throw new ArgumentException($"页码范围“{segment}”超出文档总页数 {pageCount}。");
            }

            for (var page = start; page <= end; page++)
            {
                if (!distinct || seen.Add(page))
                {
                    pages.Add(page);
                }
            }
        }

        return pages;
    }

    private static int ParsePage(string value, string segment)
    {
        if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var page) || page < 1)
        {
            throw new ArgumentException($"页码范围“{segment}”必须使用大于 0 的整数。");
        }

        return page;
    }
}
