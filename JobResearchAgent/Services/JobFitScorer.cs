using OpenAI;
using OpenAI.Chat;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace JobResearchAgent.Services;

public class JobFitScorer
{
    private readonly ChatClient _chat;

    public JobFitScorer(OpenAIClient client, IConfiguration config)
    {
        if (client == null)
            throw new ArgumentNullException(nameof(client));
        if (config == null)
            throw new ArgumentNullException(nameof(config));
        
        var model = config["AI:Model"]
            ?? throw new InvalidOperationException("AI:Model configuration is missing.");
        _chat = client.GetChatClient(model);
    }

    public async Task<(double score, string reason)> ScoreAsync(
        string resume,
        string jobTitle,
        string jobDescription)
    {
        var prompt = BuildPrompt(resume, jobTitle, jobDescription);

        var response = await _chat.CompleteChatAsync(
            new ChatMessage[]
            {
                new SystemChatMessage("You are a senior software engineering recruiter."),
                new UserChatMessage(prompt)
            },
            new ChatCompletionOptions
            {
                Temperature = 0.0f,
                TopP = 1.0f,
                MaxOutputTokenCount = 300
            }
        );

        var content = string.Join("",
            response.Value.Content
                .Where(c => c.Kind == ChatMessageContentPartKind.Text)
                .Select(c => c.Text));
        
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

    private string BuildPrompt(string resume, string title, string description)
    {
        return $@"
            Evaluate this candidate against the job like a HUMAN hiring manager.

            SCORING RULES (MANDATORY):

            - Score MUST be an integer between 0 and 100.
            - 0 = no match at all
            - 50 = partial / transferable skills
            - 70 = strong match, some gaps
            - 85 = excellent match
            - 100 = perfect match

            DO NOT use a 1–10 scale.
            DO NOT output decimals.
            DO NOT explain scoring.
            DO NOT output anything except the JSON.

            If you output a value <= 10, you are WRONG.

            Equivalent skills count:
            - .NET = any modern backend (Java, Python, Node, Go, etc.)
            - Angular = React/Vue acceptable
            - AWS/Azure/GCP = ANY cloud
            - SQL = any relational DB
            - Microservices = distributed systems
            - REST APIs = backend service experience

            Evaluate like a human hiring manager, not a keyword matcher.

            Return ONLY this JSON format:
            {{ ""score"": 0-100, ""reason"": ""one short sentence"" }}

            Resume:
            {resume}

            Job Title:
            {title}

            Job Description:
            {description}
            ";
    }

    private class JobFitResponse
    {
        public double score { get; set; }
        public string reason { get; set; } = "";
    }
}
