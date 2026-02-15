using System.Text;
using Microsoft.Extensions.Logging;

namespace JobResearchAgent;

public class ResearchAgent
{
    private readonly IEnumerable<IJobSource> _sources;
    private readonly ILogger<ResearchAgent> _logger;

    public ResearchAgent(IEnumerable<IJobSource> sources, ILogger<ResearchAgent> logger)
    {
        _sources = sources;
        _logger = logger;
    }

    public async Task<List<JobPosting>> RunAsync()
    {
        _logger.LogInformation("Starting Job Research Agent...");

        var query = BuildSearchQuery();

        _logger.LogInformation("Search Query Built:");
        _logger.LogInformation(query);

        var allJobs = new List<JobPosting>();

        foreach (var source in _sources)
        {
            _logger.LogInformation($"Querying source: {source.GetType().Name}");

            var jobs = await source.SearchAsync(query);
            
            _logger.LogInformation($"Source {source.GetType().Name} returned {jobs.Count()} jobs");

            var filtered = ApplyHardFilters(jobs);
            
            _logger.LogInformation($"After filtering: {filtered.Count()} jobs from {source.GetType().Name}");

            allJobs.AddRange(filtered);
        }

        _logger.LogInformation($"Total jobs collected after filtering: {allJobs.Count}");

        return allJobs;
    }

    /// <summary>
    /// STEP 4 — This builds the query sent to LinkedIn / Indeed.
    /// This is where we inject keywords + seniority + remote intent.
    /// </summary>
    private string BuildSearchQuery()
    {
        var keywords = new[]
        {
            ".NET",
            "C#",
            "SQL",
            "AWS",
            "Azure",
            "Angular"
        };

        var seniority = new[]
        {
            "Senior",
            "Mid",
            "Lead",
            "Software Engineer",
            "Backend Engineer",
            "Full Stack"
        };

        var workType = new[]
        {
            "Remote",
            "Hybrid"
        };

        var sb = new StringBuilder();

        sb.Append("(");
        sb.Append(string.Join(" OR ", keywords));
        sb.Append(")");

        sb.Append(" AND (");
        sb.Append(string.Join(" OR ", seniority));
        sb.Append(")");

        sb.Append(" AND (");
        sb.Append(string.Join(" OR ", workType));
        sb.Append(")");

        return sb.ToString();
    }

    /// <summary>
    /// HARD FILTERS applied AFTER scraping.
    /// This guarantees we only keep exactly what you want,
    /// even if job boards return noisy data.
    /// </summary>
    private List<JobPosting> ApplyHardFilters(IEnumerable<JobPosting> jobs)
    {
        var jobList = jobs.ToList();
        
        _logger.LogInformation($"===== DEBUGGING: Received {jobList.Count()} total jobs before filtering =====");
        
        foreach (var job in jobList)
        {
            _logger.LogInformation($"Job: {job.Title} | Company: {job.Company} | Location: {job.Location} | Description length: {job.Description?.Length ?? 0}");
        }
        
        // FOR DEBUGGING: Return ALL jobs without filtering
        _logger.LogInformation($"===== RETURNING ALL {jobList.Count()} JOBS WITHOUT FILTERING =====");
        return jobList;
    }
}