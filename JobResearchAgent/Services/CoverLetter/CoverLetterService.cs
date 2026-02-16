using JobResearchAgent.Models;
using OpenAI;
using OpenAI.Chat;

public class CoverLetterService : ICoverLetterService
{
    private readonly ChatClient _chat;
    private readonly ILogger<CoverLetterService> _logger;
    private readonly IConfiguration _config;

    public CoverLetterService(OpenAIClient client, IConfiguration config, ILogger<CoverLetterService> logger)
    {
        var model = config["AI:Model"]!;
        _chat = client.GetChatClient(model);
        _config = config;
    }

    public async Task<GeneratedCoverLetter> GenerateAsync(
        JobPosting job,
        TailoredResume resume,
        CancellationToken ct = default)
    {
        var prompt = BuildPrompt(job, resume);

        var response = await _chat.CompleteChatAsync(
            new ChatMessage[]
            {
                new SystemChatMessage(SystemPrompt),
                new UserChatMessage(prompt)
            },
            new ChatCompletionOptions
            {
                Temperature = 0.3f,
                TopP = 1.0f,
                MaxOutputTokenCount = 500
            },
            ct);

        var text = string.Join("",
            response.Value.Content
                .Where(c => c.Kind == ChatMessageContentPartKind.Text)
                .Select(c => c.Text));

        return new GeneratedCoverLetter
        {
            JobId = job.ExternalJobId,
            Company = job.Company,
            Title = job.Title,
            Content = text
        };
    }

    private const string SystemPrompt = """
        You are a senior engineering hiring manager writing concise, credible cover letters.

        Rules:
        - Do NOT summarize the resume.
        - Do NOT invent skills or experience.
        - Do NOT sound enthusiastic or sales-like.
        - Avoid buzzwords.
        - Use a formal tone, but not overly stiff.
        - Use human-like language.
        - Write like an experienced engineer.
        - Focus on alignment with the role and business impact.
        - 220–300 words maximum.
        """;

    private string BuildPrompt(JobPosting job, TailoredResume resume)
    {
        var today = DateTime.UtcNow.ToString("MMMM dd, yyyy");

        var skills = string.Join(", ", resume.KeySkills);

        var experiences = string.Join("\n\n",
            resume.Experience.Select(e => $"""
    Company: {e.Company}
    Role: {e.Role}
    Period: {e.StartDate} - {e.EndDate}

    Key Contributions:
    {string.Join("\n", e.Highlights.Select(a => "- " + a))}
    """));

        return $"""
    Write a tailored cover letter for this role.

    The letter MUST follow this EXACT structure:

    --------------------------------
    {_config["Candidate:FullName"]}
    {_config["Candidate:Phone"]}
    {_config["Candidate:Email"]}

    {today}
    --------------------------------

    Then begin the letter (no extra headings).

    The letter MUST be grounded in the candidate's real selected experience below.
    Do NOT generalize. Reference the work naturally (no bullet lists).

    ROLE:
    {job.Title}

    COMPANY:
    {job.Company}

    JOB DESCRIPTION:
    {job.Description}

    CANDIDATE PROFESSIONAL SUMMARY:
    {resume.ProfessionalSummary}

    CORE SKILLS IDENTIFIED FOR THIS ROLE:
    {skills}

    RELEVANT EXPERIENCE SELECTED FOR THIS APPLICATION:
    {experiences}

    Instructions:
    - Connect past systems to this company's needs.
    - Show engineering ownership and delivery mindset.
    - Emphasize architecture, modernization, scalability, or cloud when relevant.
    - Use specific experience context, but do NOT restate bullets.
    - Avoid buzzwords and enthusiasm language.
    - Sound like a senior engineer writing directly to a hiring manager.
    - 220–300 words max.
    - Use natural business prose (no lists).

    Do NOT invent experience.
    Do NOT list every technology.
    Do NOT summarize the resume.
    Focus on relevance and impact.
    Always add "Sincerely, {_config["Candidate:FullName"]}" at the end.

    Return ONLY the finished letter.
    """;
    }
}