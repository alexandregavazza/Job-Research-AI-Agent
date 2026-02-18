using System.Text.Json;
using JobResearchAgent.Models;
using OpenAI;
using OpenAI.Chat;

namespace JobResearchAgent.Services;

public class ResumeCustomizer
{
    private readonly ChatClient _chat;

    public ResumeCustomizer(OpenAIClient client, IConfiguration config)
    {
        if (client == null)
            throw new ArgumentNullException(nameof(client));
        if (config == null)
            throw new ArgumentNullException(nameof(config));
        
        var model = config["AI:Model"]
            ?? throw new InvalidOperationException("AI:Model configuration is missing.");
        _chat = client.GetChatClient(model);
    }

    public async Task<TailoredResume> CustomizeAsync(
        string baseResume,
        string jobTitle,
        string jobDescription)
    {
        var prompt = BuildPrompt(baseResume, jobTitle, jobDescription);

        var response = await _chat.CompleteChatAsync(
            new ChatMessage[]
            {
                new SystemChatMessage(
                    "You are an expert technical recruiter rewriting resumes to maximize interview chances."),
                new UserChatMessage(prompt)
            },
            new ChatCompletionOptions
            {
                Temperature = 0.3f,
                TopP = 1.0f,
                MaxOutputTokenCount = 1200
            }
        );

        var content = string.Join("",
            response.Value.Content
                .Where(c => c.Kind == ChatMessageContentPartKind.Text)
                .Select(c => c.Text));

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

        return $@"
Rewrite the candidate's resume so it is highly targeted to THIS specific job.

Rules:
- Do NOT invent experience.
- Do NOT add technologies not present in the original resume.
- You may rephrase to match terminology used in the job description.
- Prioritize the most relevant experience.
- De-emphasize unrelated work.
- Keep everything truthful.
- Focus on achievements and impact.
- Make the candidate sound like a strong fit for THIS role.
- Select ALL relevant experiences (5–8 typical).
- Do not artificially limit the list.
- Keep each concise and achievement-focused.
- Use bullet points for readability.
- Add the name of the company for each experience and the period that I worked in the company.
- Do not summarize, rewrite strategically like a recruiter would.
- Use human language, not robotic or generic phrasing.
- Add my name, phone number, LinkedIn profile, my personal website, and email at the top of the resume.
- Avoid creating more than 5 pages of content.
- {languageInstruction}

You are not summarizing.
You are rewriting strategically like a recruiter would.

Return ONLY valid JSON in this format:

{{
  ""ProfessionalSummary"": ""..."",
  ""KeySkills"": [""..."", ""...""],
  ""Experience"": [
    {{
      ""Role"": ""..."",
      ""Company"": ""..."",
      ""StartDate"": ""..."",
      ""EndDate"": ""..."",
      ""Highlights"": [""..."", ""...""]
    }}
  ]
}}

Candidate Resume:
{resume}

Target Job Title:
{title}

Target Job Description:
{description}
""";
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