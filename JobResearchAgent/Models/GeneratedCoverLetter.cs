namespace JobResearchAgent.Models;

public class GeneratedCoverLetter
{
    public string JobId { get; set; } = default!;
    public string Company { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string Content { get; set; } = default!;
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}
