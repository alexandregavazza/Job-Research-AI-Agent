public class JobPosting
{
    public string Title { get; set; }
    public string Company { get; set; }
    public string Location { get; set; }
    public string Url { get; set; }
    public string Description { get; set; }
    public string Source { get; set; }
    public DateTime CollectedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? ExternalJobId { get; set; }
    public double MatchScore { get; set; }
}
