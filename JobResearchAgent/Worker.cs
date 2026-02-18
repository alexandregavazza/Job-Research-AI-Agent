using JobResearchAgent.Matching;
using JobResearchAgent.Services;
using JobResearchAgent.Services.CoverLetter;
using JobResearchAgent.Infrastructure;
using JobResearchAgent.Agents;
using Microsoft.Extensions.Options;
using JobResearchAgent.Application;
using JobResearchAgent.Models;

namespace JobResearchAgent;

/// <summary>
/// Background worker following SOLID principles with dependency injection
/// </summary>
public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly PipelineRunner _runner;
    private readonly IHostApplicationLifetime _lifetime;

    public Worker(
        ILogger<Worker> logger,
        PipelineRunner runner,
        IHostApplicationLifetime lifetime)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _runner = runner ?? throw new ArgumentNullException(nameof(runner));
        _lifetime = lifetime ?? throw new ArgumentNullException(nameof(lifetime));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _runner.RunAsync(stoppingToken);
        _lifetime.StopApplication();
    }
}