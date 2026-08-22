using Xunit;

namespace DupDetector.Sources.Tests.Providers;

/// <summary>
///     
/// </summary>
[CollectionDefinition("msbuild")]
public sealed class MicrosoftBuildCollection : ICollectionFixture<MicrosoftBuildFixture>;
