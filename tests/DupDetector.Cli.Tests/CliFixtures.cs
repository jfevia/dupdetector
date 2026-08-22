using DupDetector.Cli.CommandLine;

using DupDetector.Core.Normalization;

using Microsoft.CodeAnalysis.CSharp;

using Xunit;

namespace DupDetector.Cli.Tests;

/// <summary>
///     Shared helpers for the command-line suites.
/// </summary>
public static class CliFixtures
{
    /// <summary>
    ///     The structural hash of a source fragment.
    /// </summary>
    /// <returns></returns>
    /// <param name="source"></param>
    public static string Hash(string source)
    {
        return StructuralNormalizer.Normalize(CSharpSyntaxTree.ParseText(source).GetRoot()).Hash;
    }

    /// <summary>
    ///     Parses arguments and asserts they were accepted.
    /// </summary>
    /// <returns></returns>
    /// <param name="args"></param>
    public static CommandLineOptions Options(IReadOnlyList<string> args)
    {
        var result = Parse(args);
        Assert.Null(result.Error);
        Assert.NotNull(result.Options);
        return result.Options;
    }

    /// <summary>
    ///     Parses arguments.
    /// </summary>
    /// <returns></returns>
    /// <param name="args"></param>
    public static ParseResult Parse(IReadOnlyList<string> args)
    {
        return ArgumentParser.Parse(args, "9.9.9");
    }
}
