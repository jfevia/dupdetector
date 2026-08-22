using System.Text;
using DupDetector.Core.Model;
using Xunit;

namespace DupDetector.Sources.Tests;

public class SourceParserTests
{
    [Fact]
    public void Options_PinAnExplicitLanguageVersion() =>
        Assert.Equal(Microsoft.CodeAnalysis.CSharp.LanguageVersion.Preview, SourceParser.Options.LanguageVersion);

    [Theory]
    [InlineData("class C { void M() { var s = \"\\e[0m\"; } }")]
    [InlineData("static class E { extension(string s) { public bool IsLong => s.Length > 10; } }")]
    [InlineData("partial class C { public partial int P { get; set; } }")]
    [InlineData("class C { int[] A = [1, 2, 3]; }")]
    public void Parse_AcceptsModernSyntax(string source) =>
        Assert.Null(SourceParser.DescribeParseFailures(SourceParser.Parse(source, "x.cs"), "x.cs"));

    [Fact]
    public void DescribeParseFailures_ReportsBrokenSource()
    {
        var diagnostic = SourceParser.DescribeParseFailures(SourceParser.Parse("class C { void M( }", "x.cs"), "x.cs");

        Assert.NotNull(diagnostic);
        Assert.Equal(SourceDiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.Equal("x.cs", diagnostic.Path);
        Assert.Contains("parse error", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DescribeParseFailures_RejectsNull() =>
        Assert.Throws<ArgumentNullException>(() => SourceParser.DescribeParseFailures(null!, "x.cs"));

    [Fact]
    public void Diagnostics_CarryTheirSeverity()
    {
        Assert.Equal(SourceDiagnosticSeverity.Error, SourceDiagnostic.Error("m").Severity);
        Assert.Equal(SourceDiagnosticSeverity.Warning, SourceDiagnostic.Warning("m").Severity);
        Assert.Empty(SourceLoadResult.Empty.Units);
        Assert.Empty(SourceLoadResult.Empty.Diagnostics);
        Assert.Equal(DiscoveryMode.None, SourceLoadResult.Empty.Stats.Mode);
    }
}

public class SourceDecoderTests
{
    [Fact]
    public void Decode_RejectsNull() => Assert.Throws<ArgumentNullException>(() => SourceDecoder.Decode(null!));

    [Fact]
    public void Decode_ReadsUtf8WithAndWithoutAMark()
    {
        const string Text = "class C { }";
        byte[] marked = [.. Encoding.UTF8.GetPreamble(), .. Encoding.UTF8.GetBytes(Text)];

        Assert.Equal(Text, SourceDecoder.Decode(marked));
        Assert.Equal(Text, SourceDecoder.Decode(Encoding.UTF8.GetBytes(Text)));
        Assert.Equal(Encoding.UTF8, SourceDecoder.Detect(marked));
    }

    [Theory]
    // Partial byte-order marks must not be mistaken for complete ones.
    [InlineData(new byte[] { 0xEF, 0xBB, 0x00, 0x41 })]
    [InlineData(new byte[] { 0xEF, 0x00, 0xBF, 0x41 })]
    [InlineData(new byte[] { 0xFF, 0x41, 0x42, 0x43 })]
    [InlineData(new byte[] { 0xFE, 0x41, 0x42, 0x43 })]
    public void Detect_RequiresACompleteMark(byte[] bytes) =>
        Assert.NotEqual(Encoding.Unicode, SourceDecoder.Detect(bytes));

    [Fact]
    public void Detect_FallsBackToUtf8WhenNullsAreEvenlySpread()
    {
        // Equal NUL counts on both halves mean neither endianness wins.
        Assert.Equal(Encoding.UTF8, SourceDecoder.Detect(new byte[64]));
    }

    [Fact]
    public void Decode_ReadsMarkedUtf16()
    {
        const string Text = "class C { }";
        Assert.Equal(Text, SourceDecoder.Decode([.. Encoding.Unicode.GetPreamble(), .. Encoding.Unicode.GetBytes(Text)]));
        Assert.Equal(Text, SourceDecoder.Decode([.. Encoding.BigEndianUnicode.GetPreamble(), .. Encoding.BigEndianUnicode.GetBytes(Text)]));
    }

    [Fact]
    public void Decode_RecognisesUtf16WithoutAMark()
    {
        // Read as UTF-8 this becomes text interleaved with NUL characters.
        const string Text = "class Widget { public int Value { get; set; } }";

        Assert.Equal(Text, SourceDecoder.Decode(Encoding.Unicode.GetBytes(Text)));
        Assert.Equal(Text, SourceDecoder.Decode(Encoding.BigEndianUnicode.GetBytes(Text)));
    }

    [Theory]
    [InlineData("")]
    [InlineData("ab")]
    public void Detect_FallsBackToUtf8ForTinyInputs(string text) =>
        Assert.Equal(Encoding.UTF8, SourceDecoder.Detect(Encoding.UTF8.GetBytes(text)));

    [Fact]
    public void Detect_FallsBackToUtf8ForOrdinaryText() =>
        Assert.Equal(Encoding.UTF8, SourceDecoder.Detect(Encoding.UTF8.GetBytes(new string('a', 600))));
}

public class GeneratedFileDetectorTests
{
    [Fact]
    public void IsGenerated_RejectsNullArguments()
    {
        Assert.Throws<ArgumentNullException>(() => GeneratedFileDetector.IsGenerated(null!, "x"));
        Assert.Throws<ArgumentNullException>(() => GeneratedFileDetector.IsGenerated("x.cs", null!));
    }

    [Theory]
    [InlineData("Form.Designer.cs", true)]
    [InlineData("Model.g.cs", true)]
    [InlineData("Model.generated.cs", true)]
    [InlineData("OrderService.cs", false)]
    public void IsGenerated_RecognisesGeneratedNames(string fileName, bool expected) =>
        Assert.Equal(expected, GeneratedFileDetector.HasGeneratedName(fileName));

    [Fact]
    public void IsGenerated_HonoursHeaderMarkers() =>
        Assert.True(GeneratedFileDetector.IsGenerated("C.cs", "// <auto-generated />\nclass C { }"));

    [Fact]
    public void IsGenerated_IgnoresMarkersOutsideTheHeader()
    {
        // The defect that made the loader exclude its own source: a marker mentioned deep in a file.
        var content = string.Join('\n', Enumerable.Repeat("// filler", 60).Append("if (x.Contains(\"[GeneratedCode\")) { }"));

        Assert.False(GeneratedFileDetector.IsGenerated("Loader.cs", content));
    }

    [Fact]
    public void IsGenerated_ReadsAMarkerOnTheLastLineOfAShortFile() =>
        Assert.True(GeneratedFileDetector.HasHeaderMarker("class C { }\n// <auto-generated"));

    [Fact]
    public void IsGenerated_HandlesAFileWithoutNewlines() =>
        Assert.False(GeneratedFileDetector.HasHeaderMarker("class C { }"));
}

public class ProjectNameResolverTests
{
    private sealed class StubProbe(Dictionary<string, string?> projects) : IDirectoryProbe
    {
        internal int Calls { get; private set; }

        public string? FindProjectFile(string directory)
        {
            Calls++;
            return projects.TryGetValue(directory, out var project) ? project : null;
        }
    }

    [Fact]
    public void Resolve_RejectsNull() =>
        Assert.Throws<ArgumentNullException>(() => new ProjectNameResolver().Resolve(null!));

    [Fact]
    public void Resolve_FindsTheNearestProjectFile()
    {
        var root = Path.GetFullPath(Path.Combine("C:", "repo", "src", "App"));
        var probe = new StubProbe(new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            [root] = Path.Combine(root, "App.csproj"),
        });

        var resolver = new ProjectNameResolver(probe);

        Assert.Equal(ProjectIdentity.Named("App"), resolver.Resolve(Path.Combine(root, "Service.cs")));
    }

    [Fact]
    public void Resolve_WalksUpwardsUntilItFindsAProject()
    {
        var app = Path.GetFullPath(Path.Combine("C:", "repo", "src", "App"));
        var nested = Path.Combine(app, "Domain", "Orders");
        var probe = new StubProbe(new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            [app] = Path.Combine(app, "App.csproj"),
        });

        Assert.Equal(ProjectIdentity.Named("App"), new ProjectNameResolver(probe).Resolve(Path.Combine(nested, "Order.cs")));
    }

    [Fact]
    public void Resolve_ReportsUnknownWhenNoProjectExists()
    {
        var probe = new StubProbe([]);
        var resolver = new ProjectNameResolver(probe);

        Assert.Equal(ProjectIdentity.Unknown, resolver.Resolve(Path.GetFullPath(Path.Combine("C:", "loose", "File.cs"))));
    }

    [Fact]
    public void Resolve_ProbesEachDirectoryOnlyOnce()
    {
        var app = Path.GetFullPath(Path.Combine("C:", "repo", "src", "App"));
        var probe = new StubProbe(new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            [app] = Path.Combine(app, "App.csproj"),
        });
        var resolver = new ProjectNameResolver(probe);

        for (var index = 0; index < 50; index++)
        {
            resolver.Resolve(Path.Combine(app, $"File{index}.cs"));
        }

        Assert.Equal(1, probe.Calls);
        Assert.Equal(1, resolver.CachedDirectoryCount);
    }

    [Fact]
    public void Resolve_ReusesAnAncestorResultForSiblingDirectories()
    {
        var app = Path.GetFullPath(Path.Combine("C:", "repo", "src", "App"));
        var probe = new StubProbe(new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            [app] = Path.Combine(app, "App.csproj"),
        });
        var resolver = new ProjectNameResolver(probe);

        resolver.Resolve(Path.Combine(app, "A", "One.cs"));
        var before = probe.Calls;
        resolver.Resolve(Path.Combine(app, "B", "Two.cs"));

        // Only the new leaf directory is probed; the cached ancestor stops the walk.
        Assert.Equal(before + 1, probe.Calls);
    }

    [Fact]
    public void Resolve_ReturnsUnknownForARootPath() =>
        Assert.Equal(ProjectIdentity.Unknown, new ProjectNameResolver(new StubProbe([])).Resolve(Path.GetPathRoot(Path.GetFullPath("."))!));

    [Fact]
    public void FileSystemProbe_ReturnsNullForAMissingDirectory() =>
        Assert.Null(FileSystemDirectoryProbe.Instance.FindProjectFile(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString())));
}
