namespace JobResearchAgent.Matching;

/// <summary>
/// Configuration for matching thresholds, following Open/Closed Principle
/// </summary>
public class MatchingConfiguration
{
    /// <summary>
    /// Minimum similarity threshold for initial filtering (0.0 to 1.0)
    /// </summary>
    public double MinimumSimilarityThreshold { get; init; } = 0.35;

    /// <summary>
    /// Minimum score for "APPLY" recommendation (0-100)
    /// </summary>
    public double ApplyThreshold { get; init; } = 75;

    /// <summary>
    /// Minimum score for "REVIEW" recommendation (0-100)
    /// </summary>
    public double ReviewThreshold { get; init; } = 60;

    /// <summary>
    /// Minimum score for acceptance in the pipeline (0-100)
    /// </summary>
    public double QualificationThreshold { get; init; } = 70;
}
