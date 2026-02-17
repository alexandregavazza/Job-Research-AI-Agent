using Dapper;
using Npgsql;
using JobResearchAgent.Models;

namespace JobResearchAgent.Infrastructure;

public class ApplicationLogRepository : IApplicationLogRepository
{
    private readonly string _connection;

    public ApplicationLogRepository(string connection)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
    }

    public async Task InsertAsync(ApplicationLog log, CancellationToken ct)
    {
        const string sql = """
            INSERT INTO job_application_logs
            (
                id,
                external_job_id,
                job_title,
                company,
                location,
                url,
                source,
                created_at,
                resume_path,
                cover_letter_path,
                match_score,
                status,
                notes
            )
            VALUES
            (
                @Id,
                @ExternalJobId,
                @JobTitle,
                @Company,
                @Location,
                @Url,
                @Source,
                @CreatedAt,
                @ResumePath,
                @CoverLetterPath,
                @MatchScore,
                @Status,
                @Notes
            );
            """;

            await using var conn = new NpgsqlConnection(_connection);
            await conn.OpenAsync(ct);
            await conn.ExecuteAsync(new CommandDefinition(sql, log, cancellationToken: ct));
    }

    public async Task<bool> WasJobInsertedWithinDaysAsync(string externalJobId, int days, CancellationToken ct)
    {
        const string sql = "SELECT created_at FROM job_application_logs WHERE external_job_id = @id LIMIT 1;";

        await using var conn = new NpgsqlConnection(_connection);
        await conn.OpenAsync(ct);

        var createdAt = await conn.ExecuteScalarAsync<DateTime?>(
            new CommandDefinition(sql, new { id = externalJobId }, cancellationToken: ct));

        if (createdAt == null)
        {
            return false;
        }

        return createdAt.Value >= DateTime.UtcNow.AddDays(-days);
    }
}