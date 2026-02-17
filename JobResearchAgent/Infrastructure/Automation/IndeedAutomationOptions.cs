namespace JobResearchAgent.Infrastructure.Automation;
public class IndeedAutomationOptions
{
    public string ApplyButtonSelector { get; set; } = "";
    public string ResumeUploadSelector { get; set; } = "";
    public string? CoverLetterUploadSelector { get; set; }
    public string SubmitButtonSelector { get; set; } = "";
    public string SuccessIndicatorSelector { get; set; } = "";

    public List<IndeedFieldMapping> AdditionalFields { get; set; } = new();
}

public class IndeedFieldMapping
{
    public string Selector { get; set; } = "";
    public string Value { get; set; } = "";
}