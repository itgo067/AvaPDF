using AvaTools.Application.Common;
using Xunit;

namespace AvaTools.Tests;

public sealed class PageRangeExpressionTests
{
    [Theory]
    [InlineData("1-3,5", new[] { 1, 2, 3, 5 })]
    [InlineData(" 3, 1-2 ", new[] { 3, 1, 2 })]
    [InlineData("1-2；4，5", new[] { 1, 2, 4, 5 })]
    public void Resolve_preserves_caller_order(string expression, int[] expected) =>
        Assert.Equal(expected, PageRangeExpression.Resolve(expression, 5));

    [Fact]
    public void Resolve_can_expand_empty_selection_to_all_pages() =>
        Assert.Equal([1, 2, 3], PageRangeExpression.Resolve(null, 3, allowEmpty: true));

    [Fact]
    public void Resolve_can_remove_duplicate_pages() =>
        Assert.Equal([1, 2, 3], PageRangeExpression.Resolve("1-2,2-3", 3, distinct: true));

    [Theory]
    [InlineData("0")]
    [InlineData("2-1")]
    [InlineData("1-6")]
    [InlineData("one")]
    [InlineData("1,,x")]
    public void Resolve_rejects_invalid_expressions(string expression) =>
        Assert.Throws<ArgumentException>(() => PageRangeExpression.Resolve(expression, 5));

    [Fact]
    public void Resolve_requires_a_value_by_default() =>
        Assert.Throws<ArgumentException>(() => PageRangeExpression.Resolve("", 5));
}
