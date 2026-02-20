using System.Text.Json;
using JobResearchAgent.Services.Prompting;
using OpenAI.Chat;

namespace JobResearchAgent.Services;

public class JobFitScorer
{
    private readonly IChatCompletionClient _chat;
    private readonly IPromptService _promptService;

    public JobFitScorer(IChatCompletionClient chat, IPromptService promptService)
    {
        _chat = chat ?? throw new ArgumentNullException(nameof(chat));
        _promptService = promptService ?? throw new ArgumentNullException(nameof(promptService));
    }

    public async Task<(double score, string reason)> ScoreAsync(
        string resume,
        string jobTitle,
        string jobDescription)
    {
        var systemPrompt = _promptService.LoadSystemPrompt("JobFitScorer");
        var userPrompt = _promptService.LoadUserPrompt(
            "JobFitScorer",
            new Dictionary<string, string>
            {
                { "RESUME", resume },
                { "JOB_TITLE", jobTitle },
                { "JOB_DESCRIPTION", jobDescription }
            });

        var content = await _chat.CompleteAsync(
            systemPrompt,
            userPrompt,
            new ChatCompletionOptions
            {
                Temperature = 0.0f,
                TopP = 1.0f,
                MaxOutputTokenCount = 300
            });
        
        var cleaned = CleanJson(content);

        var result = JsonSerializer.Deserialize<JobFitResponse>(cleaned)
             ?? throw new Exception("Failed to parse LLM response.");

        return (result.score, result.reason);
    }

    private static string CleanJson(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            throw new Exception("Empty LLM response.");

        // Remove markdown fences if present
        raw = raw.Replace("```json", "")
                .Replace("```", "")
                .Trim();

        // Sometimes the model adds commentary before JSON — extract first '{'
        var start = raw.IndexOf('{');
        var end = raw.LastIndexOf('}');

        if (start == -1 || end == -1)
            throw new Exception($"No JSON object found in:\n{raw}");

        return raw.Substring(start, end - start + 1);
    }

    private class JobFitResponse
    {
        public double score { get; set; }
        public string reason { get; set; } = "";
    }
}
