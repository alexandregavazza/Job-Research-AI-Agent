using JobResearchAgent.Models;

namespace JobResearchAgent.Tests;

public class JobPostingTests
{
    [Fact]
    public void CanSetRequiredProperties()
    {
        var posting = new JobPosting
        {
            Title = "Backend Engineer",
            Company = "Contoso",
            Location = "Remote",
            Url = "https://example.com",
            Description = "Backend role",
            Source = "test",
            MatchScore = 80
        };

        Assert.Equal("Backend Engineer", posting.Title);
        Assert.Equal("Contoso", posting.Company);
        Assert.Equal("Remote", posting.Location);
        Assert.Equal("https://example.com", posting.Url);
        Assert.Equal("Backend role", posting.Description);
        Assert.Equal("test", posting.Source);
        Assert.Equal(80, posting.MatchScore);
        Assert.Null(posting.ExternalJobId);
    }

    [Fact]
    public void CanSetDatesAndExternalId()
    {
        var collected = new DateTime(2026, 2, 18, 12, 0, 0, DateTimeKind.Utc);
        var created = new DateTime(2026, 2, 18, 13, 0, 0, DateTimeKind.Utc);

        var posting = new JobPosting
        {
            Title = "Backend Engineer",
            Company = "Contoso",
            Location = "Remote",
            Url = "https://example.com",
            Description = "Backend role",
            Source = "test",
            CollectedAt = collected,
            CreatedAt = created,
            ExternalJobId = "ext-1"
        };

        Assert.Equal(collected, posting.CollectedAt);
        Assert.Equal(created, posting.CreatedAt);
        Assert.Equal("ext-1", posting.ExternalJobId);
    }
}
