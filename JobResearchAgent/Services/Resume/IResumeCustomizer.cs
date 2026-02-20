using JobResearchAgent.Models;

namespace JobResearchAgent.Services.Resume;

public interface IResumeCustomizer
{
    Task<TailoredResume> CustomizeAsync(
        string baseResume,
        string jobTitle,
        string jobDescription);
}
