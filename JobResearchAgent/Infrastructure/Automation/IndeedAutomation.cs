using JobResearchAgent.Application;
using Microsoft.Extensions.Options;
using System.IO;

namespace JobResearchAgent.Infrastructure.Automation;

public class IndeedAutomation : IApplicationAutomation
{
    private readonly IBrowserAutomation _browser;
    private readonly IndeedAutomationOptions _options;

    public IndeedAutomation(
        IBrowserAutomation browser,
        IOptions<IndeedAutomationOptions> options)
    {
        _browser = browser;
        _options = options.Value;
    }

    public async Task<ApplicationResult> ApplyAsync(
        JobPosting job,
        string resumePath,
        string coverLetterPath,
        string? screenshotPath,
        CancellationToken ct = default)
    {
        try
        {
            await _browser.InitializeAsync(ct);

            // Navigate to job page
            await _browser.NavigateAsync(job.Url, ct);

            // Wait for Apply button
            await _browser.WaitForSelectorAsync(_options.ApplyButtonSelector, ct);

            // Click Apply
            await _browser.ClickAsync(_options.ApplyButtonSelector, ct);

            // Wait for application modal/page
            await _browser.WaitForSelectorAsync(_options.ResumeUploadSelector, ct);

            // Upload Resume
            if (!string.IsNullOrWhiteSpace(resumePath))
            {
                await _browser.UploadFileAsync(_options.ResumeUploadSelector, resumePath, ct);
            }

            // Upload Cover Letter (if the flow supports it)
            if (!string.IsNullOrWhiteSpace(coverLetterPath) &&
                !string.IsNullOrWhiteSpace(_options.CoverLetterUploadSelector))
            {
                if (await _browser.ElementExistsAsync(_options.CoverLetterUploadSelector, ct))
                {
                    await _browser.UploadFileAsync(_options.CoverLetterUploadSelector, coverLetterPath, ct);
                }
            }

            // Fill additional fields if configured
            foreach (var field in _options.AdditionalFields)
            {
                if (await _browser.ElementExistsAsync(field.Selector, ct))
                {
                    await _browser.FillAsync(field.Selector, field.Value, ct);
                }
            }

            // Submit application
            await _browser.ClickAsync(_options.SubmitButtonSelector, ct);

            // Wait for confirmation
            await _browser.WaitForSelectorAsync(_options.SuccessIndicatorSelector, ct);

            if (!string.IsNullOrWhiteSpace(screenshotPath))
            {
                await _browser.TakeScreenshotAsync(screenshotPath, ct);
            }

            return ApplicationResult.CreateSuccess(job.ExternalJobId ?? "unknown", screenshotPath);
        }
        catch (Exception ex)
        {
            var failurePath = AppendSuffix(screenshotPath, "failure");
            if (!string.IsNullOrWhiteSpace(failurePath))
            {
                await _browser.TakeScreenshotAsync(failurePath, ct);
            }

            return ApplicationResult.CreateFailure(job.ExternalJobId ?? "unknown", ex.Message, failurePath);
        }
    }

    private static string? AppendSuffix(string? path, string suffix)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        var directory = Path.GetDirectoryName(path);
        var name = Path.GetFileNameWithoutExtension(path);
        var extension = Path.GetExtension(path);

        if (string.IsNullOrWhiteSpace(name))
        {
            return path;
        }

        var fileName = $"{name}_{suffix}{extension}";
        return string.IsNullOrWhiteSpace(directory)
            ? fileName
            : Path.Combine(directory, fileName);
    }
}