using JobResearchAgent.Models;

namespace JobResearchAgent.Application;

public interface IApplicationAutomation
{
    Task<ApplicationResult> ApplyAsync(
        JobPosting job,
        string resumePath,
        string coverLetterPath,
        string? screenshotPath,
        CancellationToken ct = default);
}