using JobResearchAgent.Application;

namespace JobResearchAgent.Infrastructure.Automation;

public class LinkedInAutomation : IApplicationAutomation
{
    private readonly IBrowserAutomation _browser;

    public LinkedInAutomation(IBrowserAutomation browser)
    {
        _browser = browser;
    }

    public async Task<ApplicationResult> ApplyAsync(
        JobPosting job,
        string resumePath,
        string coverLetterPath,
        CancellationToken ct = default)
    {
        await _browser.InitializeAsync(ct);

        await _browser.NavigateAsync(job.Url, ct);

        await _browser.WaitForSelectorAsync("button.jobs-apply-button", ct);
        await _browser.ClickAsync("button.jobs-apply-button", ct);

        if (await _browser.ElementExistsAsync("input[type='file']", ct))
        {
            await _browser.UploadFileAsync("input[type='file']", resumePath, ct);
        }

        await _browser.ClickAsync("button[aria-label='Submit application']", ct);

        return ApplicationResult.CreateSuccess(job.ExternalJobId ?? "unknown");
    }
}