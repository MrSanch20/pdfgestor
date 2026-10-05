using PdfGestor.Core;

namespace PdfGestor.Core.Tests;

public class PageRangeParserTests
{
    [Fact]
    public void Parses_single_pages_and_ranges()
    {
        var ranges = PageRangeParser.Parse("1-3, 5, 8-10", pageCount: 10);

        Assert.Equal(new[] { new PageRange(1, 3), new PageRange(5, 5), new PageRange(8, 10) }, ranges);
    }

    [Theory]
    [InlineData("7-", 7, 10)]
    [InlineData("-3", 1, 3)]
    [InlineData(" 2 - 4 ", 2, 4)]
    public void Supports_open_ranges_and_spaces(string text, int start, int end)
    {
        var range = Assert.Single(PageRangeParser.Parse(text, pageCount: 10));

        Assert.Equal(new PageRange(start, end), range);
    }

    [Fact]
    public void Accepts_semicolons_and_ignores_empty_parts()
    {
        var ranges = PageRangeParser.Parse("1;;3,", pageCount: 5);

        Assert.Equal(new[] { new PageRange(1, 1), new PageRange(3, 3) }, ranges);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(",")]
    [InlineData("abc")]
    [InlineData("0")]
    [InlineData("5-2")]
    [InlineData("1-11")]
    [InlineData("1-2-3")]
    public void Rejects_invalid_input_with_friendly_error(string text)
    {
        var ex = Assert.Throws<PdfGestorException>(() => PageRangeParser.Parse(text, pageCount: 10));

        Assert.False(string.IsNullOrWhiteSpace(ex.Message));
    }

    [Fact]
    public void PageRange_to_string_is_compact()
    {
        Assert.Equal("4", new PageRange(4, 4).ToString());
        Assert.Equal("1-3", new PageRange(1, 3).ToString());
    }
}
