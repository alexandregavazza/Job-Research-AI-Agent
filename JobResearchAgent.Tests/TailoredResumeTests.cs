using JobResearchAgent.Models;

namespace JobResearchAgent.Tests;

public class TailoredResumeTests
{
    [Fact]
    public void Defaults_AreExpected()
    {
        var resume = new TailoredResume();

        Assert.Equal(string.Empty, resume.ProfessionalSummary);
        Assert.NotNull(resume.KeySkills);
        Assert.NotNull(resume.Experience);
        Assert.Empty(resume.KeySkills);
        Assert.Empty(resume.Experience);
    }

    [Fact]
    public void CanAddExperience()
    {
        var resume = new TailoredResume
        {
            ProfessionalSummary = "Senior engineer",
            KeySkills = new List<string> { "C#", "Azure" },
            Experience = new List<TailoredExperience>
            {
                new TailoredExperience
                {
                    Role = "Backend Engineer",
                    Company = "Contoso",
                    StartDate = "2020",
                    EndDate = "2024",
                    Highlights = new List<string> { "Built APIs" }
                }
            }
        };

        Assert.Equal("Senior engineer", resume.ProfessionalSummary);
        Assert.Equal(2, resume.KeySkills.Count);
        Assert.Single(resume.Experience);
        Assert.Equal("Backend Engineer", resume.Experience[0].Role);
    }
}
