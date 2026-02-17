using JobResearchAgent.Services;
using JobResearchAgent.Services.FileManipulator;
using QuestPDF.Fluent;
using QuestPDF.Helpers;

/// <summary>
/// PDF cover letter exporter following SOLID principles with dependency injection
/// </summary>
public class PdfCoverLetterExporter
{
    private readonly string _basePath;
    private readonly IConfiguration _config;
    private readonly IFileSanitizer _fileSanitizer;

    public PdfCoverLetterExporter(IConfiguration config, IFileSanitizer fileSanitizer)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _fileSanitizer = fileSanitizer ?? throw new ArgumentNullException(nameof(fileSanitizer));
        
        _basePath = config["Output:BasePath"]
            ?? throw new InvalidOperationException("Output:BasePath not configured.");
    }

    public string Export(GeneratedCoverLetter letter)
    {
        var todayFolder = Path.Combine(_basePath, DateTime.UtcNow.ToString("yyyy-MM-dd"));
        Directory.CreateDirectory(todayFolder);

        var safeCompany = _fileSanitizer.Sanitize(letter.Company);
        var safeTitle = _fileSanitizer.Sanitize(letter.Title);

        var filePath = Path.Combine(todayFolder, $"{safeCompany}_{safeTitle}_CoverLetter.pdf");

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(40);

                page.Content().Column(col =>
                {
                    col.Item().PaddingTop(20);

                    col.Item().Text(letter.Content)
                        .FontSize(11)
                        .LineHeight(1.4f);
                });
            });
        })
        .GeneratePdf(filePath);

        return filePath;
    }
}