using JobResearchAgent.Models;

namespace JobResearchAgent.Tests;

public class ApplicationPolicyTests
{
    [Fact]
    public void Defaults_AreExpected()
    {
        var policy = new ApplicationPolicy();

        Assert.False(policy.Enabled);
        Assert.False(policy.RequireApproval);
        Assert.False(policy.AutoSubmit);
        Assert.Equal(0, policy.DelayBetweenApplicationsSeconds);
        Assert.Equal(string.Empty, policy.DocumentsBasePath);
        Assert.False(policy.ScreenshotOnFailure);
        Assert.Equal(string.Empty, policy.AllowedCompany);
    }

    [Fact]
    public void CanSetValues()
    {
        var policy = new ApplicationPolicy
        {
            Enabled = true,
            RequireApproval = true,
            AutoSubmit = true,
            DelayBetweenApplicationsSeconds = 30,
            DocumentsBasePath = "C:\\docs",
            ScreenshotOnFailure = true,
            AllowedCompany = "Contoso"
        };

        Assert.True(policy.Enabled);
        Assert.True(policy.RequireApproval);
        Assert.True(policy.AutoSubmit);
        Assert.Equal(30, policy.DelayBetweenApplicationsSeconds);
        Assert.Equal("C:\\docs", policy.DocumentsBasePath);
        Assert.True(policy.ScreenshotOnFailure);
        Assert.Equal("Contoso", policy.AllowedCompany);
    }
}
