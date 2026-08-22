using System.Text;

using Xunit;

namespace DupDetector.Sources.Tests;

/// <summary>
///     
/// </summary>
public class SourceDecoderTests
{

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Decode_ReadsMarkedUtf16()
    {
        const string Text = "class C { }";
        Assert.Equal(Text, SourceDecoder.Decode([.. Encoding.Unicode.GetPreamble(), .. Encoding.Unicode.GetBytes(Text)]));
        Assert.Equal(Text, SourceDecoder.Decode([.. Encoding.BigEndianUnicode.GetPreamble(), .. Encoding.BigEndianUnicode.GetBytes(Text)]));
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Decode_ReadsUtf8WithAndWithoutMark()
    {
        const string Text = "class C { }";
        byte[] marked = [.. Encoding.UTF8.GetPreamble(), .. Encoding.UTF8.GetBytes(Text)];

        Assert.Equal(Text, SourceDecoder.Decode(marked));
        Assert.Equal(Text, SourceDecoder.Decode(Encoding.UTF8.GetBytes(Text)));
        Assert.Equal(Encoding.UTF8, SourceDecoder.Detect(marked));
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Decode_RecognisesUtf16WithoutMark()
    {
        const string Text = "class Widget { public int Value { get; set; } }";

        Assert.Equal(Text, SourceDecoder.Decode(Encoding.Unicode.GetBytes(Text)));
        Assert.Equal(Text, SourceDecoder.Decode(Encoding.BigEndianUnicode.GetBytes(Text)));
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Detect_FallsBackToUtf8ForOrdinaryText()
    {
        var text = new string('a', 600);
        Assert.Equal(Encoding.UTF8, SourceDecoder.Detect(Encoding.UTF8.GetBytes(text)));
    }

    /// <summary>
    ///     
    /// </summary>
    /// <param name="text"></param>
    [Theory]
    [InlineData("")]
    [InlineData("ab")]
    public void Detect_FallsBackToUtf8ForTinyInputs(string text)
    {
        Assert.Equal(Encoding.UTF8, SourceDecoder.Detect(Encoding.UTF8.GetBytes(text)));
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Detect_FallsBackToUtf8WhenNullsAreEvenlySpread()
    {
        var bytes = new byte[64];
        Assert.Equal(Encoding.UTF8, SourceDecoder.Detect(bytes));
    }

    /// <summary>
    ///     
    /// </summary>
    /// <param name="bytes"></param>
    [Theory]
    [InlineData(new byte[]
    {
        0xEF,
        0xBB,
        0x00,
        0x41
    })]
    [InlineData(new byte[]
    {
        0xEF,
        0x00,
        0xBF,
        0x41
    })]
    [InlineData(new byte[]
    {
        0xFF,
        0x41,
        0x42,
        0x43
    })]
    [InlineData(new byte[]
    {
        0xFE,
        0x41,
        0x42,
        0x43
    })]
    public void Detect_RequiresCompleteMark(byte[] bytes)
    {
        Assert.NotEqual(Encoding.Unicode, SourceDecoder.Detect(bytes));
    }
}
