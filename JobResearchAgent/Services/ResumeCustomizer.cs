using System.Text.Json;
using JobResearchAgent.Models;
using OpenAI.Chat;

namespace JobResearchAgent.Services;

public class ResumeCustomizer
{
    private readonly IChatCompletionClient _chat;
    private readonly IPromptService _promptService;

    public ResumeCustomizer(IChatCompletionClient chat, IPromptService promptService)
    {
        _chat = chat ?? throw new ArgumentNullException(nameof(chat));
        _promptService = promptService ?? throw new ArgumentNullException(nameof(promptService));
    }

    public async Task<TailoredResume> CustomizeAsync(
        string baseResume,
        string jobTitle,
        string jobDescription)
    {
        var systemPrompt = _promptService.LoadSystemPrompt("ResumeCustomizer");
        var userPrompt = BuildPrompt(baseResume, jobTitle, jobDescription);

        var content = await _chat.CompleteAsync(
            systemPrompt,
            userPrompt,
            new ChatCompletionOptions
            {
                Temperature = 0.1f,
                TopP = 1.0f,
                MaxOutputTokenCount = 1200
            });

        var cleaned = CleanJson(content);

        return JsonSerializer.Deserialize<TailoredResume>(cleaned)
            ?? throw new Exception("Failed to parse tailored resume.");
    }

    // ✅ STEP 4 — THIS is the prompt location
    private string BuildPrompt(string resume, string title, string description)
    {
        var isPortuguese = LanguageDetector.IsPortuguese(description);
        var languageInstruction = isPortuguese 
            ? "Write the resume in Portuguese."
            : "Write the resume in English.";

        var placeholders = new Dictionary<string, string>
        {
            { "LANGUAGE_INSTRUCTION", languageInstruction },
            { "RESUME", resume },
            { "JOB_TITLE", title },
            { "JOB_DESCRIPTION", description }
        };

        return _promptService.LoadUserPrompt("ResumeCustomizer", placeholders);
    }

    private string CleanJson(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            throw new InvalidOperationException("LLM returned empty response.");

        var start = raw.IndexOf('{');
        var end = raw.LastIndexOf('}');

        if (start >= 0 && end > start)
            return raw[start..(end + 1)];

        throw new InvalidOperationException("LLM did not return valid JSON.");
    }
}