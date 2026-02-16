using System.Text.Json;
using Dapper;
using JobResearchAgent.Models;
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

    public async Task SaveTailoredResumeAsync(JobPosting job, TailoredResume tailored)
    {
        await using var conn = new NpgsqlConnection(_connection);
        await conn.OpenAsync();

        var cmd = new NpgsqlCommand(@"
            INSERT INTO job_tailored_resumes
            (external_job_id, professional_summary, key_skills, experience_json)
            VALUES (@jobId, @summary, @skills, @experience)
        ", conn);

        cmd.Parameters.AddWithValue("@jobId", job.ExternalJobId ?? "");
        cmd.Parameters.AddWithValue("@summary", tailored.ProfessionalSummary);
        cmd.Parameters.AddWithValue("@skills", string.Join(", ", tailored.KeySkills));
        cmd.Parameters.AddWithValue("@experience",
            NpgsqlTypes.NpgsqlDbType.Jsonb, JsonSerializer.Serialize(tailored.Experience));

        await cmd.ExecuteNonQueryAsync();
    }
}