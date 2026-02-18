using JobResearchAgent.Services;
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

        var scorer = new JobFitScorer(chat.Object);

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

        var scorer = new JobFitScorer(chat.Object);

        await Assert.ThrowsAsync<Exception>(() =>
            scorer.ScoreAsync("resume", "title", "description"));
    }
}