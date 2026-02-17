namespace JobResearchAgent.Models;
public class ApplicationPolicy
{
    public bool Enabled { get; init; }
    public bool RequireApproval { get; init; }
    public bool AutoSubmit { get; init; }
    public int DelayBetweenApplicationsSeconds { get; init; }
    public string DocumentsBasePath { get; init; } = "";
    public bool ScreenshotOnFailure { get; init; }
}
