namespace JobResearchAgent.Services;

/// <summary>
/// Concrete implementation of IFileSanitizer following SOLID principles
/// </summary>
public class FileSanitizer : IFileSanitizer
{
    public string Sanitize(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        foreach (var c in Path.GetInvalidFileNameChars())
            input = input.Replace(c, '_');

        return input.Replace(" ", "");
    }
}