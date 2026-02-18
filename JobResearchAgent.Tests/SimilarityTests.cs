using JobResearchAgent.Matching;

namespace JobResearchAgent.Tests;

public class SimilarityTests
{
    [Fact]
    public void Cosine_IdenticalVectors_ReturnsOne()
    {
        var result = Similarity.Cosine(new float[] { 1f, 2f, 3f }, new float[] { 1f, 2f, 3f });

        Assert.InRange(result, 0.999, 1.001);
    }

    [Fact]
    public void Cosine_OrthogonalVectors_ReturnsZero()
    {
        var result = Similarity.Cosine(new float[] { 1f, 0f }, new float[] { 0f, 1f });

        Assert.InRange(result, -0.000001, 0.000001);
    }

    [Fact]
    public void Cosine_OppositeVectors_ReturnsMinusOne()
    {
        var result = Similarity.Cosine(new float[] { 1f, 0f }, new float[] { -1f, 0f });

        Assert.InRange(result, -1.001, -0.999);
    }

    [Fact]
    public void Cosine_SameDirectionDifferentMagnitude_ReturnsOne()
    {
        var result = Similarity.Cosine(new float[] { 2f, 0f, 0f }, new float[] { 1f, 0f, 0f });

        Assert.InRange(result, 0.999, 1.001);
    }
}
