namespace JobResearchAgent;

public class AgentPolicy
{
    public string[] Countries { get; init; } =
    {
        "Canada",
        "United States",
        "Brazil",
        "Singapore",
        "United Arab Emirates"
    };

    public string[] Keywords { get; init; } =
    {
        "Full Stack Developer", "C# Developer", "Software Engineer", "Solutions Architect", "Software Development Engineer", "Backend Developer", "Frontend Developer", "Cloud Engineer", "DevOps Engineer" 
    };

    public string[] Levels { get; init; } =
    {
        "Mid", "Senior", "Lead", "Staff"
    };

    public int MaxAgeHours { get; init; } = 24;
    public bool RemoteOnly { get; init; } = false;
    public bool AllowHybrid { get; init; } = true;
}
