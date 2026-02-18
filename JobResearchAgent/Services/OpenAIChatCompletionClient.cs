using OpenAI;
using OpenAI.Chat;

namespace JobResearchAgent.Services;

public class OpenAIChatCompletionClient : IChatCompletionClient
{
    private readonly ChatClient _chat;

    public OpenAIChatCompletionClient(OpenAIClient client, IConfiguration config)
    {
        if (client == null)
            throw new ArgumentNullException(nameof(client));
        if (config == null)
            throw new ArgumentNullException(nameof(config));

        var model = config["AI:Model"]
            ?? throw new InvalidOperationException("AI:Model configuration is missing.");
        _chat = client.GetChatClient(model);
    }

    public async Task<string> CompleteAsync(
        string systemPrompt,
        string userPrompt,
        ChatCompletionOptions options,
        CancellationToken ct = default)
    {
        var response = await _chat.CompleteChatAsync(
            new ChatMessage[]
            {
                new SystemChatMessage(systemPrompt),
                new UserChatMessage(userPrompt)
            },
            options,
            ct);

        return string.Join("",
            response.Value.Content
                .Where(c => c.Kind == ChatMessageContentPartKind.Text)
                .Select(c => c.Text));
    }
}