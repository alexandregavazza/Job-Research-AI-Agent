using JobResearchAgent.Models;
using JobResearchAgent.Services;
using JobResearchAgent.Services.Prompting;
using OpenAI.Chat;

namespace JobResearchAgent.Services.CoverLetter;

public class CoverLetterService : ICoverLetterService
{
    private readonly IChatCompletionClient _chat;
    private readonly ILogger<CoverLetterService> _logger;
    private readonly IConfiguration _config;
    private readonly IPromptService _promptService;
    private readonly LanguageDetector _languageDetector;

    public CoverLetterService(
        IChatCompletionClient chat,
        IConfiguration config,
        ILogger<CoverLetterService> logger,
        IPromptService promptService,
        LanguageDetector languageDetector)
    {
        if (chat == null)
            throw new ArgumentNullException(nameof(chat));
        if (config == null)
            throw new ArgumentNullException(nameof(config));
        if (logger == null)
            throw new ArgumentNullException(nameof(logger));
        if (promptService == null)
            throw new ArgumentNullException(nameof(promptService));
        if (languageDetector == null)
            throw new ArgumentNullException(nameof(languageDetector));

        _chat = chat;
        _config = config;
        _logger = logger;
        _promptService = promptService;
        _languageDetector = languageDetector;
    }

    public async Task<GeneratedCoverLetter> GenerateAsync(
        JobPosting job,
        TailoredResume resume,
        CancellationToken ct = default)
    {
        var systemPrompt = _promptService.LoadSystemPrompt("CoverLetter");
        var userPrompt = BuildPrompt(job, resume);

        var text = await _chat.CompleteAsync(
            systemPrompt,
            userPrompt,
            new ChatCompletionOptions
            {
                Temperature = 0.3f,
                TopP = 1.0f,
                MaxOutputTokenCount = 500
            },
            ct);

        return new GeneratedCoverLetter
        {
            JobId = job.ExternalJobId ?? job.Url ?? "unknown",
            Company = job.Company,
            Title = job.Title,
            Content = text
        };
    }

    private string ResolvePhoneByLocation(JobPosting job)
    {
        // Try to infer country from job metadata
        var location = job.Location?.ToLowerInvariant() ?? "";

        if (location.Contains("brazil") ||
            location.Contains("são paulo") ||
            location.Contains("belo horizonte") ||
            location.Contains("rio de janeiro") ||
            location.Contains("brasília") ||
            location.Contains("brasil"))
            return _config["Candidate:PhoneBR"]
                ?? _config["Candidate:Phone"]
                ?? "Phone Not Configured";

        if (location.Contains("canada"))
            return _config["Candidate:PhoneCA"]
                ?? _config["Candidate:Phone"]
                ?? "Phone Not Configured";

        if (location.Contains("united states") ||
            location.Contains("usa") ||
            location.Contains("us"))
            return _config["Candidate:PhoneUS"]
                ?? _config["Candidate:Phone"]
                ?? "Phone Not Configured";

        // Default for Singapore, UAE, etc.
        return _config["Candidate:Phone"] ?? "Phone Not Configured";
    }

    private string ResolveLocationByJob(JobPosting job)
    {
        // Try to infer country from job metadata
        var location = job.Location?.ToLowerInvariant() ?? "";

        if (location.Contains("brazil") ||
            location.Contains("são paulo") ||
            location.Contains("belo horizonte") ||
            location.Contains("rio de janeiro") ||
            location.Contains("brasília") ||
            location.Contains("brasil"))
            return _config["Candidate:LocationBR"]
                ?? _config["Candidate:Location"]
                ?? "Location Not Configured";

        if (location.Contains("canada"))
            return _config["Candidate:LocationCA"]
                ?? _config["Candidate:Location"]
                ?? "Location Not Configured";

        if (location.Contains("united states") ||
            location.Contains("usa") ||
            location.Contains("us"))
            return _config["Candidate:LocationUS"]
                ?? _config["Candidate:Location"]
                ?? "Location Not Configured";

        // Default for Singapore, UAE, etc.
        return _config["Candidate:Location"] ?? "Location Not Configured";
    }

    private string BuildPrompt(JobPosting job, TailoredResume resume)
    {
        var today = DateTime.UtcNow.ToString("MMMM dd, yyyy");
        var phone = ResolvePhoneByLocation(job);
        var location = ResolveLocationByJob(job);
        var skills = string.Join(", ", resume.KeySkills);

        var experiences = string.Join("\n\n",
            resume.Experience.Select(e => $"""
                Company: {e.Company}
                Role: {e.Role}
                Period: {e.StartDate} - {e.EndDate}

                Key Contributions:
                {string.Join("\n", e.Highlights.Select(a => "- " + a))}
                """));

        var isPortuguese = _languageDetector.IsPortuguese(job.Description ?? "");
        var languageInstruction = isPortuguese
            ? "IMPORTANT: Write the entire cover letter in Portuguese (PT-BR), as the job description is in Portuguese."
            : "";

        var placeholders = new Dictionary<string, string>
        {
            { "LANGUAGE_INSTRUCTION", languageInstruction },
            { "FULL_NAME", _config["Candidate:FullName"] ?? "Name Not Configured" },
            { "PHONE", phone },
            { "EMAIL", _config["Candidate:Email"] ?? "Email Not Configured" },
            { "LOCATION", location },
            { "TODAY", today },
            { "JOB_TITLE", job.Title ?? "" },
            { "COMPANY", job.Company ?? "" },
            { "JOB_DESCRIPTION", job.Description ?? "" },
            { "PROFESSIONAL_SUMMARY", resume.ProfessionalSummary ?? "" },
            { "SKILLS", skills },
            { "EXPERIENCES", experiences }
        };

        return _promptService.LoadUserPrompt("CoverLetter", placeholders);
    }
}