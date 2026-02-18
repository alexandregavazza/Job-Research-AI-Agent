using OpenAI.Chat;

namespace JobResearchAgent.Services;

public interface IChatCompletionClient
{
    Task<string> CompleteAsync(
        string systemPrompt,
        string userPrompt,
        ChatCompletionOptions options,
        CancellationToken ct = default);
}