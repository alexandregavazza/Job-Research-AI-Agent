using Microsoft.Extensions.Configuration;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using JobResearchAgent.Models;
using JobResearchAgent.Services.FileManipulator;

namespace JobResearchAgent.Services;

/// <summary>
/// PDF resume exporter following SOLID principles with dependency injection
/// </summary>
public class PdfResumeExporter
{
    private readonly string _basePath;
    private readonly IConfiguration _config;
    private readonly string[] _education;
    private readonly IFileSanitizer _fileSanitizer;

    public PdfResumeExporter(IConfiguration config, IFileSanitizer fileSanitizer)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _fileSanitizer = fileSanitizer ?? throw new ArgumentNullException(nameof(fileSanitizer));
        
        _basePath = config["Output:BasePath"]
            ?? throw new InvalidOperationException("Output:BasePath not configured.");
        _education = config.GetSection("Candidate:Education").Get<string[]>() 
            ?? throw new InvalidOperationException("Candidate:Education not configured.");
    }

    public string Export(JobPosting job, TailoredResume resume)
    {
        var isPortuguese = LanguageDetector.IsPortuguese(job.Description ?? resume.ProfessionalSummary ?? "");
        var earlyCareerLabel = EarlyCareerTextSelector.SelectLabel(_config, isPortuguese);
        var earlyCareerDescription = EarlyCareerTextSelector.SelectDescription(_config, isPortuguese);
        var phone = ResolvePhoneByLocation(job);
        var todayFolder = Path.Combine(_basePath, DateTime.UtcNow.ToString("yyyy-MM-dd"));
        Directory.CreateDirectory(todayFolder);

        var safeCompany = _fileSanitizer.Sanitize(job.Company);
        var safeTitle = _fileSanitizer.Sanitize(job.Title);

        var filePath = Path.Combine(todayFolder, $"{safeCompany}_{safeTitle}_Resume.pdf");

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(40);

                page.Header().ShowOnce().Column(header =>
                {
                    header.Item().Text(_config["Candidate:FullName"] ?? "Name Not Configured")
                        .FontSize(20)
                        .Bold();

                    header.Item().Text(text =>
                    {
                        text.Span(phone);
                    });

                    header.Item().Text(text =>
                    {
                        text.Span(_config["Candidate:Email"] ?? "Email Not Configured");
                    });

                    header.Item().Text(text =>
                    {
                        text.Span(_config["Candidate:LinkedIn"] ?? "LinkedIn Not Configured");
                    });

                    header.Spacing(15);
                    header.Item().PaddingTop(10).LineHorizontal(1);
                });

                page.Content().Column(col =>
                {
                    col.Spacing(15);
                    col.Item().Text("Professional Summary").FontSize(12).Bold();
                    col.Item().Text(resume.ProfessionalSummary);

                    col.Spacing(15);
                    col.Item().Text("Key Skills").FontSize(12).Bold();
                    col.Item().Text(string.Join(" • ", resume.KeySkills));

                    string lastYear = string.Empty;
                    foreach (var exp in resume.Experience)
                    {
                        col.Item().Column(expCol =>
                        {
                            expCol.Item().Text($"{exp.Role} — {exp.Company} ({exp.StartDate} - {exp.EndDate})")
                                .Bold();

                            foreach (var highlight in exp.Highlights)
                            {
                                expCol.Item().Text($"• {highlight}");
                            }
                        });

                        lastYear = exp.StartDate.Split('-').Last().Trim();
                    }

                    // Add early career section if configured
                    col.Item().Column(expCol =>
                    {
                        expCol.Item().Text($"{earlyCareerLabel} ({_config["Candidate:Career:StartYear"] ?? "N/A"} - {lastYear})")
                            .Bold();
                        expCol.Item().Text($"• {earlyCareerDescription}");
                    });

                    // EDUCATION SECTION
                    if (_education.Any())
                    {
                        col.Item().Text("Education").FontSize(12).Bold();

                        foreach (var edu in _education)
                        {
                            col.Item().Text($"• {edu}");
                        }
                    }
                });
            });
        })
        .GeneratePdf(filePath);

        return filePath;
    }

    private string ResolvePhoneByLocation(JobPosting job)
    {
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

        return _config["Candidate:Phone"] ?? "Phone Not Configured";
    }
}