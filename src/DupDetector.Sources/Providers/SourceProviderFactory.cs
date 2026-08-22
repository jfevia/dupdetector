namespace DupDetector.Sources.Providers;

/// <summary>
///     Chooses the provider that loads one input path.
/// </summary>
/// <param name="path">The path to load.</param>
/// <returns>The provider for that path.</returns>
public delegate ISourceProvider SourceProviderFactory(string path);
