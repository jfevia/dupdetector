using System.Text;

namespace DupDetector.Cli;

/// <summary>
///     Writes the report to standard output and everything else to standard error, so a report can be
///     piped without diagnostics contaminating it.
/// </summary>
public sealed class ConsoleOutputSink : IOutputSink
{

    /// <summary>
    ///     
    /// </summary>
    /// <param name="path"></param>
    /// <param name="content"></param>
    public void Save(string path, string content)
    {
        var full = Path.GetFullPath(path);

        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        var encoding = new UTF8Encoding(false);
        File.WriteAllText(full, content, encoding);
    }

    /// <summary>
    ///     
    /// </summary>
    /// <param name="message"></param>
    public void WriteMessage(string message)
    {
        Console.Out.Write(message);
    }

    /// <summary>
    ///     
    /// </summary>
    /// <param name="content"></param>
    public void WriteReport(string content)
    {
        Console.Out.Write(content);
    }
}
