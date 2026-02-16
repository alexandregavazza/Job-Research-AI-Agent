using Microsoft.Extensions.Configuration;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using JobResearchAgent.Models;

namespace JobResearchAgent.Services;

public class PdfResumeExporter
{
    private readonly string _basePath;
    private readonly IConfiguration _config;
    private readonly string[] _education;

    public PdfResumeExporter(IConfiguration config)
    {
        _config = config;
        _basePath = config["Output:BasePath"]
            ?? throw new Exception("Output:BasePath not configured.");
        _education = config.GetSection("Candidate:Education").Get<string[]>() 
            ?? throw new Exception("Candidate:Education not configured.");
    }

    public string Export(JobPosting job, TailoredResume resume)
    {
        var todayFolder = Path.Combine(_basePath, DateTime.UtcNow.ToString("yyyy-MM-dd"));
        Directory.CreateDirectory(todayFolder);

        var safeCompany = Sanitize(job.Company);
        var safeTitle = Sanitize(job.Title);

        var filePath = Path.Combine(todayFolder, $"{safeCompany}_{safeTitle}.pdf");

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(40);

                page.Header().ShowOnce().Column(header =>
                {
                    header.Item().Text(_config["Candidate:FullName"])
                        .FontSize(20)
                        .Bold();

                    header.Item().Text(text =>
                    {
                        text.Span(_config["Candidate:Phone"]);
                    });

                    header.Item().Text(text =>
                    {
                        text.Span(_config["Candidate:Email"]);
                    });

                    header.Item().Text(text =>
                    {
                        text.Span(_config["Candidate:LinkedIn"]);
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
                        expCol.Item().Text($"{_config["Candidate:Career:EarlyCareerLabel"]} ({_config["Candidate:Career:StartYear"]} - {lastYear})")
                            .Bold();
                        expCol.Item().Text($"• {_config["Candidate:Career:Description"]}");
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

    private string Sanitize(string input)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
            input = input.Replace(c, '_');

        return input.Replace(" ", "");
    }
}