using JobResearchAgent.Infrastructure;
using JobResearchAgent.Models;
using JobResearchAgent.Services.FileManipulator;
using Microsoft.Extensions.Options;

namespace JobResearchAgent.Application;
public class ApplicationAgent
{
    private readonly IApplicationAutomation _automation;
    private readonly IApplicationLogRepository _logRepository;
    private readonly ApplicationPolicy _policy;
    private readonly ILogger<ApplicationAgent> _logger;
    private readonly IFileSanitizer _fileSanitizer;

    public ApplicationAgent(
        IApplicationAutomation automation,
        IApplicationLogRepository logRepository,
        IOptions<ApplicationPolicy> policy,
        ILogger<ApplicationAgent> logger,
        IFileSanitizer fileSanitizer)
    {
        _automation = automation ?? throw new ArgumentNullException(nameof(automation));
        _logRepository = logRepository ?? throw new ArgumentNullException(nameof(logRepository));
        _policy = policy?.Value ?? throw new ArgumentNullException(nameof(policy));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _fileSanitizer = fileSanitizer ?? throw new ArgumentNullException(nameof(fileSanitizer));
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

        var screenshotPath = BuildScreenshotPath(job);
        var result = await _automation.ApplyAsync(job, resumePath, coverLetterPath, screenshotPath, ct);

        var notes = result.Error;
        if (!string.IsNullOrWhiteSpace(result.ScreenshotPath))
        {
            notes = string.IsNullOrWhiteSpace(notes)
                ? $"Confirmation screenshot: {result.ScreenshotPath}"
                : $"{notes} | Screenshot: {result.ScreenshotPath}";
        }

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
            Status = result.Status,
            Notes = notes
        };

        await _logRepository.InsertAsync(log, ct);

        await Task.Delay(TimeSpan.FromSeconds(_policy.DelayBetweenApplicationsSeconds), ct);
    }

    private string? BuildScreenshotPath(JobPosting job)
    {
        if (string.IsNullOrWhiteSpace(_policy.DocumentsBasePath))
        {
            return null;
        }

        var dateFolder = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var screenshotFolder = Path.Combine(_policy.DocumentsBasePath, dateFolder, "Screenshot");
        Directory.CreateDirectory(screenshotFolder);

        var safeCompany = _fileSanitizer.Sanitize(job.Company);
        var safeTitle = _fileSanitizer.Sanitize(job.Title);
        var timestamp = DateTime.UtcNow.ToString("HHmmss");
        var fileName = $"{safeCompany}_{safeTitle}_{timestamp}.png";

        return Path.Combine(screenshotFolder, fileName);
    }

    private bool AskForApproval(JobPosting job)
    {
        Console.WriteLine($"Apply to {job.Company} - {job.Title}? (y/n)");
        return Console.ReadLine()?.ToLower() == "y";
    }
}