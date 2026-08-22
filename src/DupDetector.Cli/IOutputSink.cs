namespace DupDetector.Cli;

/// <summary>
///     Where the program's output goes. Injected so a run can be observed without a console.
/// </summary>
public interface IOutputSink
{

    /// <summary>
    ///     
    /// </summary>
    /// <param name="path"></param>
    /// <param name="content"></param>
    void Save(string path, string content);

    /// <summary>
    ///     
    /// </summary>
    /// <param name="message"></param>
    void WriteMessage(string message);

    /// <summary>
    ///     
    /// </summary>
    /// <param name="content"></param>
    void WriteReport(string content);
}
