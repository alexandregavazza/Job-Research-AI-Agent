namespace JobResearchAgent.Matching;

/// <summary>
/// Interface for loading resume profiles, following Dependency Inversion Principle
/// </summary>
public interface IResumeLoader
{
    /// <summary>
    /// Loads the resume profile from configured sources
    /// </summary>
    /// <returns>The loaded resume profile</returns>
    ResumeProfile Load();
}
