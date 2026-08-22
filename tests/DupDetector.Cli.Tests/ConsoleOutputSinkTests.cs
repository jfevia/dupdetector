using System.Text;

using Xunit;

namespace DupDetector.Cli.Tests;

/// <summary>
///     
/// </summary>
public class ConsoleOutputSinkTests
{

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Save_AcceptsPathWithNoDirectoryComponent()
    {
        var name = "dupdetector-sink-" + Guid.NewGuid().ToString("N") + ".yaml";
        var original = Directory.GetCurrentDirectory();
        var scratch = Path.Combine(Path.GetTempPath(), "dupdetector-cwd-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(scratch);

        try
        {
            Directory.SetCurrentDirectory(scratch);
            var consoleOutputSink = new ConsoleOutputSink();
            consoleOutputSink.Save(name, "summary: ok");

            Assert.True(File.Exists(Path.Combine(scratch, name)));
        }
        finally
        {
            Directory.SetCurrentDirectory(original);
            Directory.Delete(scratch, recursive: true);
        }
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Save_CreatesMissingDirectories()
    {
        var root = Path.Combine(Path.GetTempPath(), "dupdetector-sink-" + Guid.NewGuid().ToString("N"));
        var path = Path.Combine(root, "nested", "deeper", "report.yaml");
        try
        {
            var consoleOutputSink2 = new ConsoleOutputSink();
            consoleOutputSink2.Save(path, "summary: ok");

            Assert.True(File.Exists(path));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Save_WritesUtf8WithoutByteOrderMark()
    {
        var path = Path.Combine(Path.GetTempPath(), "dupdetector-sink-" + Guid.NewGuid().ToString("N") + ".yaml");
        try
        {
            var consoleOutputSink3 = new ConsoleOutputSink();
            consoleOutputSink3.Save(path, "summary: ok");

            var bytes = File.ReadAllBytes(path);
            Assert.Equal("summary: ok", Encoding.UTF8.GetString(bytes));
            Assert.False(bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF);
        }
        finally
        {
            File.Delete(path);
        }
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void WriteMessage_GoesToStandardOutput()
    {
        var original = Console.Out;
        var captured = new StringWriter();
        try
        {
            Console.SetOut(captured);
            var consoleOutputSink4 = new ConsoleOutputSink();
            consoleOutputSink4.WriteMessage("help text");
        }
        finally
        {
            Console.SetOut(original);
        }

        Assert.Equal("help text", captured.ToString());
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void WriteReport_GoesToStandardOutput()
    {
        var original = Console.Out;
        var captured = new StringWriter();
        try
        {
            Console.SetOut(captured);
            var consoleOutputSink5 = new ConsoleOutputSink();
            consoleOutputSink5.WriteReport("report body");
        }
        finally
        {
            Console.SetOut(original);
        }

        Assert.Equal("report body", captured.ToString());
    }
}
