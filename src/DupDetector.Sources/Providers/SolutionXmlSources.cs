using System.Xml;
using System.Xml.Linq;

namespace DupDetector.Sources.Providers;

/// <summary>
///     Helpers for the <c>.slnx</c> source provider.
/// </summary>
public static class SolutionXmlSources
{
    /// <summary>
    ///     
    /// </summary>
    /// <returns></returns>
    /// <param name="path"></param>
    public static bool CanHandle(string path)
    {
        return Path.GetExtension(path).Equals(".slnx", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    ///     Reads the declared project paths, warning about each missing project individually rather
    ///     than only when every one of them is absent.
    /// </summary>
    /// <returns></returns>
    /// <param name="solutionXmlPath"></param>
    /// <param name="root"></param>
    /// <param name="diagnostics"></param>
    public static HashSet<string> ReadProjectPaths(
        string solutionXmlPath,
        string root,
        List<SourceDiagnostic> diagnostics)
    {
        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        XDocument document;
        try
        {
            document = XDocument.Load(solutionXmlPath);
        }
        catch (XmlException exception)
        {
            diagnostics.Add(SourceDiagnostics.Error(
                $"'{solutionXmlPath}' is not valid XML: {exception.Message}",
                solutionXmlPath));

            return paths;
        }

        foreach (var element in document.Descendants("Project"))
        {
            var declared = element.Attribute("Path")?.Value;
            if (string.IsNullOrWhiteSpace(declared))
            {
                continue;
            }

            var resolved = Path.GetFullPath(Path.Combine(root, declared));
            if (File.Exists(resolved))
            {
                paths.Add(resolved);
            }
            else
            {
                diagnostics.Add(SourceDiagnostics.Warning(
                    "Project referenced by the solution is missing.",
                    resolved));
            }
        }

        return paths;
    }
}
