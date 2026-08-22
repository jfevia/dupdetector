namespace DupDetector.Cli.Tests;

/// <summary>
///     A disposable source tree with duplicated code in two projects.
/// </summary>
public sealed class Workspace : IDisposable
{
    private const string Duplicated = """
        namespace Sample;

        public class Calculator
        {
            public int Total(Order order)
            {
                var running = order.Price;
                var adjusted = running;
                var final = adjusted;
                return final;
            }
        }
        """;

    /// <summary>
    ///     
    /// </summary>
    public string Root { get; }

    /// <summary>
    ///     
    /// </summary>
    public Workspace()
    {
        Root = Path.Combine(Path.GetTempPath(), "dupdetector-cli-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(Root, "App"));
        Directory.CreateDirectory(Path.Combine(Root, "Lib"));

        File.WriteAllText(Path.Combine(Root, "App", "App.csproj"), "<Project />");
        File.WriteAllText(Path.Combine(Root, "Lib", "Lib.csproj"), "<Project />");
        File.WriteAllText(Path.Combine(Root, "App", "AppCalculator.cs"), Duplicated);
        File.WriteAllText(Path.Combine(Root, "Lib", "LibCalculator.cs"), Duplicated);
    }

    /// <summary>
    ///     
    /// </summary>
    public void Dispose()
    {
        _ = CanTryDelete();
    }

    private bool CanTryDelete()
    {
        try
        {
            Directory.Delete(Root, recursive: true);
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }
}
