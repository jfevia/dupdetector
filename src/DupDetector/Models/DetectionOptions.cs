namespace DupDetector;

public class DetectionOptions
{
    public int MinLines { get; set; } = 5;
    public double Similarity { get; set; } = 0.85;
    public string Format { get; set; } = "yaml";
    public List<string> Exclude { get; set; } = new();
    public bool IncludeGenerated { get; set; } = false;
    public DetectionKind DetectionKinds { get; set; } = DetectionKind.All;
    public List<string> InputPaths { get; set; } = new();
    public string OutputPath { get; set; } = "";
}
