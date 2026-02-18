using JobResearchAgent.Matching;

namespace JobResearchAgent.Tests;

public class MatchingConfigurationTests
{
    [Fact]
    public void Defaults_AreExpected()
    {
        var config = new MatchingConfiguration();

        Assert.Equal(0.35, config.MinimumSimilarityThreshold);
        Assert.Equal(75, config.ApplyThreshold);
        Assert.Equal(60, config.ReviewThreshold);
        Assert.Equal(70, config.QualificationThreshold);
    }

    [Fact]
    public void CustomValues_AreRespected()
    {
        var config = new MatchingConfiguration
        {
            MinimumSimilarityThreshold = 0.5,
            ApplyThreshold = 80,
            ReviewThreshold = 65,
            QualificationThreshold = 75
        };

        Assert.Equal(0.5, config.MinimumSimilarityThreshold);
        Assert.Equal(80, config.ApplyThreshold);
        Assert.Equal(65, config.ReviewThreshold);
        Assert.Equal(75, config.QualificationThreshold);
    }
}
