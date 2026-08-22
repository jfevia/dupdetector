using System.Text;

namespace DupDetector.Cli;

/// <summary>
/// Writes the report to standard output and everything else to standard error, so a report can be
/// piped without diagnostics contaminating it.
/// </summary>
internal sealed class ConsoleOutputSink : IOutputSink
{
    public void WriteReport(string content) => Console.Out.Write(content);

    public void WriteMessage(string message) => Console.Out.Write(message);

    public void Save(string path, string content)
    {
        var full = Path.GetFullPath(path);

        // A resolved file path always has a parent directory; creating it is a no-op when it exists.
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        File.WriteAllText(full, content, new UTF8Encoding(false));
    }
}
