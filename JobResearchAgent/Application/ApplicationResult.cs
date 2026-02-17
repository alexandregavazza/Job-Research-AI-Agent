namespace JobResearchAgent.Application;

public class ApplicationResult
{
    public bool Success { get; set; }
    public string Status { get; set; } = "";
    public string? ExternalApplicationId { get; set; }
    public string? Error { get; set; }

    public static ApplicationResult CreateSuccess(string? externalId)
    {
        return new ApplicationResult
        {
            Success = true,
            Status = "submitted",
            ExternalApplicationId = externalId
        };
    }

    public static ApplicationResult CreateFailure(string? externalId, string error)
    {
        return new ApplicationResult
        {
            Success = false,
            Status = "failed",
            ExternalApplicationId = externalId,
            Error = error
        };
    }
}
