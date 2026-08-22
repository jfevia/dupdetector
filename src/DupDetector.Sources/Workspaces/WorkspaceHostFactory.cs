namespace DupDetector.Sources.Workspaces;

/// <summary>
///     Creates the workspace host a provider loads projects through.
/// </summary>
/// <returns>The new host.</returns>
public delegate IWorkspaceHost WorkspaceHostFactory();
