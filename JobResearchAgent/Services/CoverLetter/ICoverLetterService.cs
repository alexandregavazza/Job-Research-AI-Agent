using JobResearchAgent.Models;

public interface ICoverLetterService
{
    Task<GeneratedCoverLetter> GenerateAsync(
        JobPosting job,
        TailoredResume resume,
        CancellationToken ct = default);
}