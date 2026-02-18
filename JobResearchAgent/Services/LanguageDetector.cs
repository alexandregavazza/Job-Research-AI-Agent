namespace JobResearchAgent.Services;

/// <summary>
/// Helper class to detect the language of a given text.
/// Currently supports Portuguese detection.
/// </summary>
public static class LanguageDetector
{
    /// <summary>
    /// Determines if the provided text is in Portuguese.
    /// </summary>
    /// <param name="text">The text to analyze</param>
    /// <returns>True if the text is detected as Portuguese, false otherwise</returns>
    public static bool IsPortuguese(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        // Common Portuguese words and patterns
        var portugueseIndicators = new[]
        {
            "experiência", "responsabilidades", "requisitos", "empresa",
            "trabalho", "habilidades", "cargo", "qualificações", "descrição",
            "competências", "departamento", "português", "brasil", "portugal",
            "educação", "formação", "certificações", "certificados", "linguagem",
            "deve ter", "é necessário", "buscamos", "procuramos", "estamos",
            "processo seletivo", "candidatos", "vaga", "salário", "benefícios",
            "você", "será", "através", "conhecimento"
        };

        var lowerText = text.ToLowerInvariant();
        var matches = portugueseIndicators.Count(indicator => lowerText.Contains(indicator));

        // If we find at least 3 Portuguese indicators, consider it Portuguese
        return matches >= 3;
    }
}
