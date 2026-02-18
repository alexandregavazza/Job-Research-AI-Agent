using JobResearchAgent.Models;

namespace JobResearchAgent.Tests;

public class GeneratedCoverLetterTests
{
    [Fact]
    public void GeneratedAt_DefaultsToCurrentTime()
    {
        var before = DateTime.UtcNow;
        var coverLetter = new GeneratedCoverLetter();
        var after = DateTime.UtcNow;

        Assert.InRange(coverLetter.GeneratedAt, before, after);
    }

    [Fact]
    public void CanSetValues()
    {
        var coverLetter = new GeneratedCoverLetter
        {
            JobId = "job-1",
            Company = "Contoso",
            Title = "Backend Engineer",
            Content = "Body"
        };

        Assert.Equal("job-1", coverLetter.JobId);
        Assert.Equal("Contoso", coverLetter.Company);
        Assert.Equal("Backend Engineer", coverLetter.Title);
        Assert.Equal("Body", coverLetter.Content);
    }
}
