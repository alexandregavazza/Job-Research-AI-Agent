using JobResearchAgent.Models;

namespace JobResearchAgent.Infrastructure;

public interface IApplicationLogRepository
{
    Task InsertAsync(ApplicationLog log, CancellationToken ct);
}