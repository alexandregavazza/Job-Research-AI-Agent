using JobResearchAgent.Matching;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace JobResearchAgent;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly ResearchAgent _agent;
    private readonly JobRepository _repository;
    private readonly MatchingAgent _matchingAgent;

    public Worker(
        ILogger<Worker> logger,
        ResearchAgent agent,
        JobRepository repository,
        MatchingAgent matchingAgent)
    {
        _logger = logger;
        _agent = agent;
        _repository = repository;
        _matchingAgent = matchingAgent;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Job Research Pipeline started.");

        // 1️⃣ Initialize semantic matcher (embeds your resume once)
        await _matchingAgent.InitializeAsync();

        // 2️⃣ Run the research agent (scrapes LinkedIn + Indeed)
        var jobs = await _agent.RunAsync();

        _logger.LogInformation("Found {Count} jobs", jobs.Count);

        // 3️⃣ Save raw jobs first (data lake concept)
        await _repository.SaveAsync(jobs);

        // 4️⃣ Evaluate each job semantically
        foreach (var job in jobs)
        {
            var result = await _matchingAgent.EvaluateAsync(job);

            _logger.LogInformation(
                "JobId: {JobId} | Score: {Score:0}% | Decision: {Decision} | {Title}",
                job.ExternalJobId,
                result.Score,
                result.Decision,
                job.Title);
        }

        _logger.LogInformation("Pipeline finished.");

        // Stop the worker after one run (important!)
        Environment.Exit(0);
    }
}