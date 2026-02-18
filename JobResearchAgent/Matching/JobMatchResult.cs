using JobResearchAgent.Models;

namespace JobResearchAgent.Matching;

public class JobMatchResult
{
    public JobPosting Job { get; set; } = null!;
    public double Score { get; set; }
    public string Decision { get; set; } = "";
    public string Reason { get; set; } = "";
}
