using JobResearchAgent.Services;
using Microsoft.Extensions.Options;

namespace JobResearchAgent.Tests;

public class LanguageDetectorTests
{
    [Theory]
    [InlineData("Experiência sólida com responsabilidades e requisitos do cargo.", true)]
    [InlineData("Requisitos e responsabilidades do cargo na empresa.", true)]
    [InlineData("We are looking for a senior engineer with cloud experience.", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    [InlineData("Experiência com cloud, trabalho remoto, and team collaboration.", false)]
    [InlineData("EXPERIÊNCIA e RESPONSABILIDADES com requisitos claros.", true)]
    [InlineData("requisitos e empresa", false)]
    [InlineData("Requisitos, experiência; empresa—benefícios.", true)]
    [InlineData("experiência e requisitos", false)]
    [InlineData("We need experience in cloud and data; empresa is a plus.", false)]
    [InlineData("Experiencia y responsabilidades para el puesto.", false)]
    [InlineData("A empresa busca profissionais com experiência e responsabilidades claras no cargo.", true)]
    public void IsPortuguese_ReturnsExpectedResult(string? input, bool expected)
    {
        var detector = BuildLanguageDetector(CreateDefaultOptions());
        var result = detector.IsPortuguese(input);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void IsPortuguese_ReturnsFalseWhenIndicatorsEmpty()
    {
        var options = new LanguageDetectionOptions
        {
            PortugueseIndicators = new List<string>(),
            MinimumIndicatorMatches = 3
        };

        var detector = BuildLanguageDetector(options);

        Assert.False(detector.IsPortuguese("experiência"));
    }

    [Fact]
    public void IsPortuguese_UsesMinimumOneWhenNonPositive()
    {
        var options = new LanguageDetectionOptions
        {
            PortugueseIndicators = new List<string> { "empresa" },
            MinimumIndicatorMatches = 0
        };

        var detector = BuildLanguageDetector(options);

        Assert.True(detector.IsPortuguese("empresa"));
    }

    private static LanguageDetector BuildLanguageDetector(LanguageDetectionOptions options)
    {
        return new LanguageDetector(Options.Create(options));
    }

    private static LanguageDetectionOptions CreateDefaultOptions()
    {
        return new LanguageDetectionOptions
        {
            PortugueseIndicators = new List<string>
            {
                "experiência", "responsabilidades", "requisitos", "empresa",
                "trabalho", "habilidades", "cargo", "qualificações", "descrição",
                "competências", "departamento", "português", "brasil", "portugal",
                "educação", "formação", "certificações", "certificados", "linguagem",
                "deve ter", "é necessário", "buscamos", "procuramos", "estamos",
                "processo seletivo", "candidatos", "vaga", "salário", "benefícios",
                "você", "será", "através", "conhecimento"
            },
            MinimumIndicatorMatches = 3
        };
    }
}
