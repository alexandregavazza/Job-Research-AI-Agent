namespace JobResearchAgent.Services.Storage;

public class LocalDocumentStorage : IDocumentStorage
{
    private readonly ILogger<LocalDocumentStorage> _logger;

    public LocalDocumentStorage(ILogger<LocalDocumentStorage> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<string> StoreAsync(string filePath, CancellationToken cancellationToken)
    {
        //_logger.LogInformation("Using local storage. File saved at: {FilePath}", filePath);
        return Task.FromResult(filePath);
    }
}
