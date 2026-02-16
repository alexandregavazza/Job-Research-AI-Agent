using JobResearchAgent.Services;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
public class PdfCoverLetterExporter
{
    private readonly string _basePath;
    private readonly IConfiguration _config;

    public PdfCoverLetterExporter(IConfiguration config)
    {
        _config = config;
        _basePath = config["Output:BasePath"]
            ?? throw new Exception("Output:BasePath not configured.");
    }

    public string Export(GeneratedCoverLetter letter)
    {
        var todayFolder = Path.Combine(_basePath, DateTime.UtcNow.ToString("yyyy-MM-dd"));
        Directory.CreateDirectory(todayFolder);

        var safeCompany = FileSanitizer.Sanitize(letter.Company);
        var safeTitle = FileSanitizer.Sanitize(letter.Title);

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