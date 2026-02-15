using Microsoft.Playwright;
using Microsoft.Extensions.Logging;

namespace JobResearchAgent;

public class LinkedInSource : IJobSource
{
    private readonly ILogger<LinkedInSource> _logger;
    private readonly AgentPolicy _policy;

    public LinkedInSource(ILogger<LinkedInSource> logger, AgentPolicy policy)
    {
        _logger = logger;
        _policy = policy;
    }

    public async Task<List<JobPosting>> SearchAsync(string _)
    {
        // Parameter intentionally ignored.
        // Source builds query from AgentPolicy (agent owns the mission).

        var jobs = new List<JobPosting>();

        using var playwright = await Playwright.CreateAsync();

        await using var browser = await playwright.Chromium.LaunchAsync(new()
        {
            Headless = true,
            SlowMo = 50 // helps avoid LinkedIn throttling
        });

        var query = BuildQuery();

        foreach (var country in _policy.Countries)
        {
            try
            {
                var context = await browser.NewContextAsync(new()
                {
                    UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64)"
                });

                var page = await context.NewPageAsync();

                var url = BuildLinkedInUrl(query, country);

                _logger.LogInformation("LinkedIn search → {Url}", url);

                await page.GotoAsync(url, new() { Timeout = 60000 });

                // Wait for job list to load
                await page.WaitForSelectorAsync(".jobs-search__results-list");

                // Scroll to force lazy loading
                for (int i = 0; i < 5; i++)
                {
                    await page.Mouse.WheelAsync(0, 2000);
                    await page.WaitForTimeoutAsync(1500);
                }

                var cards = await page.QuerySelectorAllAsync(".jobs-search__results-list li");

                foreach (var card in cards)
                {
                    try
                    {
                        var linkEl = await card.QuerySelectorAsync("a.base-card__full-link");
                        if (linkEl == null)
                            continue;

                        var jobUrl = await linkEl.GetAttributeAsync("href") ?? "";

                        var titleEl = await card.QuerySelectorAsync("h3");
                        var companyEl = await card.QuerySelectorAsync("h4");
                        var locationEl = await card.QuerySelectorAsync(".job-search-card__location");
                        var dateEl = await card.QuerySelectorAsync("time");
                        //var descriptionEl = await card.QuerySelectorAsync("jobs-description__container");

                        var title = titleEl != null ? await titleEl.InnerTextAsync() : "";
                        var company = companyEl != null ? await companyEl.InnerTextAsync() : "";
                        var location = locationEl != null ? await locationEl.InnerTextAsync() : "";
                        var dateText = dateEl != null ? await dateEl.GetAttributeAsync("datetime") : "";

                        //var descriptionText = descriptionEl != null ? await descriptionEl.InnerTextAsync() : "";
                        // Throttle requests: small delay to reduce LinkedIn rate-limiting
                        await Task.Delay(500);

                        // Open job details page (needed for description)
                        var detailPage = await context.NewPageAsync();
                        _logger.LogInformation("Opening LinkedIn detail page: {Url}", jobUrl);

                        var response = await detailPage.GotoAsync(jobUrl, new() { Timeout = 120000 });
                        _logger.LogInformation("Detail page response: {Status} {Url}", response?.Status, response?.Url);

                        // Skip pages that didn't return a successful 200 response
                        if (response == null || response.Status != 200)
                        {
                            _logger.LogWarning("Skipping LinkedIn detail page {Url} — HTTP {Status}", jobUrl, response?.Status);
                            await detailPage.CloseAsync();
                            continue;
                        }

                        try
                        {
                            await detailPage.WaitForSelectorAsync(".show-more-less-html__markup", new() { Timeout = 15000 });
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "WaitForSelector failed for LinkedIn detail page {Url}", jobUrl);
                            try
                            {
                                var content = await detailPage.ContentAsync();
                                var preview = content.Length > 5000 ? content.Substring(0, 5000) + "..." : content;
                                _logger.LogDebug("Detail page HTML preview for {Url}: {Html}", jobUrl, preview);
                            }
                            catch (Exception cex)
                            {
                                _logger.LogDebug(cex, "Failed to read detail page content for {Url}", jobUrl);
                            }
                        }

                        // Try primary selector then a couple of reasonable fallbacks
                        var descEl = await detailPage.QuerySelectorAsync(".show-more-less-html__markup")
                                     ?? await detailPage.QuerySelectorAsync(".description__text")
                                     ?? await detailPage.QuerySelectorAsync("div[class*='description']");

                        var description = descEl != null
                            ? await descEl.InnerTextAsync()
                            : "";

                        if (string.IsNullOrWhiteSpace(description))
                        {
                            try
                            {
                                var fallbackText = await detailPage.InnerTextAsync("body");
                                _logger.LogDebug("Fallback detail innerText length for {Url}: {Length}", jobUrl, fallbackText?.Length ?? 0);
                            }
                            catch (Exception fex)
                            {
                                _logger.LogDebug(fex, "Failed to read fallback innerText for {Url}", jobUrl);
                            }
                        }

                        await detailPage.CloseAsync();

                        jobs.Add(new JobPosting
                        {
                            Title = title.Trim(),
                            Company = company.Trim(),
                            Location = location.Trim(),
                            Description = description.Trim(),
                            Url = jobUrl,
                            Source = "LinkedIn",
                            CreatedAt = DateTime.UtcNow,
                            CollectedAt = ParseIsoDate(dateText)
                        });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed parsing LinkedIn job card.");
                    }
                }

                await context.CloseAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "LinkedIn search failed for {Country}", country);
            }
        }

        return jobs;
    }

    // -----------------------------
    // Build Boolean query from policy keywords
    // -----------------------------
    private string BuildQuery()
    {
        var keywordBlock = string.Join(" OR ",
            _policy.Keywords.Select(k => $"\"{k}\""));

        /*if (_policy.AllowHybrid || _policy.RemoteOnly)
        {
            keywordBlock += " OR \"remote\" OR \"hybrid\"";
        }*/

        return $"({keywordBlock})";
    }

    // -----------------------------
    // Translate policy → LinkedIn URL filters
    // -----------------------------
    private string BuildLinkedInUrl(string query, string country)
    {
        var encodedQuery = Uri.EscapeDataString(query);

        // LinkedIn uses seconds for freshness filter
        var seconds = _policy.MaxAgeHours * 3600;

        /*var remoteFilter = _policy.RemoteOnly
            ? "&f_WT=2"   // Remote only
            : _policy.AllowHybrid
                ? "&f_WT=2%2C3" // Remote + Hybrid
                : "";*/

        string result = $"https://www.linkedin.com/jobs/search/" +
               $"?keywords={encodedQuery}" +
               $"&location={Uri.EscapeDataString(country)}" +
               $"&f_TPR=r{seconds}" +  // last X hours
               $"&f_E=4"; // Experience level: Mid-Senior
               //$"{remoteFilter}";
        
        return result;
    }

    // -----------------------------
    // Convert LinkedIn ISO date → DateTime
    // -----------------------------
    private DateTime ParseIsoDate(string? isoDate)
    {
        if (string.IsNullOrWhiteSpace(isoDate))
            return DateTime.UtcNow;

        if (DateTime.TryParse(isoDate, out var parsed))
            return parsed.ToUniversalTime();

        return DateTime.UtcNow;
    }

    async Task<IEnumerable<JobPosting>> IJobSource.SearchAsync(string keyword)
    {
        var results = await SearchAsync(keyword);
        return results;
    }
}