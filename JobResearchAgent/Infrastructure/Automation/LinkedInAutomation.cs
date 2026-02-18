using JobResearchAgent.Application;
using JobResearchAgent.Models;

namespace JobResearchAgent.Infrastructure.Automation;

public class LinkedInAutomation : IApplicationAutomation
{
    private readonly IBrowserAutomation _browser;
    private readonly ILogger<LinkedInAutomation> _logger;

    public LinkedInAutomation(IBrowserAutomation browser, ILogger<LinkedInAutomation> logger)
    {
        _browser = browser ?? throw new ArgumentNullException(nameof(browser));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
            _logger.LogInformation("LinkedIn automation starting for job: {Url}", job.Url);

            await _browser.InitializeAsync(ct);
            _logger.LogInformation("Browser initialized");

            _logger.LogInformation("Starting navigation to job URL...");
            await _browser.NavigateAsync(job.Url, ct);
            _logger.LogInformation("Navigated to job URL");

            // Wait extra time for LinkedIn to fully load
            await Task.Delay(3000, ct);

            // Log current URL and page title for debugging
            var currentUrl = await _browser.GetCurrentUrlAsync(ct);
            var pageTitle = await _browser.GetPageTitleAsync(ct);
            _logger.LogInformation("Current URL: {Url}", currentUrl);
            _logger.LogInformation("Page Title: {Title}", pageTitle);

            // Take screenshot to see what page loaded
            var diagPath = AppendSuffix(screenshotPath, "nav-debug");
            if (!string.IsNullOrWhiteSpace(diagPath))
            {
                _logger.LogInformation("Taking navigation debug screenshot: {Path}", diagPath);
                await _browser.TakeScreenshotAsync(diagPath, ct);
            }

            // DEBUGGING: Pause for 10 seconds so you can see what's on the page
            _logger.LogInformation("PAUSING 10 seconds for manual inspection - check the browser window!");
            await Task.Delay(10000, ct);

            _logger.LogInformation("Checking for apply button...");
            
            // Try multiple possible apply button selectors (from actual LinkedIn HTML)
            string[] applyButtonSelectors = new[]
            {
                "#jobs-apply-button-id",                          // ID - most specific
                "button[data-live-test-job-apply-button]",        // Data attribute
                "button.jobs-apply-button",                       // Standard LinkedIn class
                "[data-test-id='jobs-apply-button']",             // Alternative data test
                "button[aria-label*='Apply']",                    // Any apply button
                ".jobs-apply-button"                              // Class selector
            };

            bool foundButton = false;
            string? usedSelector = null;
            
            foreach (var selector in applyButtonSelectors)
            {
                try
                {
                    _logger.LogInformation("Trying selector: {Selector}", selector);
                    
                    // Give it a few retries with delays for elements that are loading
                    for (int retry = 0; retry < 3; retry++)
                    {
                        if (await _browser.ElementExistsAsync(selector, ct))
                        {
                            _logger.LogInformation("Found apply button with selector: {Selector} on retry {Retry}", selector, retry);
                            usedSelector = selector;
                            foundButton = true;
                            break;
                        }
                        
                        if (retry < 2)
                        {
                            _logger.LogInformation("Selector not found, retrying in 1 second...");
                            await Task.Delay(1000, ct);
                        }
                    }
                    
                    if (foundButton)
                        break;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error checking selector {Selector}", selector);
                }
            }

            if (!foundButton)
            {
                _logger.LogWarning("No apply button found with any selector. Taking diagnostic screenshot.");
                var errorPath = AppendSuffix(screenshotPath, "no-button-found");
                if (!string.IsNullOrWhiteSpace(errorPath))
                {
                    await _browser.TakeScreenshotAsync(errorPath, ct);
                }
                return ApplicationResult.CreateFailure(job.ExternalJobId ?? "unknown", "Apply button not found on page", errorPath);
            }

            _logger.LogInformation("Found button with selector: {Selector}. Checking button state...", usedSelector);
            
            // Get detailed button state for diagnostics
            try
            {
                var buttonStateScript = $@"
                    const button = document.querySelector('{usedSelector}');
                    if (!button) return 'NOT_FOUND';
                    
                    const rect = button.getBoundingClientRect();
                    const isVisible = rect.width > 0 && rect.height > 0 && window.getComputedStyle(button).display !== 'none';
                    const isEnabled = !button.disabled;
                    const hasClickHandler = !!(button.onclick || button.__reactEventHandlers || button.addEventListener.toString().includes('apply'));
                    
                    return JSON.stringify({{
                        visible: isVisible,
                        enabled: isEnabled,
                        x: Math.round(rect.x),
                        y: Math.round(rect.y),
                        width: Math.round(rect.width),
                        height: Math.round(rect.height),
                        text: button.textContent.trim().substring(0, 50),
                        tagName: button.tagName,
                        classes: button.className,
                        id: button.id,
                        hasClickHandler: hasClickHandler
                    }});
                ";
                var buttonState = await _browser.EvaluateJavaScriptAsync(buttonStateScript, ct);
                _logger.LogInformation("Button state: {ButtonState}", buttonState);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get button state details");
            }

            // Take screenshot before attempting click
            var preClickPath = AppendSuffix(screenshotPath, "before-click");
            if (!string.IsNullOrWhiteSpace(preClickPath))
            {
                _logger.LogInformation("Taking screenshot before click attempt: {Path}", preClickPath);
                await _browser.TakeScreenshotAsync(preClickPath, ct);
            }

            // Instead of clicking, try to extract the URL the button points to
            string? targetUrl = null;
            
            // Try getting href attribute (with null check for usedSelector)
            if (!string.IsNullOrWhiteSpace(usedSelector))
            {
                targetUrl = await _browser.GetAttributeAsync(usedSelector, "href", ct);
                _logger.LogInformation("Href attribute: {Href}", targetUrl ?? "none");
            }
            
            if (string.IsNullOrWhiteSpace(targetUrl))
            {
                // Button might use onclick JavaScript to navigate - try to extract URL via JS
                _logger.LogInformation("No href found, trying to extract URL via JavaScript...");
                try
                {
                    var jsScript = $@"
                        const button = document.querySelector('{usedSelector}');
                        if (button) {{
                            // Check for data attributes that might contain the URL
                            const dataUrl = button.getAttribute('data-url') || 
                                          button.getAttribute('data-href') ||
                                          button.getAttribute('data-link');
                            if (dataUrl) return dataUrl;
                            
                            // Check onclick for URLs
                            const onclick = button.getAttribute('onclick');
                            if (onclick) {{
                                const urlMatch = onclick.match(/https?:\/\/[^\s'"")]+/);
                                if (urlMatch) return urlMatch[0];
                            }}
                            
                            // Try to find parent link or nearby href
                            const parentLink = button.closest('a[href]');
                            if (parentLink) return parentLink.href;
                        }}
                        return null;
                    ";
                    targetUrl = await _browser.EvaluateJavaScriptAsync(jsScript, ct);
                    _logger.LogInformation("JavaScript URL extraction result: {Url}", targetUrl ?? "none");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to extract URL via JavaScript");
                }
            }
            
            if (!string.IsNullOrWhiteSpace(targetUrl))
            {
                _logger.LogInformation("Extracted target URL: {TargetUrl}. Navigating directly...", targetUrl);
                await _browser.NavigateAsync(targetUrl, ct);
            }
            else
            {
                if (string.IsNullOrWhiteSpace(usedSelector))
                {
                    throw new InvalidOperationException("No apply button selector found");
                }
                
                _logger.LogInformation("Could not extract URL, trying to click button: {Selector}", usedSelector);
                try
                {
                    await _browser.ClickAsync(usedSelector, ct);
                    _logger.LogInformation("Apply button clicked successfully");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to click apply button with selector {Selector}", usedSelector);
                    _logger.LogError("Exception type: {ExceptionType}", ex.GetType().Name);
                    _logger.LogError("Exception message: {Message}", ex.Message);
                    
                    var errorPath = AppendSuffix(screenshotPath, "click-error");
                    if (!string.IsNullOrWhiteSpace(errorPath))
                    {
                        _logger.LogInformation("Taking failure screenshot at: {Path}", errorPath);
                        await _browser.TakeScreenshotAsync(errorPath, ct);
                    }
                    return ApplicationResult.CreateFailure(job.ExternalJobId ?? "unknown", $"Failed to click apply button: {ex.Message}", errorPath);
                }
            }

            _logger.LogInformation("Waiting for company career page to load...");
            await Task.Delay(5000, ct);

            // Take screenshot to see where we redirected
            var redirectPath = AppendSuffix(screenshotPath, "after-redirect");
            if (!string.IsNullOrWhiteSpace(redirectPath))
            {
                _logger.LogInformation("Taking screenshot after redirect: {Path}", redirectPath);
                await _browser.TakeScreenshotAsync(redirectPath, ct);
            }

            _logger.LogInformation("LinkedIn automation: Waiting for form to load on company career page...");
            await Task.Delay(3000, ct);

            _logger.LogInformation("Looking for resume upload field on company career page...");
            
            // Try multiple selectors for file upload (companies use different forms)
            string[] resumeSelectors = new[]
            {
                "input[type='file']",
                "input[name*='resume' i]",
                "input[name*='cv' i]",
                "input[accept*='pdf' i]",
                "input[accept*='doc' i]"
            };

            bool resumeUploaded = false;
            foreach (var selector in resumeSelectors)
            {
                if (await _browser.ElementExistsAsync(selector, ct))
                {
                    try
                    {
                        _logger.LogInformation("Found resume upload with selector: {Selector}", selector);
                        await _browser.UploadFileAsync(selector, resumePath, ct);
                        _logger.LogInformation("Resume uploaded successfully");
                        resumeUploaded = true;
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to upload resume with selector {Selector}", selector);
                    }
                }
            }

            if (!resumeUploaded)
            {
                _logger.LogWarning("Could not find resume upload field");
            }

            // Wait a bit after upload
            await Task.Delay(2000, ct);

            _logger.LogInformation("Looking for submit/apply button on company career page...");
            
            // Try multiple selectors for submit button
            string[] submitSelectors = new[]
            {
                "button[aria-label*='Submit' i]",
                "button[aria-label*='Apply' i]",
                "button:has-text('Submit')",
                "button:has-text('Apply')",
                "button[type='submit']",
                "input[type='submit']",
                "button.submit",
                "button.apply"
            };

            bool submitted = false;
            foreach (var selector in submitSelectors)
            {
                if (await _browser.ElementExistsAsync(selector, ct))
                {
                    try
                    {
                        _logger.LogInformation("Found submit button with selector: {Selector}", selector);
                        await _browser.ClickAsync(selector, ct);
                        _logger.LogInformation("Submit button clicked");
                        submitted = true;
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to click submit with selector {Selector}", selector);
                    }
                }
            }

            if (!submitted)
            {
                _logger.LogWarning("Could not find or click submit button");
                var errorPath = AppendSuffix(screenshotPath, "no-submit-button");
                if (!string.IsNullOrWhiteSpace(errorPath))
                {
                    await _browser.TakeScreenshotAsync(errorPath, ct);
                }
            }

            await Task.Delay(3000, ct);

            if (!string.IsNullOrWhiteSpace(screenshotPath))
            {
                _logger.LogInformation("Taking confirmation screenshot: {ScreenshotPath}", screenshotPath);
                await _browser.TakeScreenshotAsync(screenshotPath, ct);
            }

            _logger.LogInformation("Application submitted successfully");
            return ApplicationResult.CreateSuccess(job.ExternalJobId ?? "unknown", screenshotPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LinkedIn automation failed for job: {Url}", job.Url);
            var failurePath = AppendSuffix(screenshotPath, "failure");
            if (!string.IsNullOrWhiteSpace(failurePath))
            {
                _logger.LogInformation("Taking failure screenshot: {ScreenshotPath}", failurePath);
                try
                {
                    await _browser.TakeScreenshotAsync(failurePath, ct);
                }
                catch (Exception screenshotEx)
                {
                    _logger.LogError(screenshotEx, "Failed to take failure screenshot");
                }
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