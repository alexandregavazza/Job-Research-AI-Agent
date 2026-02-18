namespace JobResearchAgent.Services.Storage;

public interface IDocumentStorage
{
    Task<string> StoreAsync(string filePath, CancellationToken cancellationToken);
}
