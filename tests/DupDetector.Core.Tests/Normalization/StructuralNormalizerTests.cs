using Xunit;

namespace DupDetector.Core.Tests.Normalization;

/// <summary>
///     
/// </summary>
public class StructuralNormalizerTests
{

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void AllDeclarationSitesAreCollected()
    {
        const string Source = """
            class C
            {
                T M<T>(T seed, System.Collections.Generic.List<T> items)
                {
                    var accumulator = seed;
                    foreach (var item in items) { accumulator = item; }
                    if (items is { Count: > 0 } populated) { accumulator = populated[0]; }
                    try { Local(); } catch (System.Exception error) { System.Console.Write(error); }
                    T Local() { return accumulator; }
                    return accumulator;
                }
            }
            """;

        var normalized = NormalizerFixtures.Normalize(Source);

        Assert.DoesNotContain("accumulator", normalized, StringComparison.Ordinal);
        Assert.DoesNotContain("populated", normalized, StringComparison.Ordinal);
        Assert.DoesNotContain("error", normalized, StringComparison.Ordinal);
        Assert.DoesNotContain("seed", normalized, StringComparison.Ordinal);
        Assert.DoesNotContain("Local", normalized, StringComparison.Ordinal);
        Assert.Contains("Console", normalized, StringComparison.Ordinal);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void DeclaredNameReusedAsConditionalMemberNameIsPreserved()
    {
        var normalized = NormalizerFixtures.Normalize("class C { void M(Order Price) { var x = Price?.Price; } }");

        Assert.Contains("Order var1", normalized, StringComparison.Ordinal);
        Assert.Contains("Price", normalized, StringComparison.Ordinal);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void DeclaredNameReusedAsQualifiedNameSegmentIsPreserved()
    {
        var normalized = NormalizerFixtures.Normalize(
            "class C { void M(int Generic) { System.Collections.Generic.List<int> items = null; } }");

        Assert.Contains("int var1", normalized, StringComparison.Ordinal);
        Assert.Contains("Collections", normalized, StringComparison.Ordinal);
        Assert.Contains("Generic", normalized, StringComparison.Ordinal);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void DeclaredNamesAreRenamedInOrderOfAppearance()
    {
        Assert.Contains("var0", NormalizerFixtures.Normalize("class C { void M(int alpha) { int beta = alpha; } }"), StringComparison.Ordinal);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void GenuineCopiesStillMatchDespiteRenaming()
    {
        var original = NormalizerFixtures.Hash("class C { int Total(Order order) { var sum = order.Price; return sum; } }");
        var copy = NormalizerFixtures.Hash("class C { int Sum(Order invoice) { var accumulator = invoice.Price; return accumulator; } }");

        Assert.Equal(original, copy);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Hash_IsStableAcrossCalls()
    {
        Assert.Equal(NormalizerFixtures.Hash("class C { void M() { } }"), NormalizerFixtures.Hash("class C { void M() { } }"));
    }

    /// <summary>
    ///     
    /// </summary>
    /// <param name="source"></param>
    /// <param name="placeholder"></param>
    [Theory]
    [InlineData("class C { void M() { var x = \"s\"; } }", "STR")]
    [InlineData("class C { void M() { var x = \"s\"u8; } }", "STR")]
    [InlineData("class C { void M() { var x = 1; } }", "NUM")]
    [InlineData("class C { void M() { var x = 'c'; } }", "CHR")]
    [InlineData("class C { void M() { var x = true; } }", "BOOL")]
    [InlineData("class C { void M() { var x = false; } }", "BOOL")]
    [InlineData("class C { void M() { object x = null; } }", "NULL")]
    [InlineData("class C { void M() { int x = default; } }", "LIT")]
    public void LiteralsBecomeKindPlaceholders(string source, string placeholder)
    {
        Assert.Contains(placeholder, NormalizerFixtures.Normalize(source), StringComparison.Ordinal);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void LiteralsOfDifferentValuesButOneKindCollapseTogether()
    {
        Assert.Equal(
            NormalizerFixtures.Hash("class C { int M() { return 1; } }"),
            NormalizerFixtures.Hash("class C { int M() { return 9999; } }"));
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void LocalSharingMemberNameDoesNotRenameTheMember()
    {
        var normalized = NormalizerFixtures.Normalize("class C { void M(Order Price) { var x = Price.Price; } }");

        Assert.Equal("class C { void var0 ( Order var1 ) { var var2 = var1 . Price ; } }", normalized);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void MemberAccessNamesArePreserved()
    {
        var price = NormalizerFixtures.Hash("class C { int M(Order o) { return o.Price; } }");
        var quantity = NormalizerFixtures.Hash("class C { int M(Order o) { return o.Quantity; } }");

        Assert.NotEqual(price, quantity);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void MemberBindingNamesArePreserved()
    {
        var first = NormalizerFixtures.Hash("class C { int? M(Order o) { return o?.Price; } }");
        var second = NormalizerFixtures.Hash("class C { int? M(Order o) { return o?.Quantity; } }");

        Assert.NotEqual(first, second);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void ParameterAndLocalTypesArePreservedConsistently()
    {
        var ints = NormalizerFixtures.Hash("class C { int M(int a) { int t = a; return t; } }");
        var longs = NormalizerFixtures.Hash("class C { long M(long a) { long t = a; return t; } }");

        Assert.NotEqual(ints, longs);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void QualifiedTypeNamesArePreserved()
    {
        var first = NormalizerFixtures.Hash("class C { void M() { System.Console.Write(1); } }");
        var second = NormalizerFixtures.Hash("class C { void M() { System.Diagnostics.Trace.Write(1); } }");

        Assert.NotEqual(first, second);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void RenamedIdentifierKeepsTheSameReplacementEverywhere()
    {
        var normalized = NormalizerFixtures.Normalize("class C { void M(int alpha) { int b = alpha; int c = alpha; } }");
        Assert.Equal(3, normalized.Split("var1").Length - 1);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void UnrelatedMappersOverDifferentTypesDoNotCollide()
    {
        var widget = NormalizerFixtures.Hash("class C { WidgetResult Process(WidgetInput input) { var r = new WidgetResult(); r.Name = input.Name; return r; } }");
        var gadget = NormalizerFixtures.Hash("class C { GadgetResult Handle(GadgetInput input) { var g = new GadgetResult(); g.Name = input.Name; return g; } }");

        Assert.NotEqual(widget, gadget);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void UnrelatedMethodsWithTheSameShapeDoNotCollide()
    {
        var charge = NormalizerFixtures.Hash("class C { int ChargeCard(int p) { int amount = 100; return amount; } }");
        var email = NormalizerFixtures.Hash("class C { int SendEmail(int p) { int retries = 5; return retries; } }");

        Assert.Equal(charge, email);
    }
}
