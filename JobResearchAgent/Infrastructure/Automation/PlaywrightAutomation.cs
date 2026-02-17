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
        await _page!.GotoAsync(url, new PageGotoOptions
        {
            WaitUntil = WaitUntilState.NetworkIdle
        });
    }

    public async Task ClickAsync(string selector, CancellationToken ct)
    {
        EnsureInitialized();
        await _page!.ClickAsync(selector);
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