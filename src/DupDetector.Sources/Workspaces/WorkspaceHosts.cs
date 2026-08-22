namespace DupDetector.Sources.Workspaces;

/// <summary>
///     Creates the default workspace host.
/// </summary>
public static class WorkspaceHosts
{
    /// <summary>
    ///     
    /// </summary>
    /// <returns></returns>
    public static IWorkspaceHost Create()
    {
        var host = new MicrosoftBuildWorkspaceHost();
        return host;
    }
}
