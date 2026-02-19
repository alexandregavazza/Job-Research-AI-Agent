using JobResearchAgent.Services;
using Microsoft.Extensions.Configuration;

namespace JobResearchAgent.Tests;

public class EarlyCareerTextSelectorTests
{
    [Fact]
    public void SelectLabel_UsesPortugueseValueWhenAvailable()
    {
        var config = BuildConfig();

        var result = EarlyCareerTextSelector.SelectLabel(config, true);

        Assert.Equal("Label PT", result);
    }

    [Fact]
    public void SelectDescription_UsesEnglishValueWhenAvailable()
    {
        var config = BuildConfig();

        var result = EarlyCareerTextSelector.SelectDescription(config, false);

        Assert.Equal("Description EN", result);
    }

    [Fact]
    public void SelectLabel_FallsBackToLegacyWhenLanguageValueMissing()
    {
        var data = new Dictionary<string, string?>
        {
            ["Candidate:Career:EarlyCareerLabel"] = "Legacy Label"
        };
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(data)
            .Build();

        var result = EarlyCareerTextSelector.SelectLabel(config, true);

        Assert.Equal("Legacy Label", result);
    }

    private static IConfiguration BuildConfig()
    {
        var data = new Dictionary<string, string?>
        {
            ["Candidate:Career:EarlyCareerLabelEn"] = "Label EN",
            ["Candidate:Career:EarlyCareerLabelPt"] = "Label PT",
            ["Candidate:Career:DescriptionEn"] = "Description EN",
            ["Candidate:Career:DescriptionPt"] = "Description PT",
            ["Candidate:Career:EarlyCareerLabel"] = "Legacy Label",
            ["Candidate:Career:Description"] = "Legacy Description"
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(data)
            .Build();
    }
}
