using JobResearchAgent.Models;

namespace JobResearchAgent.Infrastructure;

public interface IApplicationLogRepository
{
    Task InsertAsync(ApplicationLog log, CancellationToken ct);

    Task<bool> WasJobInsertedWithinDaysAsync(string externalJobId, string jobTitle, string company, string location, int days, CancellationToken ct);
}