using JobResearchAgent.Models;

namespace JobResearchAgent.Agents;

public interface IJobSource
{
    Task<IEnumerable<JobPosting>> SearchAsync(string keyword);
}
