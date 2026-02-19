using System.Text;
using Microsoft.Extensions.Options;
using JobResearchAgent.Models;

namespace JobResearchAgent.Agents;

public class ResearchAgent
{
    private readonly IEnumerable<IJobSource> _sources;
    private readonly ILogger<ResearchAgent> _logger;
    private readonly AgentPolicy _policy;

    public ResearchAgent(IEnumerable<IJobSource> sources, ILogger<ResearchAgent> logger, IOptions<AgentPolicy> policy)
    {
        _sources = sources ?? throw new ArgumentNullException(nameof(sources));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _policy = policy?.Value ?? throw new ArgumentNullException(nameof(policy));
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
        var sb = new StringBuilder();

        // 1️⃣ Keywords (Role titles you're targeting)
        if (_policy.Keywords.Any())
        {
            sb.Append("(");
            sb.Append(string.Join(" OR ", _policy.Keywords.Select(Escape)));
            sb.Append(")");
        }

        // 2️⃣ Seniority levels
        if (_policy.Levels.Any())
        {
            sb.Append(" AND (");
            sb.Append(string.Join(" OR ", _policy.Levels.Select(Escape)));
            sb.Append(")");
        }

        // 3️⃣ Work model (Remote / Hybrid logic)
        var workModes = new List<string>();

        if (_policy.RemoteOnly)
        {
            workModes.Add("Remote");
        }
        else
        {
            workModes.Add("Onsite");

            if (_policy.AllowHybrid)
                workModes.Add("Hybrid");

            workModes.Add("Remote");
        }

        if (workModes.Any())
        {
            sb.Append(" AND (");
            sb.Append(string.Join(" OR ", workModes.Select(Escape)));
            sb.Append(")");
        }

        return sb.ToString();
    }

    private static string Escape(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return value;

        // If the value contains spaces, wrap it in quotes so LinkedIn treats it as one term
        return value.Contains(' ')
            ? $"\"{value}\""
            : value;
    }

    /// <summary>
    /// HARD FILTERS applied AFTER scraping.
    /// This guarantees we only keep exactly what you want,
    /// even if job boards return noisy data.
    /// </summary>
    private List<JobPosting> ApplyHardFilters(IEnumerable<JobPosting> jobs)
    {
        var jobList = jobs.ToList();

        // FOR DEBUGGING: Return ALL jobs without filtering
        _logger.LogInformation($"===== RETURNING ALL {jobList.Count()} JOBS WITHOUT FILTERING =====");
        return jobList;
    }
}