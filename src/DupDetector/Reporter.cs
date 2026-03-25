using System.Text;
using System.Text.Json;

namespace DupDetector;

/// <summary>
/// Renders a DetectionReport as JSON or YAML.
/// </summary>
public class Reporter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public string Render(DetectionReport report, string format)
    {
        return format.ToLowerInvariant() switch
        {
            "yaml" => RenderYaml(report),
            _ => JsonSerializer.Serialize(report, JsonOptions)
        };
    }

    // ----------------------------------------------------------------
    // Simple recursive YAML serializer (no external library)
    // ----------------------------------------------------------------

    private static string RenderYaml(DetectionReport report)
    {
        var sb = new StringBuilder();
        AppendYamlObject(sb, report, 0);
        return sb.ToString();
    }

    private static void AppendYamlValue(StringBuilder sb, object? value, int indent)
    {
        switch (value)
        {
            case null:
                sb.AppendLine("null");
                break;
            case string s:
                sb.AppendLine(YamlEscapeString(s));
                break;
            case bool b:
                sb.AppendLine(b ? "true" : "false");
                break;
            case int or long or short or byte:
                sb.AppendLine(value?.ToString() ?? "null");
                break;
            case double d:
                sb.AppendLine(d.ToString("G"));
                break;
            case float f:
                sb.AppendLine(f.ToString("G"));
                break;
            case System.Collections.IEnumerable list:
                sb.AppendLine();
                foreach (var item in list)
                {
                    sb.Append(new string(' ', indent));
                    sb.Append("- ");
                    if (item is string || item is int || item is long || item is double || item is bool)
                    {
                        AppendYamlValue(sb, item, indent + 2);
                    }
                    else
                    {
                        sb.AppendLine();
                        AppendYamlObject(sb, item, indent + 2);
                    }
                }
                break;
            default:
                // Complex object
                sb.AppendLine();
                AppendYamlObject(sb, value, indent);
                break;
        }
    }

    private static void AppendYamlObject(StringBuilder sb, object obj, int indent)
    {
        var type = obj.GetType();
        var props = type.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        foreach (var prop in props)
        {
            var key = ToCamelCase(prop.Name);
            var val = prop.GetValue(obj);
            sb.Append(new string(' ', indent));
            sb.Append(key);
            sb.Append(": ");
            AppendYamlValue(sb, val, indent + 2);
        }
    }

    private static string YamlEscapeString(string s)
    {
        if (s.Length == 0) return "\"\"";
        // Use double-quoted style if string contains special chars
        if (s.Any(c => c == ':' || c == '#' || c == '\n' || c == '\r' || c == '"' || c == '\''))
        {
            var escaped = s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
            return $"\"{escaped}\"";
        }
        return s;
    }

    private static string ToCamelCase(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        return char.ToLowerInvariant(name[0]) + name[1..];
    }
}
