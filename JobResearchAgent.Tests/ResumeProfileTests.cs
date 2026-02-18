using JobResearchAgent.Matching;

namespace JobResearchAgent.Tests;

public class ResumeProfileTests
{
    [Fact]
    public void Defaults_AreEmptyStrings()
    {
        var profile = new ResumeProfile();

        Assert.Equal(string.Empty, profile.HumanText);
        Assert.Equal(string.Empty, profile.AiText);
    }

    [Fact]
    public void CanSetValues()
    {
        var profile = new ResumeProfile
        {
            HumanText = "Human summary",
            AiText = "AI summary"
        };

        Assert.Equal("Human summary", profile.HumanText);
        Assert.Equal("AI summary", profile.AiText);
    }
}
