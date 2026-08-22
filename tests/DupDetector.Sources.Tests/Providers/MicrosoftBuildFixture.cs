using Microsoft.Build.Locator;

namespace DupDetector.Sources.Tests.Providers;

/// <summary>
///     Registers MSBuild once per test process. Loading a solution needs a real SDK.
/// </summary>
public sealed class MicrosoftBuildFixture
{

    /// <summary>
    ///     
    /// </summary>
    public bool IsAvailable { get; }

    /// <summary>
    ///     
    /// </summary>
    public MicrosoftBuildFixture()
    {
        var instances = MSBuildLocator.QueryVisualStudioInstances();
        var hasInstance = false;
        foreach (var instance in instances)
        {
            if (instance is not null)
            {
                hasInstance = true;
                break;
            }
        }

        if (!MSBuildLocator.IsRegistered && hasInstance)
        {
            MSBuildLocator.RegisterDefaults();
        }

        IsAvailable = MSBuildLocator.IsRegistered;
    }
}
