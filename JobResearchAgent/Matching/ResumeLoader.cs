using System;
using System.IO;
using JobResearchAgent.Matching;

namespace JobResearchAgent.Services;

/// <summary>
/// Concrete implementation of IResumeLoader following SOLID principles
/// </summary>
public class ResumeLoader : IResumeLoader
{
    private const string HumanResumePath = "Profiles/resume.human.txt";
    private const string AiResumePath = "Profiles/resume.ai.txt";

    public ResumeProfile Load()
    {
        if (!File.Exists(HumanResumePath))
            throw new FileNotFoundException(
                $"Human resume file not found at '{HumanResumePath}'. " +
                $"Create this file and paste your full resume text.");

        if (!File.Exists(AiResumePath))
            throw new FileNotFoundException(
                $"AI resume file not found at '{AiResumePath}'. " +
                $"Create this file with the structured skill version.");

        var humanText = File.ReadAllText(HumanResumePath);
        var aiText = File.ReadAllText(AiResumePath);

        if (string.IsNullOrWhiteSpace(humanText))
            throw new InvalidOperationException("Human resume is empty.");

        if (string.IsNullOrWhiteSpace(aiText))
            throw new InvalidOperationException("AI resume is empty.");

        return new ResumeProfile
        {
            HumanText = humanText,
            AiText = aiText
        };
    }
}
