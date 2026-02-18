using JobResearchAgent.Services;
using JobResearchAgent.Models;
using Microsoft.Extensions.Options;

namespace JobResearchAgent.Matching;

/// <summary>
/// Matching agent following SOLID principles with dependency injection
/// </summary>
public class MatchingAgent
{
    private readonly EmbeddingService _embedding;
    private readonly ResumeProfile _resume;
    private readonly JobFitScorer _scorer;
    private readonly MatchingConfiguration _config;

    private float[]? _resumeVector;

    public MatchingAgent(
        EmbeddingService embedding, 
        JobFitScorer scorer,
        IResumeLoader resumeLoader,
        IOptions<MatchingConfiguration> config)
    {
        _embedding = embedding ?? throw new ArgumentNullException(nameof(embedding));
        _scorer = scorer ?? throw new ArgumentNullException(nameof(scorer));
        _config = config?.Value ?? throw new ArgumentNullException(nameof(config));
        
        if (resumeLoader == null)
            throw new ArgumentNullException(nameof(resumeLoader));
            
        _resume = resumeLoader.Load();
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

        if (similarity < _config.MinimumSimilarityThreshold)
        {
            return new JobMatchResult
            {
                Job = job,
                Score = similarity * 100,
                Decision = "IGNORE"
            };
        }

        // Step 2 — deep reasoning
        var (score, reason) = await _scorer.ScoreAsync(
            _resume.HumanText,
            job.Title,
            job.Description);

        var decision =
            score >= _config.ApplyThreshold ? "APPLY" :
            score >= _config.ReviewThreshold ? "REVIEW" :
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
