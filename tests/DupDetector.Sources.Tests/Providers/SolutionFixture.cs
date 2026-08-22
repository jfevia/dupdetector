
namespace DupDetector.Sources.Tests.Providers;

/// <summary>
///     A temporary solution with two projects, where App references Lib.
/// </summary>
public sealed class SolutionFixture : IDisposable
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

        public class Order
        {
            public int Price { get; set; }
        }
        """;

    /// <summary>
    ///     
    /// </summary>
    public string BrokenProjectPath { get; }

    /// <summary>
    ///     
    /// </summary>
    public string ProjectPath { get; }

    /// <summary>
    ///     
    /// </summary>
    public string Root { get; }

    /// <summary>
    ///     
    /// </summary>
    public string SolutionPath { get; }

    /// <summary>
    ///     
    /// </summary>
    public string SolutionXmlPath { get; }

    /// <summary>
    ///     
    /// </summary>
    public string SolutionXmlWithBrokenPath { get; }

    /// <summary>
    ///     
    /// </summary>
    public SolutionFixture()
    {
        Root = Path.Combine(Path.GetTempPath(), "dupdetector-sln-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(Root, "App"));
        Directory.CreateDirectory(Path.Combine(Root, "Lib"));

        File.WriteAllText(
            Path.Combine(Root, "Lib", "Lib.csproj"),
            "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup></Project>");

        File.WriteAllText(
            Path.Combine(Root, "App", "App.csproj"),
            "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup>" +
            "<ItemGroup><ProjectReference Include=\"..\\Lib\\Lib.csproj\" /></ItemGroup></Project>");

        File.WriteAllText(Path.Combine(Root, "Lib", "LibCalculator.cs"), Duplicated);
        File.WriteAllText(Path.Combine(Root, "App", "AppCalculator.cs"), Duplicated);

        SolutionXmlPath = Path.Combine(Root, "Sample.slnx");
        File.WriteAllText(
            SolutionXmlPath,
            "<Solution>\n  <Project Path=\"App/App.csproj\" />\n  <Project Path=\"Lib/Lib.csproj\" />\n</Solution>");

        ProjectPath = Path.Combine(Root, "App", "App.csproj");

        BrokenProjectPath = Path.Combine(Root, "Broken", "NotAProject.txt");
        Directory.CreateDirectory(Path.Combine(Root, "Broken"));
        File.WriteAllText(BrokenProjectPath, "not a project");

        SolutionXmlWithBrokenPath = Path.Combine(Root, "Broken.slnx");
        File.WriteAllText(
            SolutionXmlWithBrokenPath,
            "<Solution>\n  <Project Path=\"App/App.csproj\" />\n  <Project Path=\"Broken/NotAProject.txt\" />\n</Solution>");

        SolutionPath = Path.Combine(Root, "Sample.sln");
        File.WriteAllText(
            SolutionPath,
            """
            Microsoft Visual Studio Solution File, Format Version 12.00
            # Visual Studio Version 17
            Project("{9A19103F-16F7-4668-BE54-9A1E7A4F7556}") = "Lib", "Lib\Lib.csproj", "{11111111-1111-1111-1111-111111111111}"
            EndProject
            Project("{9A19103F-16F7-4668-BE54-9A1E7A4F7556}") = "App", "App\App.csproj", "{22222222-2222-2222-2222-222222222222}"
            EndProject
            Global
            	GlobalSection(SolutionConfigurationPlatforms) = preSolution
            		Debug|Any CPU = Debug|Any CPU
            	EndGlobalSection
            EndGlobal
            """);
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
