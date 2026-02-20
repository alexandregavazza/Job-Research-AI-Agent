namespace JobResearchAgent.Services.Prompting;

public interface IPromptService
{
    string LoadSystemPrompt(string templateName);
    string LoadUserPrompt(string templateName, Dictionary<string, string> placeholders);
}
