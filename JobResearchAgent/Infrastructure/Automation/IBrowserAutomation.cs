namespace JobResearchAgent.Infrastructure.Automation;
public interface IBrowserAutomation : IAsyncDisposable
{
    Task InitializeAsync(CancellationToken ct);
    Task NavigateAsync(string url, CancellationToken ct);
    Task ClickAsync(string selector, CancellationToken ct);
    Task FillAsync(string selector, string value, CancellationToken ct);
    Task UploadFileAsync(string selector, string filePath, CancellationToken ct);
    Task WaitForSelectorAsync(string selector, CancellationToken ct);
    Task<bool> ElementExistsAsync(string selector, CancellationToken ct);
}