namespace JobResearchAgent.Models;

public class TailoredResume
{
    public string ProfessionalSummary { get; set; } = "";
    public List<string> KeySkills { get; set; } = new();
    public List<TailoredExperience> Experience { get; set; } = new();
}

public class TailoredExperience
{
    public string Role { get; set; } = "";
    public string Company { get; set; } = "";
    public List<string> Highlights { get; set; } = new();
}
