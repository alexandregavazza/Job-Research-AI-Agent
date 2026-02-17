namespace JobResearchAgent.Application;

public interface IApplicationAutomation
{
    Task<ApplicationResult> ApplyAsync(
        JobPosting job,
        string resumePath,
        string coverLetterPath,
        CancellationToken ct = default);
}