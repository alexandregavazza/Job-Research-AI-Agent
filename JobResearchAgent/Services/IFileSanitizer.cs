namespace JobResearchAgent.Services;

/// <summary>
/// Interface for sanitizing file names, following Dependency Inversion Principle
/// </summary>
public interface IFileSanitizer
{
    /// <summary>
    /// Sanitizes a string to be safe for use as a filename
    /// </summary>
    /// <param name="input">The input string to sanitize</param>
    /// <returns>A sanitized filename-safe string</returns>
    string Sanitize(string input);
}
