using JobResearchAgent.Infrastructure;
using JobResearchAgent.Models;
using Microsoft.Extensions.Options;

namespace JobResearchAgent.Application;
public class ApplicationAgent
{
    private readonly IApplicationAutomation _automation;
    private readonly IApplicationLogRepository _logRepository;
    private readonly ApplicationPolicy _policy;
    private readonly ILogger<ApplicationAgent> _logger;

    public ApplicationAgent(
        IApplicationAutomation automation,
        IApplicationLogRepository logRepository,
        IOptions<ApplicationPolicy> policy,
        ILogger<ApplicationAgent> logger)
    {
        _automation = automation;
        _logRepository = logRepository;
        _policy = policy.Value;
        _logger = logger;
    }

    public async Task ExecuteAsync(JobPosting job,
                                  string resumePath,
                                  string coverLetterPath,
                                  double matchScore,
                                  CancellationToken ct)
    {
        if (!_policy.Enabled)
            return;

        if (_policy.RequireApproval && !AskForApproval(job))
            return;

        var result = await _automation.ApplyAsync(job, resumePath, coverLetterPath, ct);

        var log = new ApplicationLog
        {
            ExternalJobId = job.ExternalJobId ?? "unknown",
            JobTitle = job.Title,
            Company = job.Company,
            Location = job.Location,
            Url = job.Url,
            Source = job.Source,
            ResumePath = resumePath,
            CoverLetterPath = coverLetterPath,
            MatchScore = matchScore,
            Status = result.Status
        };

        await _logRepository.InsertAsync(log, ct);

        await Task.Delay(TimeSpan.FromSeconds(_policy.DelayBetweenApplicationsSeconds), ct);
    }

    private bool AskForApproval(JobPosting job)
    {
        Console.WriteLine($"Apply to {job.Company} - {job.Title}? (y/n)");
        return Console.ReadLine()?.ToLower() == "y";
    }
}