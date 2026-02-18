using JobResearchAgent.Application;

namespace JobResearchAgent.Tests;

public class AutomationOptionsTests
{
    [Fact]
    public void Defaults_AreExpected()
    {
        var options = new AutomationOptions();

        Assert.False(options.Headless);
        Assert.Equal(150, options.SlowMoMs);
        Assert.Equal(30000, options.TimeoutMs);
    }

    [Fact]
    public void CanSetValues()
    {
        var options = new AutomationOptions
        {
            Headless = true,
            SlowMoMs = 50,
            TimeoutMs = 10000
        };

        Assert.True(options.Headless);
        Assert.Equal(50, options.SlowMoMs);
        Assert.Equal(10000, options.TimeoutMs);
    }
}
