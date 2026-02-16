using Dapper;
using Npgsql;

public class JobRepository
{
    private readonly string _connection;

    public JobRepository(string connection)
    {
        _connection = connection;
    }

    public async Task SaveAsync(IEnumerable<JobPosting> jobs)
    {
        using var conn = new NpgsqlConnection(_connection);

        foreach (var job in jobs)
        {
            await conn.ExecuteAsync(
            @"INSERT INTO jobs(title, company, location, url, description,source, collectedat, createdat, external_job_id, match_score)
              VALUES (@Title, @Company, @Location, @Url, @Description, @Source, @CollectedAt, @CreatedAt, @ExternalJobId, @MatchScore) ON CONFLICT (external_job_id) DO NOTHING;",
              job);
        }
    }
}