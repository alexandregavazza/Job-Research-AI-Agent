namespace JobResearchAgent.Application;

public class ApplicationResult
{
    public bool Success { get; set; }
    public string Status { get; set; } = "";
    public string? ExternalApplicationId { get; set; }
    public string? Error { get; set; }
    public string? ScreenshotPath { get; set; }

    public static ApplicationResult CreateSuccess(string? externalId, string? screenshotPath = null)
    {
        return new ApplicationResult
        {
            Success = true,
            Status = "submitted",
            ExternalApplicationId = externalId,
            ScreenshotPath = screenshotPath
        };
    }

    public static ApplicationResult CreateFailure(string? externalId, string error, string? screenshotPath = null)
    {
        return new ApplicationResult
        {
            Success = false,
            Status = "failed",
            ExternalApplicationId = externalId,
            Error = error,
            ScreenshotPath = screenshotPath
        };
    }
}
