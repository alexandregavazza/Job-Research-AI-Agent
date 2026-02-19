using JobResearchAgent.Services;
using Moq;
using OpenAI.Chat;

namespace JobResearchAgent.Tests;

public class ResumeCustomizerTests
{
    [Fact]
    public async Task CustomizeAsync_ReturnsParsedResume()
    {
        var responseJson = "Here is your result:\n{\"ProfessionalSummary\":\"Summary\",\"KeySkills\":[\"C#\"],\"Experience\":[{\"Role\":\"Dev\",\"Company\":\"Contoso\",\"StartDate\":\"2020\",\"EndDate\":\"2024\",\"Highlights\":[\"Built APIs\"]}]}";
        var chat = new Mock<IChatCompletionClient>();
        chat.Setup(c => c.CompleteAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<ChatCompletionOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(responseJson);

        var promptService = new Mock<IPromptService>();
        promptService.Setup(p => p.LoadSystemPrompt("ResumeCustomizer"))
            .Returns("system");
        promptService.Setup(p => p.LoadUserPrompt("ResumeCustomizer", It.IsAny<Dictionary<string, string>>()))
            .Returns<string, Dictionary<string, string>>((_, placeholders) =>
                $"Prompt: {placeholders.GetValueOrDefault("LANGUAGE_INSTRUCTION", string.Empty)}");

        var service = new ResumeCustomizer(chat.Object, promptService.Object);

        var result = await service.CustomizeAsync("base", "title", "description");

        Assert.Equal("Summary", result.ProfessionalSummary);
        Assert.Single(result.KeySkills);
        Assert.Single(result.Experience);
        Assert.Equal("Dev", result.Experience[0].Role);
    }

    [Fact]
    public async Task CustomizeAsync_UsesPortugueseInstructionWhenDescriptionPortuguese()
    {
        var capturedPrompt = "";
        var responseJson = "{\"ProfessionalSummary\":\"Summary\",\"KeySkills\":[],\"Experience\":[]}";
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
            .ReturnsAsync(responseJson);

        var promptService = new Mock<IPromptService>();
        promptService.Setup(p => p.LoadSystemPrompt("ResumeCustomizer"))
            .Returns("system");
        promptService.Setup(p => p.LoadUserPrompt("ResumeCustomizer", It.IsAny<Dictionary<string, string>>()))
            .Returns<string, Dictionary<string, string>>((_, placeholders) =>
                $"Prompt: {placeholders.GetValueOrDefault("LANGUAGE_INSTRUCTION", string.Empty)}");

        var service = new ResumeCustomizer(chat.Object, promptService.Object);

        await service.CustomizeAsync("base", "title", "Experiência sólida com responsabilidades e requisitos do cargo.");

        Assert.Contains("Write the resume in Portuguese.", capturedPrompt);
    }
}