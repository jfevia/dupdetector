namespace DupDetector.Core.Internal;

/// <summary>
///     Argument guards used by the model so invalid settings cannot be represented.
/// </summary>
public static class Require
{
    /// <summary>
    ///     
    /// </summary>
    /// <returns></returns>
    /// <param name="value"></param>
    /// <param name="minimum"></param>
    /// <param name="name"></param>
    public static int AtLeast(int value, int minimum, string name)
    {
        if (value < minimum)
        {
            var argumentOutOfRangeException = new ArgumentOutOfRangeException(name, value, $"Value must be at least {minimum}.");
            throw argumentOutOfRangeException;
        }

        return value;
    }

    /// <summary>
    ///     
    /// </summary>
    /// <returns></returns>
    /// <param name="value"></param>
    /// <param name="minimum"></param>
    /// <param name="maximum"></param>
    /// <param name="name"></param>
    public static double InRange(double value, double minimum, double maximum, string name)
    {
        if (double.IsNaN(value) || value < minimum || value > maximum)
        {
            var argumentOutOfRangeException2 = new ArgumentOutOfRangeException(name, value, $"Value must be between {minimum} and {maximum}.");
            throw argumentOutOfRangeException2;
        }

        return value;
    }

    /// <summary>
    ///     
    /// </summary>
    /// <returns></returns>
    /// <param name="value"></param>
    /// <param name="name"></param>
    public static string NotBlank(string value, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            var argumentException = new ArgumentException("Value must not be blank.", name);
            throw argumentException;
        }

        return value;
    }
}
