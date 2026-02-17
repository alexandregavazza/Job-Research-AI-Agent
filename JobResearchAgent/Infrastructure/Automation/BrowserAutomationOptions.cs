namespace JobResearchAgent.Infrastructure.Automation;

public class BrowserAutomationOptions
{
    public bool Headless { get; set; } = false;
    public int SlowMoMs { get; set; } = 0;
    public int TimeoutMs { get; set; } = 30000;
    public string UserDataDir { get; set; } = "playwright-profile";
    public ViewportOptions Viewport { get; set; } = new();
}

public class ViewportOptions
{
    public int Width { get; set; } = 1400;
    public int Height { get; set; } = 900;
}