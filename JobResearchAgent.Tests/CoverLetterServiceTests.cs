using JobResearchAgent.Models;
using JobResearchAgent.Services;
using JobResearchAgent.Services.CoverLetter;
using JobResearchAgent.Services.Prompting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using OpenAI.Chat;

namespace JobResearchAgent.Tests;

public class CoverLetterServiceTests
{
    [Fact]
    public async Task GenerateAsync_ReturnsContentFromCompletion()
    {
        var chat = new Mock<IChatCompletionClient>();
        chat.Setup(c => c.CompleteAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<ChatCompletionOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("Letter body");

        var config = BuildConfig();
        var logger = Mock.Of<ILogger<CoverLetterService>>();
        var promptService = new Mock<IPromptService>();
        promptService.Setup(p => p.LoadSystemPrompt("CoverLetter"))
            .Returns("system");
        promptService.Setup(p => p.LoadUserPrompt("CoverLetter", It.IsAny<Dictionary<string, string>>()))
            .Returns<string, Dictionary<string, string>>((_, placeholders) =>
                $"Prompt: {placeholders.GetValueOrDefault("LANGUAGE_INSTRUCTION", string.Empty)}");

        var service = new CoverLetterService(
            chat.Object,
            config,
            logger,
            promptService.Object,
            BuildLanguageDetector());

        var job = CreateJob("Remote", "English description", "job-1");
        var resume = CreateResume();

        var result = await service.GenerateAsync(job, resume);

        Assert.Equal("job-1", result.JobId);
        Assert.Equal("Contoso", result.Company);
        Assert.Equal("Backend Engineer", result.Title);
        Assert.Equal("Letter body", result.Content);
    }

    [Fact]
    public async Task GenerateAsync_UsesPortugueseInstructionWhenDescriptionPortuguese()
    {
        var capturedPrompt = "";
        var chat = new Mock<IChatCompletionClient>();
        chat.Setup(c => c.CompleteAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<ChatCompletionOptions>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, string, ChatCompletionOptions, CancellationToken>((_, prompt, _, _) =>
            {
                capturedPrompt = prompt;
            })
            .ReturnsAsync("Letter body");

        var config = BuildConfig();
        var logger = Mock.Of<ILogger<CoverLetterService>>();
        var promptService = new Mock<IPromptService>();
        promptService.Setup(p => p.LoadSystemPrompt("CoverLetter"))
            .Returns("system");
        promptService.Setup(p => p.LoadUserPrompt("CoverLetter", It.IsAny<Dictionary<string, string>>()))
            .Returns<string, Dictionary<string, string>>((_, placeholders) =>
                $"Prompt: {placeholders.GetValueOrDefault("LANGUAGE_INSTRUCTION", string.Empty)}");

        var service = new CoverLetterService(
            chat.Object,
            config,
            logger,
            promptService.Object,
            BuildLanguageDetector());

        var job = CreateJob("Brazil", "Experiência sólida com responsabilidades e requisitos do cargo.", "job-2");
        var resume = CreateResume();

        await service.GenerateAsync(job, resume);

        Assert.Contains("Write the entire cover letter in Portuguese (PT-BR)", capturedPrompt);
    }

    private static IConfiguration BuildConfig()
    {
        var data = new Dictionary<string, string?>
        {
            ["Candidate:FullName"] = "Jane Doe",
            ["Candidate:Email"] = "jane@example.com",
            ["Candidate:Phone"] = "000",
            ["Candidate:Location"] = "Remote",
            ["Candidate:PhoneBR"] = "111",
            ["Candidate:LocationBR"] = "Sao Paulo"
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(data)
            .Build();
    }

    private static LanguageDetector BuildLanguageDetector()
    {
        var options = new LanguageDetectionOptions
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

        return new LanguageDetector(Options.Create(options));
    }

    private static JobPosting CreateJob(string location, string description, string jobId)
    {
        return new JobPosting
        {
            Title = "Backend Engineer",
            Company = "Contoso",
            Location = location,
            Url = "https://example.com",
            Description = description,
            Source = "test",
            ExternalJobId = jobId
        };
    }

    private static TailoredResume CreateResume()
    {
        return new TailoredResume
        {
            ProfessionalSummary = "Summary",
            KeySkills = new List<string> { "C#" },
            Experience = new List<TailoredExperience>
            {
                new TailoredExperience
                {
                    Role = "Developer",
                    Company = "Contoso",
                    StartDate = "2020",
                    EndDate = "2024",
                    Highlights = new List<string> { "Built APIs" }
                }
            }
        };
    }
}