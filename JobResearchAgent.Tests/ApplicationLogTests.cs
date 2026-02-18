using JobResearchAgent.Models;

namespace JobResearchAgent.Tests;

public class ApplicationLogTests
{
    [Fact]
    public void Defaults_AreExpected()
    {
        var before = DateTime.UtcNow;
        var log = new ApplicationLog();
        var after = DateTime.UtcNow;

        Assert.NotEqual(Guid.Empty, log.Id);
        Assert.Equal(string.Empty, log.ExternalJobId);
        Assert.Equal(string.Empty, log.JobTitle);
        Assert.Equal(string.Empty, log.Company);
        Assert.Equal(string.Empty, log.Location);
        Assert.Equal(string.Empty, log.Url);
        Assert.Equal(string.Empty, log.Source);
        Assert.Equal(string.Empty, log.ResumePath);
        Assert.Equal(string.Empty, log.CoverLetterPath);
        Assert.Equal(string.Empty, log.Status);
        Assert.Null(log.Notes);
        Assert.InRange(log.CreatedAt, before, after);
    }

    [Fact]
    public void CanSetValues()
    {
        var log = new ApplicationLog
        {
            ExternalJobId = "123",
            JobTitle = "Backend Engineer",
            Company = "Contoso",
            Location = "Remote",
            Url = "https://example.com",
            Source = "test",
            ResumePath = "C:\\resumes\\resume.pdf",
            CoverLetterPath = "C:\\letters\\cover.pdf",
            MatchScore = 90,
            Status = "submitted",
            Notes = "ok"
        };

        Assert.Equal("123", log.ExternalJobId);
        Assert.Equal("Backend Engineer", log.JobTitle);
        Assert.Equal("Contoso", log.Company);
        Assert.Equal("Remote", log.Location);
        Assert.Equal("https://example.com", log.Url);
        Assert.Equal("test", log.Source);
        Assert.Equal("C:\\resumes\\resume.pdf", log.ResumePath);
        Assert.Equal("C:\\letters\\cover.pdf", log.CoverLetterPath);
        Assert.Equal(90, log.MatchScore);
        Assert.Equal("submitted", log.Status);
        Assert.Equal("ok", log.Notes);
    }
}
