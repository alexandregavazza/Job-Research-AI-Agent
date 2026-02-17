namespace JobResearchAgent.Models;
public class ApplicationLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string ExternalJobId { get; set; } = "";
    public string JobTitle { get; set; } = "";
    public string Company { get; set; } = "";
    public string Location { get; set; } = "";
    public string Url { get; set; } = "";
    public string Source { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string ResumePath { get; set; } = "";
    public string CoverLetterPath { get; set; } = "";
    public double MatchScore { get; set; }
    public string Status { get; set; } = "";
    public string? Notes { get; set; }
}
