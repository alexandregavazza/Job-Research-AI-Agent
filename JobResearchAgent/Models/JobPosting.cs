public class JobPosting
{
    public required string Title { get; set; }
    public required string Company { get; set; }
    public required string Location { get; set; }
    public required string Url { get; set; }
    public required string Description { get; set; }
    public required string Source { get; set; }
    public DateTime CollectedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? ExternalJobId { get; set; }
    public double MatchScore { get; set; }
}
