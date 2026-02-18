using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Embeddings;

namespace JobResearchAgent.Services;

public class EmbeddingService
{
#pragma warning disable CS0618 // Type is obsolete - Semantic Kernel API migration pending
    private readonly ITextEmbeddingGenerationService _embedding;

    public EmbeddingService(IConfiguration config)
    {
        if (config == null)
            throw new ArgumentNullException(nameof(config));
        
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
#pragma warning restore CS0618
}