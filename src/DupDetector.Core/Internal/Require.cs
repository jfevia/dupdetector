namespace DupDetector.Core.Internal;

/// <summary>
/// Argument guards used by the model so invalid settings cannot be represented.
/// </summary>
internal static class Require
{
    internal static int AtLeast(int value, int minimum, string name)
    {
        if (value < minimum)
        {
            throw new ArgumentOutOfRangeException(name, value, $"Value must be at least {minimum}.");
        }

        return value;
    }

    internal static double InRange(double value, double minimum, double maximum, string name)
    {
        if (double.IsNaN(value) || value < minimum || value > maximum)
        {
            throw new ArgumentOutOfRangeException(name, value, $"Value must be between {minimum} and {maximum}.");
        }

        return value;
    }

    internal static string NotBlank(string value, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value must not be blank.", name);
        }

        return value;
    }
}
