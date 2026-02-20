namespace JobResearchAgent.Services;

public class LanguageDetectionOptions
{
    public List<string> PortugueseIndicators { get; set; } = new();
    public int MinimumIndicatorMatches { get; set; } = 3;
}
