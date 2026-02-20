using Microsoft.Extensions.Options;

namespace JobResearchAgent.Services;

/// <summary>
/// Helper class to detect the language of a given text.
/// Currently supports Portuguese detection.
/// </summary>
public class LanguageDetector
{
    private readonly LanguageDetectionOptions _options;

    public LanguageDetector(IOptions<LanguageDetectionOptions> options)
    {
        _options = options?.Value ?? new LanguageDetectionOptions();
    }

    /// <summary>
    /// Determines if the provided text is in Portuguese.
    /// </summary>
    /// <param name="text">The text to analyze</param>
    /// <returns>True if the text is detected as Portuguese, false otherwise</returns>
    public bool IsPortuguese(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        if (_options.PortugueseIndicators.Count == 0)
            return false;

        var matches = _options.PortugueseIndicators.Count(indicator =>
            !string.IsNullOrWhiteSpace(indicator)
            && text.Contains(indicator, StringComparison.OrdinalIgnoreCase));

        var minimumMatches = _options.MinimumIndicatorMatches <= 0
            ? 1
            : _options.MinimumIndicatorMatches;

        // If we find enough Portuguese indicators, consider it Portuguese
        return matches >= minimumMatches;
    }
}
