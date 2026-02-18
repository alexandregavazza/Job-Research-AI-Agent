using JobResearchAgent.Application;

namespace JobResearchAgent.Tests;

public class ApplicationResultTests
{
    [Fact]
    public void CreateSuccess_SetsExpectedValues()
    {
        var result = ApplicationResult.CreateSuccess("ext-1", "C:\\shots\\ok.png");

        Assert.True(result.Success);
        Assert.Equal("submitted", result.Status);
        Assert.Equal("ext-1", result.ExternalApplicationId);
        Assert.Equal("C:\\shots\\ok.png", result.ScreenshotPath);
        Assert.Null(result.Error);
    }

    [Fact]
    public void CreateFailure_SetsExpectedValues()
    {
        var result = ApplicationResult.CreateFailure("ext-2", "failed", "C:\\shots\\fail.png");

        Assert.False(result.Success);
        Assert.Equal("failed", result.Status);
        Assert.Equal("ext-2", result.ExternalApplicationId);
        Assert.Equal("failed", result.Error);
        Assert.Equal("C:\\shots\\fail.png", result.ScreenshotPath);
    }
}
