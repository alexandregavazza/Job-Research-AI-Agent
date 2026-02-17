using JobResearchAgent.Infrastructure;
using JobResearchAgent.Models;
using Microsoft.Extensions.Options;
using System.IO;
using System.Linq;

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

        var safeCompany = SanitizeFileName(job.Company);
        var safeTitle = SanitizeFileName(job.Title);
        var timestamp = DateTime.UtcNow.ToString("HHmmss");
        var fileName = $"{safeCompany}_{safeTitle}_{timestamp}.png";

        return Path.Combine(screenshotFolder, fileName);
    }

    private static string SanitizeFileName(string value)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(value.Select(c => invalidChars.Contains(c) ? '_' : c).ToArray());
        return string.IsNullOrWhiteSpace(sanitized) ? "unknown" : sanitized;
    }

    private bool AskForApproval(JobPosting job)
    {
        Console.WriteLine($"Apply to {job.Company} - {job.Title}? (y/n)");
        return Console.ReadLine()?.ToLower() == "y";
    }
}