using Microsoft.Extensions.Configuration;

namespace JobResearchAgent.Services;

public static class EarlyCareerTextSelector
{
    public static string SelectLabel(IConfiguration config, bool isPortuguese)
    {
        if (config == null)
            throw new ArgumentNullException(nameof(config));

        var label = isPortuguese
            ? config["Candidate:Career:EarlyCareerLabelPt"]
            : config["Candidate:Career:EarlyCareerLabelEn"];

        return !string.IsNullOrWhiteSpace(label)
            ? label
            : config["Candidate:Career:EarlyCareerLabel"] ?? "Early Career";
    }

    public static string SelectDescription(IConfiguration config, bool isPortuguese)
    {
        if (config == null)
            throw new ArgumentNullException(nameof(config));

        var description = isPortuguese
            ? config["Candidate:Career:DescriptionPt"]
            : config["Candidate:Career:DescriptionEn"];

        return !string.IsNullOrWhiteSpace(description)
            ? description
            : config["Candidate:Career:Description"] ?? "Early career description not configured.";
    }
}
