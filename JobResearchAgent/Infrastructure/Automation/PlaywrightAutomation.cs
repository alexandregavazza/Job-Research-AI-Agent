namespace JobResearchAgent.Infrastructure.Automation;

using Microsoft.Extensions.Options;
using Microsoft.Playwright;

public sealed class PlaywrightAutomation : IBrowserAutomation
{
    private readonly BrowserAutomationOptions _options;

    private IPlaywright? _playwright;
    private IBrowserContext? _context;
    private IPage? _page;

    public PlaywrightAutomation(IOptions<BrowserAutomationOptions> options)
    {
        _options = options.Value;
    }

    public async Task InitializeAsync(CancellationToken ct)
    {
        _playwright = await Playwright.CreateAsync();

        _context = await _playwright.Chromium.LaunchPersistentContextAsync(
            _options.UserDataDir,
            new BrowserTypeLaunchPersistentContextOptions
            {
                Headless = _options.Headless,
                SlowMo = _options.SlowMoMs,
                ViewportSize = new ViewportSize
                {
                    Width = _options.Viewport.Width,
                    Height = _options.Viewport.Height
                }
            });

        _page = _context.Pages.FirstOrDefault() ?? await _context.NewPageAsync();

        _page.SetDefaultTimeout(_options.TimeoutMs);
    }

    public async Task NavigateAsync(string url, CancellationToken ct)
    {
        EnsureInitialized();
        
        try
        {
            // Try fast load first (just wait for DOM)
            await _page!.GotoAsync(url, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.DOMContentLoaded,
                Timeout = 30000 // 30 seconds
            });
        }
        catch (TimeoutException)
        {
            // If that times out, try with no wait at all - just navigate
            try
            {
                await _page!.GotoAsync(url, new PageGotoOptions
                {
                    WaitUntil = WaitUntilState.Commit,  // Just wait for navigation to commit
                    Timeout = 15000
                });
            }
            catch
            {
                // If even that fails, the page might already be loading - just continue
                // Page might be partially loaded which is often enough
            }
        }
        
        // Give it extra time for critical content to render
        await Task.Delay(5000);
    }

    public async Task ClickAsync(string selector, CancellationToken ct)
    {
        EnsureInitialized();
        
        // Wait for element to be visible and enabled before clicking
        await _page!.WaitForSelectorAsync(selector, new PageWaitForSelectorOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = _options.TimeoutMs
        });
        
        // Scroll into view if needed
        await _page.Locator(selector).ScrollIntoViewIfNeededAsync();
        
        // Wait a moment for any animations/overlays to finish
        await Task.Delay(1000);
        
        // Click with force (to bypass any overlays)
        await _page.ClickAsync(selector, new PageClickOptions
        {
            Force = true,
            Timeout = _options.TimeoutMs
        });
    }

    public async Task FillAsync(string selector, string value, CancellationToken ct)
    {
        EnsureInitialized();
        await _page!.FillAsync(selector, value);
    }

    public async Task UploadFileAsync(string selector, string filePath, CancellationToken ct)
    {
        EnsureInitialized();
        await _page!.SetInputFilesAsync(selector, filePath);
    }

    public async Task WaitForSelectorAsync(string selector, CancellationToken ct)
    {
        EnsureInitialized();
        await _page!.WaitForSelectorAsync(selector);
    }

    public async Task<bool> ElementExistsAsync(string selector, CancellationToken ct)
    {
        EnsureInitialized();
        var element = await _page!.QuerySelectorAsync(selector);
        return element != null;
    }

    public async Task TakeScreenshotAsync(string filePath, CancellationToken ct)
    {
        EnsureInitialized();

        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await _page!.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = filePath,
            FullPage = true
        });
    }

    public async Task<string> GetCurrentUrlAsync(CancellationToken ct)
    {
        EnsureInitialized();
        return _page!.Url;
    }

    public async Task<string> GetPageTitleAsync(CancellationToken ct)
    {
        EnsureInitialized();
        return await _page!.TitleAsync();
    }

    public async Task<string?> GetAttributeAsync(string selector, string attributeName, CancellationToken ct)
    {
        EnsureInitialized();
        var element = await _page!.QuerySelectorAsync(selector);
        if (element == null)
            return null;
        
        return await element.GetAttributeAsync(attributeName);
    }

    public async Task<string?> EvaluateJavaScriptAsync(string script, CancellationToken ct)
    {
        EnsureInitialized();
        var result = await _page!.EvaluateAsync<string>(script);
        return result;
    }

    private void EnsureInitialized()
    {
        if (_page == null)
            throw new InvalidOperationException("Browser not initialized. Call InitializeAsync first.");
    }

    public async ValueTask DisposeAsync()
    {
        if (_context != null)
            await _context.CloseAsync();

        _playwright?.Dispose();
    }
}