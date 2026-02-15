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
            @"INSERT INTO jobs(title, company, location, url, description,source, collectedat, createdat)
              VALUES (@Title, @Company, @Location, @Url, @Description, @Source, @CollectedAt, @createdAt)",
              job);
        }
    }
}