using System.Text;

namespace DupDetector.Cli.Tests;

/// <summary>
///     Captures everything a run produces.
/// </summary>
public sealed class RecordingSink : IOutputSink
{

    /// <summary>
    ///     
    /// </summary>
    public StringBuilder Messages { get; }

    /// <summary>
    ///     
    /// </summary>
    public StringBuilder Report { get; }

    /// <summary>
    ///     
    /// </summary>
    public Dictionary<string, string> Saved { get; }

    /// <summary>
    ///     
    /// </summary>
    public RecordingSink()
    {
        Messages = new();
        Report = new();
        Saved = [];
    }

    /// <summary>
    ///     
    /// </summary>
    /// <param name="path"></param>
    /// <param name="content"></param>
    public void Save(string path, string content)
    {
        Saved[path] = content;
    }

    /// <summary>
    ///     
    /// </summary>
    /// <param name="message"></param>
    public void WriteMessage(string message)
    {
        Messages.Append(message);
    }

    /// <summary>
    ///     
    /// </summary>
    /// <param name="content"></param>
    public void WriteReport(string content)
    {
        Report.Append(content);
    }
}
