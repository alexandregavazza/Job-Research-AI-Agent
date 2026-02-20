namespace JobResearchAgent.Services.Prompting;

public class PromptService : IPromptService
{
    private readonly string _templatePath;
    private readonly ILogger<PromptService> _logger;
    private readonly Dictionary<string, string> _templateCache = new();

    public PromptService(IHostEnvironment env, ILogger<PromptService> logger)
    {
        _templatePath = Path.Combine(env.ContentRootPath, "PromptTemplates");
        _logger = logger;
    }

    public string LoadSystemPrompt(string templateName)
    {
        var fileName = $"{templateName}.System.txt";
        return LoadTemplate(fileName);
    }

    public string LoadUserPrompt(string templateName, Dictionary<string, string> placeholders)
    {
        var fileName = $"{templateName}.User.txt";
        var template = LoadTemplate(fileName);
        return SubstitutePlaceholders(template, placeholders);
    }

    private string LoadTemplate(string fileName)
    {
        var cacheKey = fileName;

        // Check cache first
        if (_templateCache.TryGetValue(cacheKey, out var cached))
            return cached;

        var fullPath = Path.Combine(_templatePath, fileName);

        if (!File.Exists(fullPath))
        {
            _logger.LogError("Template file not found: {Path}", fullPath);
            throw new FileNotFoundException($"Prompt template not found: {fileName}", fullPath);
        }

        var content = File.ReadAllText(fullPath);
        _templateCache[cacheKey] = content;

        _logger.LogInformation("Loaded prompt template: {FileName}", fileName);
        return content;
    }

    private string SubstitutePlaceholders(string template, Dictionary<string, string> placeholders)
    {
        var result = template;

        foreach (var (key, value) in placeholders)
        {
            var placeholder = $"{{{{{key}}}}}"; // Creates {{KEY}}
            result = result.Replace(placeholder, value ?? string.Empty);
        }

        return result;
    }
}
