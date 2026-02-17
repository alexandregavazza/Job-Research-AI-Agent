using Microsoft.Playwright;

namespace JobResearchAgent.Agents;

public class IndeedSource : IJobSource
{
    private readonly ILogger<IndeedSource> _logger;
    private readonly AgentPolicy _policy;

    public IndeedSource(ILogger<IndeedSource> logger, AgentPolicy policy)
    {
        _logger = logger;
        _policy = policy;
    }

    public async Task<List<JobPosting>> SearchAsync(string _)
    {
        // The parameter is ignored intentionally.
        // The source builds the query from AgentPolicy (correct agent design).

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

                var url = BuildIndeedUrl(query, country);

                _logger.LogInformation("Indeed search → {Url}", url);

                await page.GotoAsync(url, new() { Timeout = 60000 });

                await page.WaitForSelectorAsync("[data-testid='jobTitle']");

                // Scroll to force lazy loading
                for (int i = 0; i < 5; i++)
                {
                    await page.Mouse.WheelAsync(0, 2000);
                    await page.WaitForTimeoutAsync(1500);
                }
				
                var cards = await page.QuerySelectorAllAsync(".job_seen_beacon");

                foreach (var card in cards)
                {
                    try
                    {
                        var titleEl = await card.QuerySelectorAsync("[data-testid='jobTitle']");
                        var companyEl = await card.QuerySelectorAsync("[data-testid='company-name']");
                        var locationEl = await card.QuerySelectorAsync("[data-testid='text-location']");
                        var dateEl = await card.QuerySelectorAsync("span.date");

                        if (titleEl == null)
                            continue;

                        var title = (await titleEl.InnerTextAsync()) ?? "";
                        var company = companyEl != null ? await companyEl.InnerTextAsync() : "";
                        var location = locationEl != null ? await locationEl.InnerTextAsync() : "";
                        var dateText = dateEl != null ? await dateEl.InnerTextAsync() : "";

                        var href = await titleEl.GetAttributeAsync("href");
                        if (string.IsNullOrWhiteSpace(href))
                            continue;

                        var jobUrl = $"https://www.indeed.com{href}";

                        // Open details page to extract description
                        var detailPage = await context.NewPageAsync();
                        await detailPage.GotoAsync(jobUrl);
                        await detailPage.WaitForSelectorAsync("#jobDescriptionText");

                        var descEl = await detailPage.QuerySelectorAsync("#jobDescriptionText");
                        var description = descEl != null
                            ? await descEl.InnerTextAsync()
                            : "";

                        await detailPage.CloseAsync();

                        jobs.Add(new JobPosting
                        {
                            Title = title.Trim(),
                            Company = company.Trim(),
                            Location = location.Trim(),
                            Description = description.Trim(),
                            Url = jobUrl,
                            Source = "Indeed",
                            CreatedAt = DateTime.UtcNow,
                            CollectedAt = ParseRelativeDate(dateText)
                        });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed parsing Indeed job card.");
                    }
                }

                await context.CloseAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Indeed search failed for {Country}", country);
            }
        }

        return jobs;
    }

    // -----------------------------
    // Query built strictly from policy
    // -----------------------------
    private string BuildQuery()
    {
        // (" .NET " OR "C#" OR "SQL" ...)
        var keywordBlock = string.Join(" OR ",
            _policy.Keywords.Select(k => $"\"{k}\""));

        // Bias toward remote/hybrid if allowed
        /*if (_policy.AllowHybrid || _policy.RemoteOnly)
        {
            keywordBlock += " OR \"remote\" OR \"hybrid\"";
        }*/

        return $"({keywordBlock})";
    }

    // -----------------------------
    // Convert policy → Indeed URL parameters
    // -----------------------------
    private string BuildIndeedUrl(string query, string country)
    {
        var encodedQuery = Uri.EscapeDataString(query);

        // Indeed uses "fromage" in days, not hours
        var days = Math.Max(1, _policy.MaxAgeHours / 24);

        return $"https://www.indeed.com/jobs" +
               $"?q={encodedQuery}" +
               $"&l={Uri.EscapeDataString(country)}" +
               $"&fromage={days}" +
               $"&sort=date";
    }

    // -----------------------------
    // Convert "Posted X days ago" → DateTime
    // -----------------------------
    private DateTime ParseRelativeDate(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return DateTime.UtcNow;

        text = text.ToLowerInvariant();

        if (text.Contains("today") || text.Contains("just posted"))
            return DateTime.UtcNow;

        var match = System.Text.RegularExpressions.Regex.Match(text, @"(\d+)");
        if (match.Success && int.TryParse(match.Value, out int days))
        {
            return DateTime.UtcNow.AddDays(-days);
        }

        return DateTime.UtcNow;
    }

    async Task<IEnumerable<JobPosting>> IJobSource.SearchAsync(string keyword)
    {
        var results = await SearchAsync(keyword);
        return results;
    }
}