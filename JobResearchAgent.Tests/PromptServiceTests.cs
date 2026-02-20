using JobResearchAgent.Services.Prompting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;

namespace JobResearchAgent.Tests;

public class PromptServiceTests
{
    [Fact]
    public void LoadSystemPrompt_ReturnsCachedContent()
    {
        var tempDir = CreateTempDir();
        try
        {
            var templatePath = Path.Combine(tempDir, "PromptTemplates");
            Directory.CreateDirectory(templatePath);

            var filePath = Path.Combine(templatePath, "Test.System.txt");
            File.WriteAllText(filePath, "First");

            var service = CreateService(tempDir);

            var first = service.LoadSystemPrompt("Test");

            File.WriteAllText(filePath, "Second");

            var second = service.LoadSystemPrompt("Test");

            Assert.Equal("First", first);
            Assert.Equal("First", second);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void LoadUserPrompt_ReplacesPlaceholders()
    {
        var tempDir = CreateTempDir();
        try
        {
            var templatePath = Path.Combine(tempDir, "PromptTemplates");
            Directory.CreateDirectory(templatePath);

            var filePath = Path.Combine(templatePath, "Test.User.txt");
            File.WriteAllText(filePath, "Hello {{NAME}}.");

            var service = CreateService(tempDir);

            var result = service.LoadUserPrompt(
                "Test",
                new Dictionary<string, string> { { "NAME", "Alex" } });

            Assert.Equal("Hello Alex.", result);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void LoadSystemPrompt_ThrowsWhenTemplateMissing()
    {
        var tempDir = CreateTempDir();
        try
        {
            var service = CreateService(tempDir);

            Assert.Throws<FileNotFoundException>(() => service.LoadSystemPrompt("Missing"));
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    private static PromptService CreateService(string contentRoot)
    {
        var env = new Mock<IHostEnvironment>();
        env.SetupGet(e => e.ContentRootPath).Returns(contentRoot);

        var logger = new Mock<ILogger<PromptService>>();

        return new PromptService(env.Object, logger.Object);
    }

    private static string CreateTempDir()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "jra-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        return tempDir;
    }
}
