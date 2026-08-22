namespace DupDetector.Testing;

/// <summary>
///     The entry point the test SDK requires; supplying our own keeps the SDK from injecting one.
/// </summary>
public static class TestEntryPoint
{
    /// <summary>
    ///     
    /// </summary>
    /// <returns></returns>
    /// <param name="args"></param>
    public static int Main(string[] args)
    {
        return args.Length;
    }
}
