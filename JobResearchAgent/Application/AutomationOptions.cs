namespace JobResearchAgent.Application;
public class AutomationOptions
{
    public bool Headless { get; set; } = false;
    public int SlowMoMs { get; set; } = 150;
    public int TimeoutMs { get; set; } = 30000;
}