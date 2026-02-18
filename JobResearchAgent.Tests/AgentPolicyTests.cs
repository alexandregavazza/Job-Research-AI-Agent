using JobResearchAgent.Agents;

namespace JobResearchAgent.Tests;

public class AgentPolicyTests
{
    [Fact]
    public void Defaults_AreExpected()
    {
        var policy = new AgentPolicy();

        Assert.Equal(0, policy.MaxAgeHours);
        Assert.False(policy.RemoteOnly);
        Assert.False(policy.AllowHybrid);
        Assert.NotNull(policy.Countries);
        Assert.NotNull(policy.Keywords);
        Assert.NotNull(policy.Levels);
        Assert.Empty(policy.Countries);
        Assert.Empty(policy.Keywords);
        Assert.Empty(policy.Levels);
    }

    [Fact]
    public void CanSetValues()
    {
        var policy = new AgentPolicy
        {
            MaxAgeHours = 24,
            RemoteOnly = true,
            AllowHybrid = true,
            Countries = new List<string> { "Canada", "Brazil" },
            Keywords = new List<string> { "C#", "Azure" },
            Levels = new List<string> { "Senior" }
        };

        Assert.Equal(24, policy.MaxAgeHours);
        Assert.True(policy.RemoteOnly);
        Assert.True(policy.AllowHybrid);
        Assert.Equal(2, policy.Countries.Count);
        Assert.Equal(2, policy.Keywords.Count);
        Assert.Single(policy.Levels);
    }
}
