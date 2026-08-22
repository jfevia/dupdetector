using Xunit;

namespace DupDetector.Reporting.Tests;

/// <summary>
///     
/// </summary>
public class ReportFormatTests
{

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void For_ReturnsWriterPerFormat()
    {
        Assert.Equal(ReportFormat.Yaml, ReportWriters.For(ReportFormat.Yaml).Format);
        Assert.Equal(ReportFormat.Json, ReportWriters.For(ReportFormat.Json).Format);
        Assert.Equal(ReportFormat.Html, ReportWriters.For(ReportFormat.Html).Format);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Names_ListTheSupportedFormats()
    {
        Assert.Equal(["yaml", "json", "markup", "sarif"], ReportFormats.Names);
    }

    /// <summary>
    ///     
    /// </summary>
    /// <param name="value"></param>
    /// <param name="expected"></param>
    [Theory]
    [InlineData("yaml", ReportFormat.Yaml)]
    [InlineData("YAML", ReportFormat.Yaml)]
    [InlineData("  json  ", ReportFormat.Json)]
    public void TryParse_AcceptsKnownNames(string value, ReportFormat expected)
    {
        Assert.True(ReportFormats.CanTryParse(value, out var format));
        Assert.Equal(expected, format);
    }

    /// <summary>
    ///     
    /// </summary>
    /// <param name="value"></param>
    [Theory]
    [InlineData("xml")]
    [InlineData("")]
    [InlineData(null)]
    public void TryParse_RejectsAnythingElse(string? value)
    {
        Assert.False(ReportFormats.CanTryParse(value, out _));
    }
}
