using JobResearchAgent.Matching;
using JobResearchAgent.Models;

namespace JobResearchAgent.Tests;

public class JobMatchResultTests
{
    [Fact]
    public void Defaults_AreExpected()
    {
        var result = new JobMatchResult();

        Assert.Equal(0, result.Score);
        Assert.Equal(string.Empty, result.Decision);
        Assert.Equal(string.Empty, result.Reason);
        Assert.Null(result.Job);
    }

    [Fact]
    public void CanSetValues()
    {
        var job = new JobPosting
        {
            Title = "Backend Engineer",
            Company = "Contoso",
            Location = "Remote",
            Url = "https://example.com",
            Description = "Backend role",
            Source = "test",
            MatchScore = 85
        };

        var result = new JobMatchResult
        {
            Job = job,
            Score = 85,
            Decision = "APPLY",
            Reason = "Strong match"
        };

        Assert.Equal(job, result.Job);
        Assert.Equal(85, result.Score);
        Assert.Equal("APPLY", result.Decision);
        Assert.Equal("Strong match", result.Reason);
    }
}
