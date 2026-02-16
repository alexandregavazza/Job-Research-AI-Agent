using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Embeddings;

namespace JobResearchAgent.Services;

public class EmbeddingService
{
    [Obsolete]
    private readonly ITextEmbeddingGenerationService _embedding;

    [Obsolete]
    public EmbeddingService(IConfiguration config)
    {
        var builder = Kernel.CreateBuilder();
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");

        _ = builder.AddOpenAITextEmbeddingGeneration(
            modelId: config["AI:EmbeddingModel"] ?? throw new InvalidOperationException("AI:EmbeddingModel configuration is missing"),
            apiKey: apiKey ?? throw new InvalidOperationException("OPENAI_API_KEY environment variable is missing"));

        var kernel = builder.Build();
        _embedding = kernel.GetRequiredService<ITextEmbeddingGenerationService>();
    }

    public async Task<float[]> GenerateAsync(string text)
    {
        var result = await _embedding.GenerateEmbeddingAsync(text);
        return result.ToArray();
    }
}