using JobResearchAgent.Services;

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
        var result = LanguageDetector.IsPortuguese(input);

        Assert.Equal(expected, result);
    }
}
