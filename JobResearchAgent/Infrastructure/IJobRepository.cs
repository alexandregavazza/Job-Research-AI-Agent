using JobResearchAgent.Models;

namespace JobResearchAgent.Infrastructure;

/// <summary>
/// Interface for job data persistence, following Dependency Inversion Principle
/// </summary>
public interface IJobRepository
{
    /// <summary>
    /// Saves a collection of job postings
    /// </summary>
    Task SaveAsync(IEnumerable<JobPosting> jobs);

    /// <summary>
    /// Saves a tailored resume for a specific job
    /// </summary>
    Task SaveTailoredResumeAsync(JobPosting job, TailoredResume tailored);
}
