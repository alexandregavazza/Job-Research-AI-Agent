using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace JobResearchAgent;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly ResearchAgent _agent;
    private readonly JobRepository _repository;

    public Worker(
        ILogger<Worker> logger,
        ResearchAgent agent,
        JobRepository repository)
    {
        _logger = logger;
        _agent = agent;
        _repository = repository;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Research Agent started at: {time}", DateTimeOffset.Now);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("Running job research...");

                var jobs = await _agent.RunAsync();

                await _repository.SaveAsync(jobs);

                _logger.LogInformation("Collected {count} jobs", jobs.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during research execution");
            }

            // Run every 24 hours (change for testing!)
            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);

            // For testing use:
            // await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }
}