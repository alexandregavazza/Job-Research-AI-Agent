using JobResearchAgent.Agents;

namespace JobResearchAgent.Tests;

public class AgentPolicyTests
{
    [Fact]
    public void Defaults_AreExpected()
    {
        var policy = new AgentPolicy();

        Assert.Equal(0, policy.SearchJobsInTheLast);
        Assert.False(policy.RemoteOnly);
        Assert.False(policy.AllowHybrid);
        Assert.NotNull(policy.CountriesTargeted);
        Assert.NotNull(policy.Keywords);
        Assert.NotNull(policy.Levels);
        Assert.Empty(policy.CountriesTargeted);
        Assert.Empty(policy.Keywords);
        Assert.Empty(policy.Levels);
    }

    [Fact]
    public void CanSetValues()
    {
        var policy = new AgentPolicy
        {
            SearchJobsInTheLast = 24,
            RemoteOnly = true,
            AllowHybrid = true,
            CountriesTargeted = new List<string> { "Canada", "Brazil" },
            Keywords = new List<string> { "C#", "Azure" },
            Levels = new List<string> { "Senior" }
        };

        Assert.Equal(24, policy.SearchJobsInTheLast);
        Assert.True(policy.RemoteOnly);
        Assert.True(policy.AllowHybrid);
        Assert.Equal(2, policy.CountriesTargeted.Count);
        Assert.Equal(2, policy.Keywords.Count);
        Assert.Single(policy.Levels);
    }
}
