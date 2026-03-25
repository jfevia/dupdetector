namespace DupDetector;

public class CodeInstance
{
    public string File { get; set; } = "";
    public int StartLine { get; set; }
    public int EndLine { get; set; }
    public string Method { get; set; } = "";
    public string Hash { get; set; } = "";
}
