using System.Text;

namespace DupDetector.Sources;

/// <summary>
/// Decodes a source file, detecting UTF-16 even when no byte-order mark is present.
/// </summary>
// UTF-16 read as UTF-8 yields NUL-interleaved text that parses into garbage, so the pattern is sniffed.
public static class SourceDecoder
{
    /// <summary>Number of leading bytes examined when sniffing an unmarked file.</summary>
    private const int SampleBytes = 512;

    public static Encoding Detect(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
        {
            return Encoding.UTF8;
        }

        if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
        {
            return Encoding.Unicode;
        }

        if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
        {
            return Encoding.BigEndianUnicode;
        }

        return SniffUnmarked(bytes);
    }

    public static string Decode(byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);

        var encoding = Detect(bytes);
        // GetString keeps a byte-order mark as a leading zero-width character; strip it.
        return encoding.GetString(bytes).TrimStart('\uFEFF');
    }

    /// <summary>
    /// Guesses UTF-16 from the NUL padding that ASCII-range characters produce, choosing the
    /// endianness by which half of each code unit is zero.
    /// </summary>
    private static Encoding SniffUnmarked(ReadOnlySpan<byte> bytes)
    {
        var limit = Math.Min(bytes.Length, SampleBytes);
        if (limit < 4)
        {
            return Encoding.UTF8;
        }

        var evenNulls = 0;
        var oddNulls = 0;
        for (var index = 0; index < limit; index++)
        {
            if (bytes[index] != 0)
            {
                continue;
            }

            if (index % 2 == 0)
            {
                evenNulls++;
            }
            else
            {
                oddNulls++;
            }
        }

        var threshold = limit / 4;
        if (oddNulls > threshold && oddNulls > evenNulls)
        {
            return Encoding.Unicode;
        }

        return evenNulls > threshold && evenNulls > oddNulls ? Encoding.BigEndianUnicode : Encoding.UTF8;
    }
}
