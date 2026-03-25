namespace DupDetector;

public class DetectionOptions
{
    public int MinLines { get; set; } = 5;
    public double Similarity { get; set; } = 0.85;
    public string Format { get; set; } = "json";
    public List<string> Exclude { get; set; } = new();
    public bool IncludeGenerated { get; set; } = false;
    public string InputPath { get; set; } = "";
}
