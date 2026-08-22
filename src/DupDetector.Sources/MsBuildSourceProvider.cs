using DupDetector.Core.Model;

namespace DupDetector.Sources;

/// <summary>
/// Loads source from a solution or project via MSBuild, so files carry real project identity.
/// </summary>
// An empty workspace is an error, not an empty success, which a clean solution is indistinguishable from.
public sealed class MsBuildSourceProvider : ISourceProvider
{
    private readonly Func<IWorkspaceHost> _createHost;

    public MsBuildSourceProvider()
        : this(static () => new MsBuildWorkspaceHost())
    {
    }

    internal MsBuildSourceProvider(Func<IWorkspaceHost> createHost) => _createHost = createHost;

    /// <summary>Extensions this provider understands.</summary>
    public static IReadOnlyList<string> Extensions { get; } = [".sln", ".slnf", ".csproj"];

    public static bool Handles(string path) =>
        Extensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase);

    public SourceLoadResult Load(string path, DetectionSettings settings, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(settings);

        var full = Path.GetFullPath(path);
        if (!File.Exists(full))
        {
            return SourceLoadResult.Empty with
            {
                Diagnostics = [SourceDiagnostic.Error($"Path does not exist: {full}", full)],
            };
        }

        var diagnostics = new List<SourceDiagnostic>();
        using var host = _createHost();

        try
        {
            var projects = host.Open(full, diagnostics, cancellationToken);
            var harvest = WorkspaceHarvester.Collect(projects, Path.GetDirectoryName(full)!, settings, cancellationToken);
            diagnostics.AddRange(harvest.Diagnostics);

            if (harvest.Units.Count == 0 && !diagnostics.Exists(IsError))
            {
                diagnostics.Add(SourceDiagnostic.Error(
                    $"'{full}' produced no source files. This usually means the SDK or a package restore is " +
                    "missing; an empty report would otherwise look like a clean solution.",
                    full));
            }

            return harvest with { Diagnostics = diagnostics };
        }
        catch (Exception exception) when (exception is InvalidOperationException or IOException or NotSupportedException)
        {
            diagnostics.Add(SourceDiagnostic.Error($"Could not open '{full}': {exception.Message}", full));
            return SourceLoadResult.Empty with { Diagnostics = diagnostics };
        }
    }

    private static bool IsError(SourceDiagnostic diagnostic) => diagnostic.Severity == SourceDiagnosticSeverity.Error;
}
