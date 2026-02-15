using JobResearchAgent.Services;

namespace JobResearchAgent.Matching;

public class MatchingAgent
{
    private readonly EmbeddingService _embedding;
    private readonly ResumeProfile _resume;

    private float[]? _resumeVector;

    private readonly JobFitScorer _scorer;

    public MatchingAgent(EmbeddingService embedding, JobFitScorer scorer)
    {
        _embedding = embedding;
        _scorer = scorer;
        _resume = ResumeLoader.Load();
    }

    public async Task InitializeAsync()
    {
        _resumeVector = await _embedding.GenerateAsync(_resume.AiText);
    }

    private string NormalizeJobText(JobPosting job)
    {
        return $"""
        Role: {job.Title}

        Responsibilities:
        {job.Description}
        """;
    }


    public async Task<JobMatchResult> EvaluateAsync(JobPosting job)
    {
        if (_resumeVector == null)
            throw new InvalidOperationException("Agent not initialized.");

        // Step 1 — quick semantic filter
        var jobVector = await _embedding.GenerateAsync(
            NormalizeJobText(job));

        var similarity = Similarity.Cosine(_resumeVector, jobVector);

        if (similarity < 0.35)
        {
            return new JobMatchResult
            {
                Job = job,
                Score = similarity * 100,
                Decision = "IGNORE"
            };
        }

        // Step 2 — deep reasoning (THIS is where JobFitScorer is used)
        var (score, reason) = await _scorer.ScoreAsync(
            _resume.HumanText,
            job.Title,
            job.Description);

        var decision =
            score >= 75 ? "APPLY" :
            score >= 60 ? "REVIEW" :
            "IGNORE";

        return new JobMatchResult
        {
            Job = job,
            Score = score,
            Decision = decision,
            Reason = reason
        };
    }
}
