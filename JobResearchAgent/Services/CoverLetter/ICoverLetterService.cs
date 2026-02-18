using JobResearchAgent.Models;

namespace JobResearchAgent.Services.CoverLetter;

public interface ICoverLetterService
{
    Task<GeneratedCoverLetter> GenerateAsync(
        JobPosting job,
        TailoredResume resume,
        CancellationToken ct = default);
}