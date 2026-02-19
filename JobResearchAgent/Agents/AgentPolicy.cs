namespace JobResearchAgent.Agents;

public class AgentPolicy
{
    public int SearchJobsInTheLast { get; init; }
    public bool RemoteOnly { get; init; }
    public bool AllowHybrid { get; init; }
    public List<string> CountriesTargeted { get; set; } = new();
    public List<string> Keywords { get; set; } = new();
    public List<string> Levels { get; set; } = new();
}
