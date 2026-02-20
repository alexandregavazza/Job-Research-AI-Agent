using JobResearchAgent.Services;
using JobResearchAgent.Services.Prompting;
using Moq;
using OpenAI.Chat;

namespace JobResearchAgent.Tests;

public class JobFitScorerTests
{
    [Fact]
    public async Task ScoreAsync_ReturnsParsedScoreAndReason()
    {
        var responseJson = "```json\n{\"score\":85,\"reason\":\"Strong match\"}\n```";
        var chat = new Mock<IChatCompletionClient>();
        chat.Setup(c => c.CompleteAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<ChatCompletionOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(responseJson);

        var promptService = new Mock<IPromptService>();
        promptService.Setup(p => p.LoadSystemPrompt("JobFitScorer"))
            .Returns("system");
        promptService.Setup(p => p.LoadUserPrompt("JobFitScorer", It.IsAny<Dictionary<string, string>>()))
            .Returns("user");

        var scorer = new JobFitScorer(chat.Object, promptService.Object);

        var (score, reason) = await scorer.ScoreAsync("resume", "title", "description");

        Assert.Equal(85, score);
        Assert.Equal("Strong match", reason);
    }

    [Fact]
    public async Task ScoreAsync_ThrowsWhenMissingJson()
    {
        var chat = new Mock<IChatCompletionClient>();
        chat.Setup(c => c.CompleteAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<ChatCompletionOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("No JSON here");

        var promptService = new Mock<IPromptService>();
        promptService.Setup(p => p.LoadSystemPrompt("JobFitScorer"))
            .Returns("system");
        promptService.Setup(p => p.LoadUserPrompt("JobFitScorer", It.IsAny<Dictionary<string, string>>()))
            .Returns("user");

        var scorer = new JobFitScorer(chat.Object, promptService.Object);

        await Assert.ThrowsAsync<Exception>(() =>
            scorer.ScoreAsync("resume", "title", "description"));
    }

    [Fact]
    public async Task ScoreAsync_PassesExpectedPlaceholders()
    {
        Dictionary<string, string>? captured = null;

        var chat = new Mock<IChatCompletionClient>();
        chat.Setup(c => c.CompleteAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<ChatCompletionOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("{\"score\":85,\"reason\":\"Strong match\"}");

        var promptService = new Mock<IPromptService>();
        promptService.Setup(p => p.LoadSystemPrompt("JobFitScorer"))
            .Returns("system");
        promptService.Setup(p => p.LoadUserPrompt("JobFitScorer", It.IsAny<Dictionary<string, string>>()))
            .Callback<string, Dictionary<string, string>>((_, placeholders) =>
            {
                captured = placeholders;
            })
            .Returns("user");

        var scorer = new JobFitScorer(chat.Object, promptService.Object);

        await scorer.ScoreAsync("resume", "title", "description");

        Assert.NotNull(captured);
        Assert.Equal("resume", captured!["RESUME"]);
        Assert.Equal("title", captured["JOB_TITLE"]);
        Assert.Equal("description", captured["JOB_DESCRIPTION"]);
    }
}