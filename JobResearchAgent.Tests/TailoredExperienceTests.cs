using JobResearchAgent.Models;

namespace JobResearchAgent.Tests;

public class TailoredExperienceTests
{
    [Fact]
    public void Defaults_AreExpected()
    {
        var experience = new TailoredExperience();

        Assert.Equal(string.Empty, experience.Role);
        Assert.Equal(string.Empty, experience.Company);
        Assert.Equal(string.Empty, experience.StartDate);
        Assert.Equal(string.Empty, experience.EndDate);
        Assert.NotNull(experience.Highlights);
        Assert.Empty(experience.Highlights);
    }

    [Fact]
    public void CanSetValues()
    {
        var experience = new TailoredExperience
        {
            Role = "Backend Engineer",
            Company = "Contoso",
            StartDate = "2020",
            EndDate = "2024",
            Highlights = new List<string> { "Built APIs" }
        };

        Assert.Equal("Backend Engineer", experience.Role);
        Assert.Equal("Contoso", experience.Company);
        Assert.Equal("2020", experience.StartDate);
        Assert.Equal("2024", experience.EndDate);
        Assert.Single(experience.Highlights);
    }
}
